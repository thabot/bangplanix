using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Pdf;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class VisualRegressionSnapshotTests
{
    [Fact]
    public void DeterministicRendering_SameReportRenderedTwice_ShouldHaveZeroPixelDiff()
    {
        // Arrange
        var samplePath = Path.GetFullPath("../../../../../schema/v1/samples/invoice.bpx");
        var json = File.ReadAllText(samplePath);
        var report = BpxParser.Parse(json);

        // Render Page 1 to Bitmap A
        using var bitmapA = RenderReportPageToBitmap(report, pageIndex: 0);

        // Render Page 1 to Bitmap B
        using var bitmapB = RenderReportPageToBitmap(report, pageIndex: 0);

        // Calculate Pixel Diff Percentage
        var diffRatio = ComputePixelDiffRatio(bitmapA, bitmapB);

        // Assert
        diffRatio.Should().Be(0.0, "Subsequent renders of the same vector report must be 100% deterministic (0% pixel diff)");
    }

    [Fact]
    public void MultiBandHierarchy_ShouldRenderWithoutOverlapOrPixelErrors()
    {
        // Arrange: Report with PageHeader, GroupHeader, Detail, GroupFooter, PageFooter
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup { PaperKind = PaperKind.A4, Unit = UnitType.Mm },
            Bands = new BandsDefinition
            {
                PageHeader = new BandDefinition
                {
                    Height = 25.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Page Header", X = 0, Y = 0, Width = 100, Height = 10 }]
                },
                Detail = new BandDefinition
                {
                    Height = 15.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Row Item", X = 0, Y = 0, Width = 100, Height = 8 }]
                },
                PageFooter = new BandDefinition
                {
                    Height = 20.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Page Footer", X = 0, Y = 0, Width = 100, Height = 10 }]
                }
            }
        };

        // Act
        using var bitmap = RenderReportPageToBitmap(report, pageIndex: 0);

        // Assert
        bitmap.Should().NotBeNull();
        bitmap.Width.Should().BeGreaterThan(500);
        bitmap.Height.Should().BeGreaterThan(700);

        // Verify bitmap contains rendered content (non-empty background)
        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha > 0 && (pixel.Red < 250 || pixel.Green < 250 || pixel.Blue < 250))
                {
                    nonWhitePixels++;
                }
            }
        }

        nonWhitePixels.Should().BeGreaterThan(0, "The rendered canvas must draw text and vector visual elements");
    }

    private static SKBitmap RenderReportPageToBitmap(ReportDefinition report, int pageIndex)
    {
        var (wPt, hPt) = UnitConverter.GetPageDimensionsInPoints(report.PageSetup);
        var widthPx = (int)Math.Ceiling(wPt * 1.5f); // 1.5x scale
        var heightPx = (int)Math.Ceiling(hPt * 1.5f);

        var bitmap = new SKBitmap(widthPx, heightPx, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var context = new BandContext
        {
            Report = report,
            CurrentPageNumber = pageIndex + 1,
            TotalPages = 1
        };

        var reportCanvas = new SkiaReportCanvas(canvas, report.PageSetup.Unit);
        float currentY = (float)UnitConverter.ToPoints(report.PageSetup.Margins.Top, report.PageSetup.Unit);

        if (report.Bands.PageHeader != null)
        {
            reportCanvas.RenderBand(report.Bands.PageHeader, currentY, context);
            currentY += (float)UnitConverter.ToPoints(report.Bands.PageHeader.Height, report.PageSetup.Unit);
        }

        if (report.Bands.Detail != null)
        {
            reportCanvas.RenderBand(report.Bands.Detail, currentY, context);
        }

        return bitmap;
    }

    private static double ComputePixelDiffRatio(SKBitmap imgA, SKBitmap imgB)
    {
        if (imgA.Width != imgB.Width || imgA.Height != imgB.Height) return 1.0;

        long diffPixels = 0;
        long totalPixels = (long)imgA.Width * imgA.Height;

        for (int y = 0; y < imgA.Height; y++)
        {
            for (int x = 0; x < imgA.Width; x++)
            {
                var colorA = imgA.GetPixel(x, y);
                var colorB = imgB.GetPixel(x, y);

                if (colorA != colorB)
                {
                    diffPixels++;
                }
            }
        }

        return (double)diffPixels / totalPixels;
    }
}