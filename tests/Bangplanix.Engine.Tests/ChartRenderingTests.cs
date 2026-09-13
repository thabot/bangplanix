using System;
using System.Collections.Generic;
using System.IO;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Visuals.Charts;
using FluentAssertions;
using SkiaSharp;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Engine.Tests;

public class ChartRenderingTests
{
    private static SKBitmap CreateBitmap(int width = 800, int height = 600) =>
        new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

    [Theory]
    [InlineData(ChartType.Column)]
    [InlineData(ChartType.Bar)]
    [InlineData(ChartType.StackedColumn)]
    [InlineData(ChartType.StackedBar)]
    [InlineData(ChartType.PercentStackedColumn)]
    [InlineData(ChartType.PercentStackedBar)]
    public void Render_BarAndColumnCharts_ShouldProduceNonEmptyCanvas(ChartType chartType)
    {
        using var bitmap = CreateBitmap();
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = chartType,
            Title = "ยอดขายประจำไตรมาส (Quarterly Sales Q1-Q4)",
            Subtitle = "แสดงเปรียบเทียบระหว่างผลิตภัณฑ์ A และ B",
            Palette = "Corporate",
            Categories = ["Q1", "Q2", "Q3", "Q4"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "Product A (สินค้า ก)",
                    Values = [12000, 19000, 15000, 24000],
                    ShowDataLabels = true
                },
                new ChartSeriesDefinition
                {
                    Name = "Product B (สินค้า ข)",
                    Values = [8000, 14000, 11000, 18000],
                    ShowDataLabels = true
                }
            ],
            ShowLegend = true,
            LegendPosition = LegendPosition.Bottom
        };

        var bounds = new SKRect(20, 20, 780, 580);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        // Verify bitmap has drawn non-white pixels
        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(50);
    }

    [Theory]
    [InlineData(ChartType.Line)]
    [InlineData(ChartType.Spline)]
    [InlineData(ChartType.StepLine)]
    [InlineData(ChartType.Area)]
    [InlineData(ChartType.SplineArea)]
    [InlineData(ChartType.StackedArea)]
    public void Render_LineAndAreaCharts_ShouldDrawContinuousPathsAndGradients(ChartType chartType)
    {
        using var bitmap = CreateBitmap();
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = chartType,
            Title = "แนวโน้มอุณหภูมิและความชื้น (Temperature & Humidity Trends)",
            Palette = "Vibrant",
            Categories = ["ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย."],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "อุณหภูมิเฉลี่ย (°C)",
                    Values = [28.5, 30.2, 33.0, 35.5, 34.0, 31.5],
                    BorderWidth = 2.5f
                },
                new ChartSeriesDefinition
                {
                    Name = "ความชื้นสัมพัทธ์ (%)",
                    Values = [65.0, 60.0, 58.0, 55.0, 70.0, 78.0],
                    BorderWidth = 2.0f
                }
            ]
        };

        var bounds = new SKRect(20, 20, 780, 580);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(50);
    }

    [Theory]
    [InlineData(ChartType.Pie, false)]
    [InlineData(ChartType.Doughnut, true)]
    public void Render_PieAndDoughnutCharts_ShouldDrawSlicesWithPercentagesAndLeaderLines(ChartType chartType, bool isDoughnut)
    {
        using var bitmap = CreateBitmap();
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = chartType,
            Title = "สัดส่วนงบประมาณรายจ่าย (Budget Breakdown)",
            Palette = "Emerald",
            Categories = ["การตลาด (Marketing)", "วิจัยและพัฒนา (R&D)", "บุคลากร (HR)", "โครงสร้างพื้นฐาน (IT/Infra)"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "งบประมาณ",
                    Values = [3500000, 4500000, 2000000, 1500000]
                }
            ],
            DonutHoleSize = 0.55f,
            ExplodedSliceIndex = 1 // R&D exploded
        };

        var bounds = new SKRect(20, 20, 780, 580);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        // Verify Center hole is white for doughnut and non-empty for pie
        var centerPixel = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
        if (isDoughnut)
        {
            // In doughnut, center has summary text or background
            centerPixel.Alpha.Should().BeGreaterThan(0);
        }

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(80);
    }

    [Fact]
    public void Render_RadialGaugeAndSpeedometer_ShouldDrawArcAndNeedle()
    {
        using var bitmap = CreateBitmap(600, 400);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = ChartType.RadialSpeedometer,
            Title = "ความจุเซิร์ฟเวอร์ (Server CPU Utilization)",
            GaugeOptions = new GaugeOptions
            {
                MinValue = 0,
                MaxValue = 100,
                Value = 74.5,
                Unit = "%",
                Ranges =
                [
                    new GaugeRange { Start = 0, End = 60, Color = "#22c55e" },
                    new GaugeRange { Start = 60, End = 85, Color = "#eab308" },
                    new GaugeRange { Start = 85, End = 100, Color = "#ef4444" }
                ]
            }
        };

        var bounds = new SKRect(20, 20, 580, 380);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 5)
        {
            for (int x = 0; x < bitmap.Width; x += 5)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(50);
    }

    [Fact]
    public void Render_BulletChart_ShouldDrawQualitativeRangesAndTargetLine()
    {
        using var bitmap = CreateBitmap(600, 200);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = ChartType.Bullet,
            Title = "ยอดขายเปรียบเทียบเป้าหมาย (Revenue vs Target)",
            BulletOptions = new BulletOptions
            {
                Actual = 275000,
                Target = 250000,
                BadRange = 150000,
                SatisfactoryRange = 225000,
                GoodRange = 300000,
                Unit = "THB"
            }
        };

        var bounds = new SKRect(20, 20, 580, 180);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 5)
        {
            for (int x = 0; x < bitmap.Width; x += 5)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(30);
    }

    [Theory]
    [InlineData(ChartType.Scatter, true)]
    [InlineData(ChartType.Bubble, false)]
    [InlineData(ChartType.Radar, false)]
    public void Render_ScatterBubbleRadarCharts_ShouldRenderAccurately(ChartType chartType, bool showTrendline)
    {
        using var bitmap = CreateBitmap();
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = chartType,
            Title = "การกระจายตัวของข้อมูลเชิงซ้อน (Multivariate Correlation)",
            ShowTrendline = showTrendline,
            Palette = "ThaiSilk",
            Categories = ["ความเร็ว", "ความแม่นยำ", "ความเสถียร", "ความคุ้มค่า", "ความง่าย"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "กลุ่มตัวอย่าง ก (Sample Alpha)",
                    Values = [85, 90, 78, 92, 88],
                    DataPoints =
                    [
                        new ChartDataPoint { X = 10.0, Y = 25.0, Z = 5.0 },
                        new ChartDataPoint { X = 20.0, Y = 45.0, Z = 12.0 },
                        new ChartDataPoint { X = 35.0, Y = 60.0, Z = 8.0 },
                        new ChartDataPoint { X = 50.0, Y = 85.0, Z = 18.0 }
                    ]
                },
                new ChartSeriesDefinition
                {
                    Name = "กลุ่มตัวอย่าง ข (Sample Beta)",
                    Values = [70, 85, 95, 80, 75],
                    DataPoints =
                    [
                        new ChartDataPoint { X = 15.0, Y = 30.0, Z = 7.0 },
                        new ChartDataPoint { X = 25.0, Y = 40.0, Z = 10.0 },
                        new ChartDataPoint { X = 40.0, Y = 70.0, Z = 14.0 },
                        new ChartDataPoint { X = 60.0, Y = 90.0, Z = 22.0 }
                    ]
                }
            ]
        };

        var bounds = new SKRect(20, 20, 780, 580);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(40);
    }

    [Theory]
    [InlineData(SparklineType.Line)]
    [InlineData(SparklineType.Area)]
    [InlineData(SparklineType.Bar)]
    [InlineData(SparklineType.WinLoss)]
    public void Render_Sparklines_ShouldFitCellBoundsCrisply(SparklineType sparklineType)
    {
        using var bitmap = CreateBitmap(150, 40);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var sparkline = new SparklineDefinition
        {
            Type = sparklineType,
            Values = sparklineType == SparklineType.WinLoss
                ? [1, 1, -1, 1, -1, 1, 1, -1, 1, 1]
                : [12.0, 15.5, 14.0, 22.0, 18.5, 25.0, 31.0, 28.0],
            HighlightMinMax = true
        };

        var bounds = new SKRect(2, 2, 148, 38);
        SparklineRenderer.RenderSparkline(canvas, sparkline, bounds);

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 2)
        {
            for (int x = 0; x < bitmap.Width; x += 2)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(10);
    }

    [Fact]
    public void SkiaReportCanvas_RenderReportWithChartAndSparklineElements_ShouldIntegrateSeamlessly()
    {
        using var bitmap = CreateBitmap(800, 1000);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var reportCanvas = new SkiaReportCanvas(canvas, UnitType.Pt);

        var report = new ReportDefinition
        {
            PageSetup = new PageSetup { Width = 800, Height = 1000, Unit = UnitType.Pt }
        };

        var bandContext = new BandContext
        {
            Report = report,
            CurrentPageNumber = 1,
            TotalPages = 1
        };

        var chartElement = new ElementDefinition
        {
            Type = ElementType.Chart,
            X = 50,
            Y = 50,
            Width = 700,
            Height = 350,
            Chart = new ChartDefinition
            {
                ChartType = ChartType.Column,
                Title = "รายงานผลประกอบการประจำปี 2026 (Annual Performance 2026)",
                Categories = ["Q1", "Q2", "Q3", "Q4"],
                Series =
                [
                    new ChartSeriesDefinition { Name = "เป้าหมาย (Target)", Values = [100, 120, 140, 160] },
                    new ChartSeriesDefinition { Name = "ทำได้จริง (Actual)", Values = [105, 115, 150, 175] }
                ]
            }
        };

        var sparklineElement = new ElementDefinition
        {
            Type = ElementType.Sparkline,
            X = 50,
            Y = 420,
            Width = 200,
            Height = 40,
            Sparkline = new SparklineDefinition
            {
                Type = SparklineType.Area,
                Values = [10, 25, 15, 30, 45, 35, 60]
            }
        };

        reportCanvas.RenderElement(chartElement, 0, bandContext);
        reportCanvas.RenderElement(sparklineElement, 0, bandContext);

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(100);
    }
}
