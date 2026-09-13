using MiniExcelLibs;
using MiniExcelLibs.Attributes;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Excel;

public static class MiniExcelReportExporter
{
    public static async Task ExportToStreamAsync(
        Stream outputStream,
        IEnumerable<IDictionary<string, object?>> rows,
        string sheetName = "Report",
        bool printHeader = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(rows);

        // Normalize dictionary keys for MiniExcel
        var normalizedRows = rows.Select(r => (IDictionary<string, object?>)new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();

        await outputStream.SaveAsAsync(
            normalizedRows,
            printHeader: printHeader,
            sheetName: sheetName,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static async Task ExportMultiSheetToStreamAsync(
        Stream outputStream,
        IDictionary<string, IEnumerable<IDictionary<string, object?>>> sheets,
        bool printHeader = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(sheets);

        var sheetData = new Dictionary<string, object>();
        foreach (var (name, rows) in sheets)
        {
            var normalizedRows = rows.Select(r => (IDictionary<string, object?>)new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase)).ToList();
            sheetData[name] = normalizedRows;
        }

        await outputStream.SaveAsAsync(
            sheetData,
            printHeader: printHeader,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static async Task ExportReportToExcelAsync(
        ReportDefinition report,
        IReadOnlyList<IDictionary<string, object?>> dataRows,
        Stream outputStream,
        string? sheetName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(dataRows);
        ArgumentNullException.ThrowIfNull(outputStream);

        var effectiveSheetName = string.IsNullOrWhiteSpace(sheetName)
            ? (string.IsNullOrWhiteSpace(report.Metadata?.Title) ? "Report" : report.Metadata.Title)
            : sheetName;

        // Inspect Detail band elements to build column mappings
        var detailBand = report.Bands?.Detail;
        var headerBand = report.Bands?.PageHeader ?? report.Bands?.ReportHeader;

        var columnHeaders = new List<string>();
        var columnFieldKeys = new List<string>();

        if (detailBand?.Elements != null && detailBand.Elements.Count > 0)
        {
            for (int i = 0; i < detailBand.Elements.Count; i++)
            {
                var el = detailBand.Elements[i];
                var fieldKey = string.Empty;

                if (!string.IsNullOrWhiteSpace(el.Expression))
                {
                    // e.g. "=Fields.Amount" or "=Fields["Amount"]"
                    var raw = el.Expression.Trim().TrimStart('=');
                    raw = raw.Replace("Fields.", "", StringComparison.OrdinalIgnoreCase);
                    raw = raw.Replace("Fields[\"", "", StringComparison.OrdinalIgnoreCase);
                    raw = raw.Replace("\"]", "", StringComparison.OrdinalIgnoreCase);
                    fieldKey = raw.Trim();
                }
                else if (!string.IsNullOrWhiteSpace(el.Text))
                {
                    fieldKey = el.Text;
                }
                else
                {
                    fieldKey = el.Id ?? $"Column{i + 1}";
                }

                // Check corresponding header text
                var headerText = fieldKey;
                if (headerBand?.Elements != null && i < headerBand.Elements.Count && !string.IsNullOrWhiteSpace(headerBand.Elements[i].Text))
                {
                    headerText = headerBand.Elements[i].Text!;
                }

                columnFieldKeys.Add(fieldKey);
                columnHeaders.Add(headerText);
            }
        }

        // Project data rows into ordered dictionaries with mapped headers
        var projectedRows = new List<IDictionary<string, object?>>(dataRows.Count);

        foreach (var row in dataRows)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (columnHeaders.Count > 0)
            {
                for (int i = 0; i < columnHeaders.Count; i++)
                {
                    var colHeader = columnHeaders[i];
                    var fieldKey = columnFieldKeys[i];

                    if (row.TryGetValue(fieldKey, out var val))
                    {
                        dict[colHeader] = val;
                    }
                    else
                    {
                        // Fallback: check case-insensitive match on keys
                        var matchingKey = row.Keys.FirstOrDefault(k => string.Equals(k, fieldKey, StringComparison.OrdinalIgnoreCase));
                        dict[colHeader] = matchingKey != null ? row[matchingKey] : null;
                    }
                }
            }
            else
            {
                foreach (var (k, v) in row)
                {
                    dict[k] = v;
                }
            }

            projectedRows.Add(dict);
        }

        await ExportToStreamAsync(outputStream, projectedRows, sheetName: effectiveSheetName, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static byte[] ExportToBytes(
        IEnumerable<IDictionary<string, object?>> rows,
        string sheetName = "Report",
        bool printHeader = true)
    {
        using var ms = new MemoryStream();
        ExportToStreamAsync(ms, rows, sheetName, printHeader).GetAwaiter().GetResult();
        return ms.ToArray();
    }
}
