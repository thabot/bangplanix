using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;

namespace Bangplanix.Adapters.Crystal;

public sealed class CrystalBatchConversionResult
{
    public int TotalFound { get; set; }
    public int ConvertedCount { get; set; }
    public int FailedCount { get; set; }
    public double ElapsedMilliseconds { get; set; }
    public List<string> ConvertedFiles { get; } = [];
    public List<string> ErrorDetails { get; } = [];
}

public static class CrystalBridgeRunner
{
    public static async Task<CrystalBatchConversionResult> ConvertDirectoryAsync(
        string sourceDirectory,
        string outputDirectory,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        if (!Directory.Exists(sourceDirectory))
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDirectory}");

        Directory.CreateDirectory(outputDirectory);

        var sw = Stopwatch.StartNew();
        var adapter = new CrystalReportsXmlAdapter();
        var files = Directory.GetFiles(sourceDirectory, "*.xml", SearchOption.AllDirectories);

        var result = new CrystalBatchConversionResult
        {
            TotalFound = files.Length
        };

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string content = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var report = adapter.Convert(content);

                string baseName = Path.GetFileName(file);
                if (baseName.EndsWith(".rpt.xml", StringComparison.OrdinalIgnoreCase))
                    baseName = baseName[..^8];
                else if (baseName.EndsWith(".crystal.xml", StringComparison.OrdinalIgnoreCase))
                    baseName = baseName[..^12];
                else
                    baseName = Path.GetFileNameWithoutExtension(file);

                string outPath = Path.Combine(outputDirectory, $"{baseName}.bpx");

                if (File.Exists(outPath) && !overwrite)
                {
                    result.ConvertedCount++;
                    continue;
                }

                string json = BpxParser.ToJson(report, indented: true);
                await File.WriteAllTextAsync(outPath, json, cancellationToken).ConfigureAwait(false);

                result.ConvertedCount++;
                result.ConvertedFiles.Add(outPath);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.ErrorDetails.Add($"File '{Path.GetFileName(file)}': {ex.Message}");
            }
        }

        sw.Stop();
        result.ElapsedMilliseconds = sw.Elapsed.TotalMilliseconds;
        return result;
    }
}