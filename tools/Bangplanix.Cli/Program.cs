using System.Diagnostics;
using System.Text.Json;
using Bangplanix.Adapters.Common;
using Bangplanix.Connectors.Excel;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Pdf;
using Bangplanix.Printing.EscPos;
using Bangplanix.Printing.Protocols;
using Bangplanix.Printing.Zpl;

Console.WriteLine("=================================================");
Console.WriteLine("Bangplanix CLI — High Performance Reporting Engine");
Console.WriteLine("=================================================");

if (args.Length == 0 || args[0] == "-h" || args[0] == "--help" || args[0] == "help")
{
    PrintUsage();
    return 0;
}

var command = args[0].ToLowerInvariant();

if (command == "doctor")
{
    var tempDir = GetArgValue(args, "--temp-dir");
    var fontDir = GetArgValue(args, "--font-dir");
    var report = Bangplanix.Core.Diagnostics.SystemDoctor.RunFullDiagnostics(tempDir, fontDir);
    Console.WriteLine(report.FormatAsciiReport());
    return report.AllCriticalPassed ? 0 : 1;
}

if (command == "validate")
{
    var path = GetArgValue(args, "-t", "--template") ?? (args.Length > 1 && !args[1].StartsWith('-') ? args[1] : null);
    if (string.IsNullOrEmpty(path) || !File.Exists(path))
    {
        Console.Error.WriteLine($"Error: Template file '{path}' not found.");
        return 1;
    }

    try
    {
        var json = await File.ReadAllTextAsync(path);
        var report = BpxParser.Parse(json);
        Console.WriteLine($"✓ Valid .bpx template: '{report.Metadata?.Title ?? "Untitled"}' (v{report.Version})");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Validation Error: {ex.Message}");
        return 1;
    }
}

if (command == "migrate")
{
    var inputPath = GetArgValue(args, "-i", "--input") ?? (args.Length > 1 && !args[1].StartsWith('-') ? args[1] : null);
    var outputPath = GetArgValue(args, "-o", "--output");

    if (string.IsNullOrEmpty(inputPath) || (!File.Exists(inputPath) && !Directory.Exists(inputPath)))
    {
        Console.Error.WriteLine($"Error: Input file or directory '{inputPath}' not found.");
        return 1;
    }

    try
    {
        var sw = Stopwatch.StartNew();

        if (File.Exists(inputPath))
        {
            var targetOut = outputPath ?? Path.ChangeExtension(inputPath, ".bpx");
            var outDir = Path.GetDirectoryName(targetOut);
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            Console.WriteLine($"[1/2] Converting legacy report '{inputPath}' via LegacyAdapterFactory...");
            var report = LegacyAdapterFactory.ConvertFile(inputPath);

            Console.WriteLine($"[2/2] Writing Bangplanix .bpx file to '{targetOut}'...");
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(targetOut, json);

            sw.Stop();
            Console.WriteLine($"✓ Migrated '{Path.GetFileName(inputPath)}' -> '{Path.GetFileName(targetOut)}' ({sw.ElapsedMilliseconds} ms)");
        }
        else if (Directory.Exists(inputPath))
        {
            var targetOutDir = outputPath ?? Path.Combine(inputPath, "bpx_migrated");
            if (!Directory.Exists(targetOutDir)) Directory.CreateDirectory(targetOutDir);

            var supportedExts = new HashSet<string>(
                LegacyAdapterFactory.RegisteredAdapters.SelectMany(a => a.SupportedExtensions),
                StringComparer.OrdinalIgnoreCase);

            var files = Directory.GetFiles(inputPath, "*.*", SearchOption.AllDirectories)
                .Where(f => supportedExts.Contains(Path.GetExtension(f)))
                .ToList();

            Console.WriteLine($"Found {files.Count} legacy report template(s) to migrate in '{inputPath}'...");

            int successCount = 0;
            foreach (var file in files)
            {
                try
                {
                    var rel = Path.GetRelativePath(inputPath, file);
                    var dest = Path.Combine(targetOutDir, Path.ChangeExtension(rel, ".bpx"));
                    var destDir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    var report = LegacyAdapterFactory.ConvertFile(file);
                    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(dest, json);
                    Console.WriteLine($"  ✓ {rel} -> {Path.GetFileName(dest)}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ✗ Failed to migrate '{file}': {ex.Message}");
                }
            }

            sw.Stop();
            Console.WriteLine($"✓ Batch migration completed: {successCount}/{files.Count} reports migrated in {sw.ElapsedMilliseconds} ms.");
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Migration Error: {ex.Message}");
        return 1;
    }
}

if (command == "print")
{
    var templatePath = GetArgValue(args, "-t", "--template") ?? (args.Length > 1 && !args[1].StartsWith('-') ? args[1] : null);
    var dataPath = GetArgValue(args, "-d", "--data");
    var mode = (GetArgValue(args, "-p", "--printer-type") ?? "escpos").ToLowerInvariant();
    var host = GetArgValue(args, "--host", "-h");
    var portStr = GetArgValue(args, "--port");
    var outputPath = GetArgValue(args, "-o", "--output");
    var protocolStr = (GetArgValue(args, "--protocol") ?? "tcp").ToLowerInvariant();

    if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
    {
        Console.Error.WriteLine($"Error: Template file '{templatePath}' not found.");
        return 1;
    }

    try
    {
        Console.WriteLine($"[1/3] Parsing .bpx template: {templatePath}");
        var templateJson = await File.ReadAllTextAsync(templatePath);
        var report = BpxParser.Parse(templateJson);

        IReadOnlyList<IDictionary<string, object?>> dataRows = Array.Empty<IDictionary<string, object?>>();
        if (!string.IsNullOrEmpty(dataPath) && File.Exists(dataPath))
        {
            Console.WriteLine($"[2/3] Loading data payload: {dataPath}");
            var dataJson = await File.ReadAllTextAsync(dataPath);
            dataRows = JsonPushStreamConnector.ParseJsonStringToRows(dataJson);
        }

        byte[] printBytes;
        if (mode is "zpl" or "zebra")
        {
            Console.WriteLine($"[3/3] Generating Zebra ZPL II commands...");
            var zplString = ZplReportRenderer.RenderToZpl(report, dataRows, ZplDpi.Dpi203);
            printBytes = System.Text.Encoding.UTF8.GetBytes(zplString);
        }
        else
        {
            Console.WriteLine($"[3/3] Generating ESC/POS thermal receipt commands...");
            printBytes = EscPosReportRenderer.RenderToEscPos(report, dataRows);
        }

        if (!string.IsNullOrEmpty(outputPath))
        {
            await File.WriteAllBytesAsync(outputPath, printBytes);
            Console.WriteLine($"✓ Hardware print output saved to file: {Path.GetFullPath(outputPath)} ({printBytes.Length} bytes)");
        }

        if (!string.IsNullOrEmpty(host))
        {
            int port = int.TryParse(portStr, out var p) ? p : (protocolStr == "ipp" ? 631 : (protocolStr == "lpr" ? 515 : 9100));
            Console.WriteLine($"Sending {printBytes.Length} bytes directly to printer {host}:{port} via {protocolStr.ToUpperInvariant()}...");

            IPrinterProtocol protocol = protocolStr switch
            {
                "ipp" => new IppPrinter(),
                "lpr" => new LprPrinter(),
                _ => new RawSocketPrinter()
            };

            var success = await protocol.PrintAsync(printBytes, host, port);
            if (success)
            {
                Console.WriteLine($"✓ Successfully printed to {host}:{port} ({protocol.ProtocolName})");
            }
            else
            {
                Console.Error.WriteLine($"✗ Failed to print to {host}:{port}");
                return 1;
            }
        }
        else if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine($"✓ Generated {printBytes.Length} print bytes (specify --host <ip> or -o <file> to transmit or save)");
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Print Error: {ex.Message}");
        return 1;
    }
}

if (command == "render")
{
    var templatePath = GetArgValue(args, "-t", "--template") ?? (args.Length > 1 && !args[1].StartsWith('-') ? args[1] : null);
    var dataPath = GetArgValue(args, "-d", "--data");
    var outputPath = GetArgValue(args, "-o", "--output") ?? "output.pdf";
    var format = (GetArgValue(args, "-f", "--format") ?? Path.GetExtension(outputPath).TrimStart('.')).ToLowerInvariant();

    if (string.IsNullOrEmpty(templatePath) || !File.Exists(templatePath))
    {
        Console.Error.WriteLine($"Error: Template file '{templatePath}' not found.");
        return 1;
    }

    try
    {
        Console.WriteLine($"[1/3] Parsing .bpx template: {templatePath}");
        var templateJson = await File.ReadAllTextAsync(templatePath);
        var report = BpxParser.Parse(templateJson);

        IReadOnlyList<IDictionary<string, object?>> dataRows = Array.Empty<IDictionary<string, object?>>();
        if (!string.IsNullOrEmpty(dataPath) && File.Exists(dataPath))
        {
            Console.WriteLine($"[2/3] Loading data payload: {dataPath}");
            var dataJson = await File.ReadAllTextAsync(dataPath);
            dataRows = JsonPushStreamConnector.ParseJsonStringToRows(dataJson);
        }
        else if (report.Datasets?.FirstOrDefault()?.StaticData != null)
        {
            dataRows = JsonPushStreamConnector.ParseObjectToRows(report.Datasets[0].StaticData!);
        }

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
        {
            Directory.CreateDirectory(outDir);
        }

        var sw = Stopwatch.StartNew();

        if (format == "xlsx" || format == "excel")
        {
            Console.WriteLine($"[3/3] Exporting High-Speed XLSX via MiniExcel...");
            using var fs = File.Create(outputPath);
            await MiniExcelReportExporter.ExportReportToExcelAsync(report, dataRows, fs);
            sw.Stop();
            Console.WriteLine($"✓ Excel generated successfully in {sw.ElapsedMilliseconds} ms: {Path.GetFullPath(outputPath)} ({new FileInfo(outputPath).Length:N0} bytes)");
        }
        else
        {
            Console.WriteLine($"[3/3] Rendering Vector PDF via SkiaSharp Engine...");
            var renderer = new SkiaPdfRenderer();
            var pdfBytes = await renderer.RenderToPdfAsync(report, null, dataRows);
            await File.WriteAllBytesAsync(outputPath, pdfBytes);
            sw.Stop();
            Console.WriteLine($"✓ PDF generated successfully in {sw.ElapsedMilliseconds} ms: {Path.GetFullPath(outputPath)} ({pdfBytes.Length:N0} bytes)");
        }

        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Render Error: {ex.Message}");
        return 1;
    }
}

Console.WriteLine($"Unknown command: {command}");
PrintUsage();
return 1;

static string? GetArgValue(string[] args, string shortFlag, string longFlag = "")
{
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].Equals(shortFlag, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(longFlag) && args[i].Equals(longFlag, StringComparison.OrdinalIgnoreCase)))
        {
            if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
            {
                return args[i + 1];
            }
        }
    }
    return null;
}

static void PrintUsage()
{
    Console.WriteLine("Usage: bangplanix <command> [options]");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  doctor    Inspect environment, hardware intrinsics, crypto & storage readiness");
    Console.WriteLine("            Options: --temp-dir <path>           Custom temp spill directory to check");
    Console.WriteLine("                     --font-dir <path>           Custom font directory to check");
    Console.WriteLine("  print     Direct hardware printing to network receipt or label printers");
    Console.WriteLine("            Options: -t, --template <file.bpx>   Template file path");
    Console.WriteLine("                     -d, --data <file.json>      Data JSON payload");
    Console.WriteLine("                     -p, --printer-type <escpos|zpl> Printer language (default: escpos)");
    Console.WriteLine("                     --host <ip|hostname>        Network printer host");
    Console.WriteLine("                     --port <port>               Printer port (default: 9100 / 631 / 515)");
    Console.WriteLine("                     --protocol <tcp|ipp|lpr>    Network protocol (default: tcp)");
    Console.WriteLine("                     -o, --output <file>         Save raw printer commands to file");
    Console.WriteLine("  migrate   Migrate legacy reports (RDL, RDLC, RPT, JRXML, FRX, MRT, TRDX, TRDP, REPX, RPX, RDLX, RPTDESIGN, HTML) to .bpx");
    Console.WriteLine("            Options: -i, --input <file|dir>      Legacy report file or directory");
    Console.WriteLine("                     -o, --output <file|dir>     Output .bpx file or destination directory");
    Console.WriteLine("  render    Render a report to PDF or XLSX");
    Console.WriteLine("            Options: -t, --template <file.bpx>   Template file path");
    Console.WriteLine("                     -d, --data <file.json>      Data JSON payload");
    Console.WriteLine("                     -o, --output <file.pdf>     Output destination");
    Console.WriteLine("                     -f, --format <pdf|xlsx>     Output format");
    Console.WriteLine("  validate  Validate a .bpx template schema");
    Console.WriteLine("            Options: -t, --template <file.bpx>   Template file path");
}
