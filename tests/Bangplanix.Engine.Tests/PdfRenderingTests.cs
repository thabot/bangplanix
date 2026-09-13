using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Pdf;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PdfRenderingTests
{
    [Fact]
    public async Task RenderToPdf_InvoiceSample_ShouldGenerateValidPdfBytes()
    {
        // Arrange
        var samplePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schema", "v1", "samples", "invoice.bpx");
        if (!File.Exists(samplePath))
        {
            samplePath = Path.GetFullPath("../../../../../schema/v1/samples/invoice.bpx");
        }

        var json = await File.ReadAllTextAsync(samplePath);
        var report = BpxParser.Parse(json);

        var renderer = new SkiaPdfRenderer();

        // Act
        var pdfBytes = await renderer.RenderToPdfAsync(report);

        // Assert
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500, "PDF document must contain header, pages, and cross-reference stream");

        // Verify PDF Header magic bytes (%PDF-1.4 or %PDF-1.5)
        var header = Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 8));
        header.Should().StartWith("%PDF-");
    }

    [Fact]
    public void UnitConverter_MmToPoints_ShouldConvertAccurately()
    {
        // 25.4 mm = 72 pt (1 inch)
        var points = UnitConverter.ToPoints(25.4, UnitType.Mm);
        points.Should().BeApproximately(72.0f, 0.001f);

        // A4 Dimensions: 210mm x 297mm
        var pageSetup = new PageSetup { PaperKind = PaperKind.A4, Unit = UnitType.Mm, Width = 210.0, Height = 297.0 };
        var (wPt, hPt) = UnitConverter.GetPageDimensionsInPoints(pageSetup);
        wPt.Should().BeApproximately(595.275f, 0.1f);
        hPt.Should().BeApproximately(841.889f, 0.1f);
    }

    [Fact]
    public void RecordBandToPicture_ShouldReturnValidSKPicture()
    {
        // Arrange
        var report = new ReportDefinition();
        var band = new BandDefinition
        {
            Height = 20.0,
            Elements =
            [
                new ElementDefinition { Type = ElementType.Text, Text = "Cached Header", X = 0, Y = 0, Width = 100, Height = 10 }
            ]
        };

        // Act
        using var picture = SkiaReportCanvas.RecordBandToPicture(band, UnitType.Mm, report);

        // Assert
        picture.Should().NotBeNull();
        picture.CullRect.Width.Should().BeGreaterThan(0);
        picture.CullRect.Height.Should().BeGreaterThan(0);
    }
}
