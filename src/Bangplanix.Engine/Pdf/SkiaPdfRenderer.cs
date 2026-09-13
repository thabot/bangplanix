using System.Text.Json;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Pagination;
using SkiaSharp;

namespace Bangplanix.Engine.Pdf;

public sealed class SkiaPdfRenderer : IReportRenderer
{
    public async Task<byte[]> RenderToPdfAsync(
        ReportDefinition report,
        IDictionary<string, object?>? parameters = null,
        IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await RenderToStreamAsync(report, memoryStream, parameters, mainDataRows, cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    public Task RenderToStreamAsync(
        ReportDefinition report,
        Stream outputStream,
        IDictionary<string, object?>? parameters = null,
        IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(outputStream);

        var dataRows = mainDataRows ?? ExtractDataRows(report);

        var context = new BandContext
        {
            Report = report,
            Parameters = parameters != null ? new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
            MainDataRows = dataRows
        };

        var pdfMetadata = new SKDocumentPdfMetadata
        {
            Title = report.Metadata.Title ?? "Bangplanix Report",
            Author = report.Metadata.Author ?? "Bangplanix",
            Subject = report.Metadata.Description ?? "Enterprise Report",
            Creator = "Bangplanix .NET 10 Vector Engine",
            Producer = "SkiaSharp / HarfBuzz PDF",
            Creation = report.Metadata.CreatedAt ?? DateTime.UtcNow,
            Modified = report.Metadata.UpdatedAt ?? DateTime.UtcNow,
            RasterDpi = 300.0f,
            PdfA = false
        };

        using var document = SKDocument.CreatePdf(outputStream, pdfMetadata);
        PaginationEngine.RenderReportPages(report, document, context);
        document.Close();

        return Task.CompletedTask;
    }

    private static List<IDictionary<string, object?>> ExtractDataRows(ReportDefinition report)
    {
        if (report.Datasets.Count == 0)
        {
            return [];
        }

        var primaryDataset = report.Datasets[0];
        if (primaryDataset.StaticData is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            var list = new List<IDictionary<string, object?>>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (item.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in item.EnumerateObject())
                    {
                        dict[prop.Name] = prop.Value.ValueKind switch
                        {
                            JsonValueKind.Number when prop.Value.TryGetInt64(out var l) => l,
                            JsonValueKind.Number when prop.Value.TryGetDouble(out var d) => d,
                            JsonValueKind.String => prop.Value.GetString(),
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => prop.Value.ToString()
                        };
                    }
                }
                list.Add(dict);
            }
            return list;
        }

        return [];
    }
}
