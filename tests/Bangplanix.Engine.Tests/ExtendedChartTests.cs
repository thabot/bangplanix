using System;
using System.Collections.Generic;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Visuals.Charts;
using FluentAssertions;
using SkiaSharp;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Engine.Tests;

public class ExtendedChartTests
{
    private static SKBitmap CreateBitmap(int width = 800, int height = 600) =>
        new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

    [Fact]
    public void ChartDataBinder_AggregateDatasetRows_ShouldGroupByCategoriesAndSeries()
    {
        var rawRows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Quarter"] = "Q1", ["Category"] = "Software", ["Revenue"] = 1000.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q1", ["Category"] = "Software", ["Revenue"] = 500.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q1", ["Category"] = "Hardware", ["Revenue"] = 800.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q2", ["Category"] = "Software", ["Revenue"] = 1200.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q2", ["Category"] = "Hardware", ["Revenue"] = 900.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q3", ["Category"] = "Software", ["Revenue"] = 1500.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q3", ["Category"] = "Hardware", ["Revenue"] = 1100.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q4", ["Category"] = "Software", ["Revenue"] = 2000.0 },
            new Dictionary<string, object?> { ["Quarter"] = "Q4", ["Category"] = "Hardware", ["Revenue"] = 1400.0 },
        };

        var chart = new ChartDefinition
        {
            ChartType = ChartType.Column,
            DataBinding = new ChartDataBindingDefinition
            {
                CategoryField = "Quarter",
                ValueField = "Revenue",
                SeriesGroupField = "Category",
                AggregateFunction = ChartAggregateFunction.Sum
            }
        };

        ChartDataBinder.Bind(chart, rawRows);

        chart.Categories.Should().Equal("Q1", "Q2", "Q3", "Q4");
        chart.Series.Should().HaveCount(2);

        var softwareSeries = chart.Series.Find(s => s.Name == "Software");
        softwareSeries.Should().NotBeNull();
        softwareSeries!.Values.Should().Equal(1500.0, 1200.0, 1500.0, 2000.0);

        var hardwareSeries = chart.Series.Find(s => s.Name == "Hardware");
        hardwareSeries.Should().NotBeNull();
        hardwareSeries!.Values.Should().Equal(800.0, 900.0, 1100.0, 1400.0);
    }

    [Fact]
    public void ComboChart_DualAxisRendering_ShouldRenderColumnsAndLinesWithSecondaryAxis()
    {
        using var bitmap = CreateBitmap(800, 500);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = ChartType.Combo,
            Title = "รายได้และอัตรากำไร (Revenue vs Profit Margin %)",
            Categories = ["2022", "2023", "2024", "2025", "2026"],
            YAxis = new ChartAxisDefinition { Title = "Revenue (THB)" },
            SecondaryYAxis = new ChartAxisDefinition { Title = "Margin (%)", Min = 0, Max = 50 },
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "Revenue",
                    SeriesType = ChartType.Column,
                    AxisTarget = AxisTarget.Primary,
                    Values = [5000000, 6800000, 8200000, 11000000, 14500000],
                    Color = "#3b82f6"
                },
                new ChartSeriesDefinition
                {
                    Name = "Profit Margin %",
                    SeriesType = ChartType.Line,
                    AxisTarget = AxisTarget.Secondary,
                    Values = [18.5, 22.0, 24.5, 29.0, 33.5],
                    Color = "#ef4444"
                }
            ],
            ShowLegend = true,
            LegendPosition = LegendPosition.Bottom
        };

        var bounds = new SKRect(20, 20, 780, 480);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 5)
        {
            for (int x = 0; x < bitmap.Width; x += 5)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(100);
    }

    [Fact]
    public void WaterfallChart_FinancialBridge_ShouldRenderFloatingBarsAndConnectors()
    {
        using var bitmap = CreateBitmap(800, 450);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = ChartType.Waterfall,
            Title = "งบกำไรขาดทุนสะพานการเงิน (Financial P&L Waterfall)",
            Categories = ["รายได้รวม", "ต้นทุนขาย", "ค่าการตลาด", "ค่าบริหาร", "ดอกเบี้ย", "กำไรสุทธิ"],
            WaterfallOptions = new WaterfallOptions
            {
                PositiveColor = "#10b981",
                NegativeColor = "#ef4444",
                TotalColor = "#2563eb",
                ShowConnectorLines = true
            },
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "P&L Steps",
                    Values = [1000000, -400000, -150000, -120000, -30000, 300000],
                    DataPoints =
                    [
                        new ChartDataPoint { Label = "Start" },
                        new ChartDataPoint { Label = "COGS" },
                        new ChartDataPoint { Label = "Marketing" },
                        new ChartDataPoint { Label = "Admin" },
                        new ChartDataPoint { Label = "Interest" },
                        new ChartDataPoint { Label = "Net Profit", IsTotal = true }
                    ]
                }
            ]
        };

        var bounds = new SKRect(20, 20, 780, 430);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 5)
        {
            for (int x = 0; x < bitmap.Width; x += 5)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(80);
    }

    [Fact]
    public void FunnelChart_SalesConversion_ShouldRenderTrapezoidalStages()
    {
        using var bitmap = CreateBitmap(600, 500);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var chart = new ChartDefinition
        {
            ChartType = ChartType.Funnel,
            Title = "กระบวนการแปลงยอดขาย (Sales Pipeline Funnel)",
            Categories = ["ผู้เข้าชมเว็บ (Visitors)", "ลงทะเบียนทดลอง (Leads)", "นัดหมายสาธิต (Demos)", "ส่งใบเสนอราคา (Quotes)", "ปิดการขาย (Won Deals)"],
            FunnelOptions = new FunnelOptions
            {
                NeckWidthRatio = 0.25f,
                ShowConversionRates = true
            },
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "Leads",
                    Values = [10000, 3500, 1200, 480, 150]
                }
            ]
        };

        var bounds = new SKRect(20, 20, 580, 480);
        SkiaChartRenderer.RenderChart(canvas, chart, bounds, "Sarabun");

        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 5)
        {
            for (int x = 0; x < bitmap.Width; x += 5)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(100);
    }

    [Fact]
    public void SvgChartExporter_ShouldGenerateValidInteractiveSvgWithTooltips()
    {
        var chart = new ChartDefinition
        {
            ChartType = ChartType.Column,
            Title = "ยอดสั่งซื้อรายภาค (Regional Sales)",
            Categories = ["ภาคเหนือ", "ภาคกลาง", "ภาคใต้", "ภาคอีสาน"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Name = "2026",
                    Values = [450000, 890000, 620000, 710000]
                }
            ]
        };

        var svg = SvgChartExporter.ExportToSvg(chart, 600, 400);

        svg.Should().StartWith("<svg");
        svg.TrimEnd().Should().EndWith("</svg>");
        svg.Should().Contain("bpx-chart-node");
        svg.Should().Contain("data-tooltip=\"2026 - ภาคเหนือ: 450,000\"");
        svg.Should().Contain("data-tooltip=\"2026 - ภาคกลาง: 890,000\"");
        svg.Should().Contain("<rect");
    }

    [Fact]
    public void SvgChartExporter_PieChart_ShouldGenerateInteractivePathSlices()
    {
        var chart = new ChartDefinition
        {
            ChartType = ChartType.Pie,
            Title = "สัดส่วนช่องทางชำระเงิน",
            Categories = ["PromptPay QR", "Credit Card", "Bank Transfer"],
            Series =
            [
                new ChartSeriesDefinition
                {
                    Values = [6500, 2500, 1000]
                }
            ]
        };

        var svg = SvgChartExporter.ExportToSvg(chart, 500, 400);

        svg.Should().Contain("<path d=\"M");
        svg.Should().Contain("data-tooltip=\"PromptPay QR: 6,500 (65.0%)\"");
        svg.Should().Contain("<title>PromptPay QR: 6,500 (65.0%)</title>");
    }
}
