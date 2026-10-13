using System.Text.Json;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Pdf;

namespace Bangplanix.Fluent;

public sealed class BangplanixDocument
{
    private readonly ReportDefinition _reportDefinition;

    private BangplanixDocument(ReportDefinition reportDefinition)
    {
        _reportDefinition = reportDefinition ?? throw new ArgumentNullException(nameof(reportDefinition));
    }

    public static BangplanixDocument Create(Action<IDocumentContainer> composer)
    {
        ArgumentNullException.ThrowIfNull(composer);

        var docContainer = new DocumentContainer();
        composer(docContainer);

        var report = docContainer.PageBuilder.CompileReport();
        return new BangplanixDocument(report);
    }

    public ReportDefinition ToReportDefinition() => _reportDefinition;

    public string ToJson(bool indented = true)
    {
        return JsonSerializer.Serialize(_reportDefinition, new JsonSerializerOptions
        {
            WriteIndented = indented,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    public BangplanixDocument WithWatermark(string text, float opacity = 0.2f, float rotationAngle = -45f)
    {
        _reportDefinition.PageSetup.Watermark = new WatermarkDefinition
        {
            Text = text,
            Opacity = opacity,
            RotationAngle = rotationAngle,
            Layer = WatermarkLayer.Background
        };
        return this;
    }

    public async Task<byte[]> GeneratePdfAsync(
        IDictionary<string, object?>? parameters = null,
        IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null,
        CancellationToken cancellationToken = default)
    {
        var renderer = new SkiaPdfRenderer();
        return await renderer.RenderToPdfAsync(_reportDefinition, parameters, mainDataRows, cancellationToken).ConfigureAwait(false);
    }

    public byte[] GeneratePdf(
        IDictionary<string, object?>? parameters = null,
        IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null)
    {
        return GeneratePdfAsync(parameters, mainDataRows).GetAwaiter().GetResult();
    }

    public async Task GeneratePdfStreamAsync(
        Stream outputStream,
        IDictionary<string, object?>? parameters = null,
        IReadOnlyList<IDictionary<string, object?>>? mainDataRows = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputStream);
        var renderer = new SkiaPdfRenderer();
        await renderer.RenderToStreamAsync(_reportDefinition, outputStream, parameters, mainDataRows, cancellationToken).ConfigureAwait(false);
    }
}
