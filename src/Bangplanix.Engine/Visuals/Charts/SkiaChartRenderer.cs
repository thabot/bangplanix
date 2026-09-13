using System;
using System.Collections.Generic;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class SkiaChartRenderer
{
    public static void RenderChart(SKCanvas canvas, ChartDefinition chart, SKRect bounds, string? fontFamily)
    {
        if (chart == null) return;

        // Background / Border if needed
        using var bgPaint = new SKPaint { Color = SKColors.Transparent, Style = SKPaintStyle.Fill };
        canvas.DrawRect(bounds, bgPaint);

        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        var curTop = bounds.Top + 4f;
        var curBottom = bounds.Bottom - 4f;
        var curLeft = bounds.Left + 4f;
        var curRight = bounds.Right - 4f;

        // Render Title
        if (!string.IsNullOrWhiteSpace(chart.Title))
        {
            using var titlePaint = new SKPaint
            {
                Color = new SKColor(15, 23, 42),
                TextSize = 11f,
                FakeBoldText = true,
                IsAntialias = true,
                Typeface = typeface
            };
            var titleW = titlePaint.MeasureText(chart.Title);
            canvas.DrawText(chart.Title, bounds.MidX - (titleW / 2f), curTop + 10f, titlePaint);
            curTop += 16f;
        }

        // Render Subtitle
        if (!string.IsNullOrWhiteSpace(chart.Subtitle))
        {
            using var subPaint = new SKPaint
            {
                Color = new SKColor(100, 116, 139),
                TextSize = 8f,
                IsAntialias = true,
                Typeface = typeface
            };
            var subW = subPaint.MeasureText(chart.Subtitle);
            canvas.DrawText(chart.Subtitle, bounds.MidX - (subW / 2f), curTop + 7f, subPaint);
            curTop += 12f;
        }

        // Calculate Legend Area
        SKRect legendArea = SKRect.Empty;
        var hasLegend = chart.ShowLegend && chart.LegendPosition != LegendPosition.None && chart.Series.Count > 0;

        if (hasLegend)
        {
            var legendSize = 18f;
            switch (chart.LegendPosition)
            {
                case LegendPosition.Bottom:
                    legendArea = new SKRect(curLeft, curBottom - legendSize, curRight, curBottom);
                    curBottom -= (legendSize + 4f);
                    break;
                case LegendPosition.Top:
                    legendArea = new SKRect(curLeft, curTop, curRight, curTop + legendSize);
                    curTop += (legendSize + 4f);
                    break;
                case LegendPosition.Left:
                    legendArea = new SKRect(curLeft, curTop, curLeft + 60f, curBottom);
                    curLeft += 64f;
                    break;
                case LegendPosition.Right:
                    legendArea = new SKRect(curRight - 60f, curTop, curRight, curBottom);
                    curRight -= 64f;
                    break;
            }
        }

        // Plot Area with margins for axes
        var isPieOrGauge = chart.ChartType is ChartType.Pie or ChartType.Doughnut or ChartType.Gauge or ChartType.RadialSpeedometer or ChartType.Radar;
        var hasSecondaryAxis = chart.SecondaryYAxis != null || chart.ChartType == ChartType.Combo;
        var leftMargin = isPieOrGauge ? 10f : 28f;
        var bottomMargin = isPieOrGauge ? 10f : 20f;
        var rightMargin = hasSecondaryAxis ? 28f : 10f;
        var topMargin = 8f;

        var plotArea = new SKRect(curLeft + leftMargin, curTop + topMargin, curRight - rightMargin, curBottom - bottomMargin);
        if (plotArea.Width <= 10 || plotArea.Height <= 10) return;

        // Render Chart Geometry by Type
        switch (chart.ChartType)
        {
            case ChartType.Column:
            case ChartType.Bar:
            case ChartType.StackedColumn:
            case ChartType.StackedBar:
            case ChartType.PercentStackedColumn:
            case ChartType.PercentStackedBar:
                BarColumnChartRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;

            case ChartType.Line:
            case ChartType.Spline:
            case ChartType.StepLine:
            case ChartType.Area:
            case ChartType.SplineArea:
            case ChartType.StackedArea:
                LineAreaChartRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;

            case ChartType.Pie:
            case ChartType.Doughnut:
                PieDoughnutChartRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;

            case ChartType.Gauge:
            case ChartType.RadialSpeedometer:
            case ChartType.Bullet:
                GaugeAndBulletRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;

            case ChartType.Scatter:
            case ChartType.Bubble:
            case ChartType.Radar:
                ScatterBubbleRadarRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;

            case ChartType.Combo:
                ComboChartRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;

            case ChartType.Waterfall:
            case ChartType.Funnel:
                WaterfallFunnelChartRenderer.Render(canvas, chart, plotArea, fontFamily);
                break;
        }

        // Render Legend Items
        if (hasLegend && !legendArea.IsEmpty)
        {
            RenderLegend(canvas, chart, legendArea, fontFamily);
        }
    }

    private static void RenderLegend(SKCanvas canvas, ChartDefinition chart, SKRect legendArea, string? fontFamily)
    {
        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var textPaint = new SKPaint
        {
            Color = new SKColor(71, 85, 105),
            TextSize = 8f,
            IsAntialias = true,
            Typeface = typeface
        };
        using var swatchPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        var items = new List<(string Name, SKColor Color)>();

        if (chart.ChartType is ChartType.Pie or ChartType.Doughnut)
        {
            for (int i = 0; i < chart.Categories.Count; i++)
            {
                var col = ChartColorPalette.GetColor(chart.Palette, i, chart.CustomColors);
                items.Add((chart.Categories[i], col));
            }
        }
        else
        {
            for (int s = 0; s < chart.Series.Count; s++)
            {
                var ser = chart.Series[s];
                var col = ChartColorPalette.ParseColor(ser.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));
                var name = string.IsNullOrEmpty(ser.Name) ? $"Series {s + 1}" : ser.Name;
                items.Add((name, col));
            }
        }

        if (items.Count == 0) return;

        // Draw horizontal legend flow
        float totalWidth = items.Sum(it => 12f + textPaint.MeasureText(it.Name) + 10f);
        float startX = Math.Max(legendArea.Left, legendArea.MidX - (totalWidth / 2f));
        float curX = startX;
        float y = legendArea.MidY;

        foreach (var item in items)
        {
            // Swatch
            swatchPaint.Color = item.Color;
            canvas.DrawRoundRect(curX, y - 3.5f, 7f, 7f, 1.5f, 1.5f, swatchPaint);

            // Text
            canvas.DrawText(item.Name, curX + 11f, y + 2.5f, textPaint);
            curX += 11f + textPaint.MeasureText(item.Name) + 12f;

            if (curX > legendArea.Right - 10f) break;
        }
    }
}
