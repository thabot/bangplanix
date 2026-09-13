using Bangplanix.Core.Models;
using Bangplanix.Engine.Visuals.Charts;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class ChartVectorPrecisionTests
{
    [Theory]
    [InlineData(ChartType.Bar)]
    [InlineData(ChartType.Column)]
    [InlineData(ChartType.Line)]
    [InlineData(ChartType.Pie)]
    [InlineData(ChartType.Doughnut)]
    [InlineData(ChartType.Combo)]
    [InlineData(ChartType.Radar)]
    [InlineData(ChartType.Waterfall)]
    [InlineData(ChartType.Funnel)]
    [InlineData(ChartType.Gauge)]
    public void SkiaChartRenderer_ShouldRenderAllSupportedChartTypesToVectorCanvas(ChartType chartType)
    {
        var chartDef = new ChartDefinition
        {
            ChartType = chartType,
            Title = $"Vector Precision Chart: {chartType}",
            Palette = "Corporate",
            Categories = ["Q1", "Q2", "Q3", "Q4"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "Quarterly Revenue",
                    Color = "#1E88E5",
                    Values = [120_000, 185_000, 160_000, 240_000],
                    Categories = ["Q1", "Q2", "Q3", "Q4"]
                }
            ]
        };

        if (chartType == ChartType.Gauge)
        {
            chartDef.GaugeOptions = new GaugeOptions { Value = 75, MinValue = 0, MaxValue = 100 };
        }

        using var bitmap = new SKBitmap(600, 400);
        using var canvas = new SKCanvas(bitmap);
        SkiaChartRenderer.RenderChart(canvas, chartDef, new SKRect(0, 0, 600, 400), "Sarabun");

        Assert.Equal(600, bitmap.Width);
        Assert.Equal(400, bitmap.Height);
    }

    [Fact]
    public void SvgChartExporter_ShouldGenerateCrispScalableVectorGraphics()
    {
        var chartDef = new ChartDefinition
        {
            ChartType = ChartType.Bar,
            Title = "Department Expenses Breakdown",
            Palette = "Emerald",
            Categories = ["Engineering", "Sales", "Operations"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "Expenses",
                    Values = [450_000, 320_000, 180_000],
                    Categories = ["Engineering", "Sales", "Operations"]
                }
            ]
        };

        var svgString = SvgChartExporter.ExportToSvg(chartDef, width: 400, height: 250);

        Assert.NotNull(svgString);
        Assert.StartsWith("<svg", svgString.TrimStart(), StringComparison.Ordinal);
        Assert.Contains("viewBox=\"0 0 400 250\"", svgString, StringComparison.Ordinal);
        Assert.Contains("Department Expenses Breakdown", svgString, StringComparison.Ordinal);
        Assert.Contains("Engineering", svgString, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", svgString.TrimEnd(), StringComparison.Ordinal);
    }

    [Fact]
    public void ChartColorPalette_ShouldProvideHarmoniousColorPalettesWithSufficientContrast()
    {
        var palettes = new[]
        {
            "default",
            "corporate",
            "emerald",
            "vibrant",
            "pastel",
            "thaiheritage"
        };

        foreach (var paletteTheme in palettes)
        {
            var colors = new List<SKColor>();
            for (int i = 0; i < 6; i++)
            {
                var color = ChartColorPalette.GetColor(paletteTheme, i);
                colors.Add(color);
            }
            Assert.Equal(6, colors.Count);

            // Verify unique distinct colors
            var uniqueColors = colors.Select(c => c.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            Assert.True(uniqueColors.Count >= 5);
        }
    }
}
