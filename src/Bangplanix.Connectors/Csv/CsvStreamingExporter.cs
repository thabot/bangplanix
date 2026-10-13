using System.Text;
using Bangplanix.Core.Security;

namespace Bangplanix.Connectors.Csv;

/// <summary>
/// Enterprise CSV Streaming Exporter.
/// Features:
/// 1. UTF-8 Byte Order Mark (BOM) to prevent Thai and CJK text garbling in Microsoft Excel.
/// 2. CSV Formula Injection Guard (CWE-123) to sanitize malicious leading characters (=, +, -, @, \t, \r).
/// 3. High-throughput custom delimiters (Comma, Semicolon, Pipe, Tab).
/// </summary>
public static class CsvStreamingExporter
{
    private static readonly char[] DangerousLeadingChars = { '=', '+', '-', '@', '\t', '\r' };

    /// <summary>
    /// Exports rows of dictionary data to a CSV stream with enterprise security and encoding rules.
    /// </summary>
    public static async Task ExportToStreamAsync(
        Stream outputStream,
        IEnumerable<IDictionary<string, object?>> rows,
        CsvExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(rows);

        var opt = options ?? new CsvExportOptions();
        var delimiter = string.IsNullOrEmpty(opt.Delimiter) ? "," : opt.Delimiter;

        // 1. Write UTF-8 BOM if requested (EF BB BF)
        if (opt.IncludeUtf8Bom)
        {
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            await outputStream.WriteAsync(bom, cancellationToken).ConfigureAwait(false);
        }

        using var writer = new StreamWriter(outputStream, new UTF8Encoding(false), bufferSize: 64 * 1024, leaveOpen: true);

        bool headerWritten = false;
        List<string> headers = new();

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!headerWritten)
            {
                headers = row.Keys.ToList();
                var headerLine = string.Join(delimiter, headers.Select(h => FormatCell(h, opt)));
                await writer.WriteLineAsync(headerLine.AsMemory(), cancellationToken).ConfigureAwait(false);
                headerWritten = true;
            }

            var values = headers.Select(h =>
            {
                var val = row.TryGetValue(h, out var v) ? v?.ToString() ?? string.Empty : string.Empty;
                return FormatCell(val, opt);
            });

            var line = string.Join(delimiter, values);
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sanitizes and escapes individual cell values.
    /// </summary>
    public static string FormatCell(string input, CsvExportOptions options)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var val = input;

        // CSV Formula Injection Sanitization (CWE-123)
        if (options.SanitizeFormulas && DangerousLeadingChars.Contains(val[0]))
        {
            val = "'" + val;
        }

        // Quote string if contains delimiter, quote, or newline
        bool needsQuotes = options.QuoteStrings ||
                           val.Contains(options.Delimiter, StringComparison.Ordinal) ||
                           val.Contains('\"') ||
                           val.Contains('\n') ||
                           val.Contains('\r');

        if (needsQuotes)
        {
            val = "\"" + val.Replace("\"", "\"\"") + "\"";
        }

        return val;
    }
}
