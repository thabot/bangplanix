using BenchmarkDotNet.Attributes;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Visuals.Charts;
using SkiaSharp;

namespace Bangplanix.Benchmarks;

[MemoryDiagnoser]
public class ChartRenderingBenchmarks
{
    private ChartDefinition _columnChart = null!;
    private ChartDefinition _pieChart = null!;
    private ChartDefinition _lineChart = null!;
    private SKBitmap _bitmap = null!;
    private SKCanvas _canvas = null!;
    private SKRect _bounds;

    [GlobalSetup]
    public void Setup()
    {
        _bounds = new SKRect(0, 0, 800, 500);
        _bitmap = new SKBitmap(800, 500);
        _canvas = new SKCanvas(_bitmap);

        _columnChart = new ChartDefinition
        {
            ChartType = ChartType.Column,
            Title = "Quarterly Financial Analysis",
            Palette = "Corporate",
            Categories = ["Q1", "Q2", "Q3", "Q4"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "Revenue",
                    Values = [120_000, 180_000, 150_000, 240_000],
                    Categories = ["Q1", "Q2", "Q3", "Q4"]
                },
                new ChartSeriesDefinition
                {
                    Name = "Expenses",
                    Values = [80_000, 95_000, 90_000, 110_000],
                    Categories = ["Q1", "Q2", "Q3", "Q4"]
                }
            ]
        };

        _pieChart = new ChartDefinition
        {
            ChartType = ChartType.Pie,
            Title = "Market Share by Division",
            Palette = "Vibrant",
            Categories = ["North", "South", "East", "West", "Central"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Values = [35, 25, 15, 15, 10],
                    Categories = ["North", "South", "East", "West", "Central"]
                }
            ]
        };

        _lineChart = new ChartDefinition
        {
            ChartType = ChartType.Line,
            Title = "Monthly Active Users Growth",
            Palette = "Emerald",
            Categories = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "2026 Active Users",
                    Values = [1000, 1500, 2200, 3100, 4200, 5800, 7200, 8900, 11000, 13500, 16200, 20000],
                    Categories = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
                }
            ]
        };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void Render_ColumnChart_VectorCanvas()
    {
        SkiaChartRenderer.RenderChart(_canvas, _columnChart, _bounds, "Sarabun");
    }

    [Benchmark]
    public void Render_PieChart_VectorCanvas()
    {
        SkiaChartRenderer.RenderChart(_canvas, _pieChart, _bounds, "Sarabun");
    }

    [Benchmark]
    public void Render_LineChart_VectorCanvas()
    {
        SkiaChartRenderer.RenderChart(_canvas, _lineChart, _bounds, "Sarabun");
    }

    [Benchmark]
    public string Export_Svg_ColumnChart()
    {
        return SvgChartExporter.ExportToSvg(_columnChart, 800, 500);
    }
}
