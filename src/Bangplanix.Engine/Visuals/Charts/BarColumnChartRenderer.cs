using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class BarColumnChartRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;

        var isHorizontal = chart.ChartType is ChartType.Bar or ChartType.StackedBar or ChartType.PercentStackedBar;
        var isStacked = chart.ChartType is ChartType.StackedColumn or ChartType.StackedBar;
        var is100Percent = chart.ChartType is ChartType.PercentStackedColumn or ChartType.PercentStackedBar;

        if (isHorizontal)
        {
            RenderHorizontalBar(canvas, chart, plotArea, isStacked, is100Percent, fontFamily);
        }
        else
        {
            RenderVerticalColumn(canvas, chart, plotArea, isStacked, is100Percent, fontFamily);
        }
    }

    private static void RenderVerticalColumn(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, bool isStacked, bool is100Percent, string? fontFamily)
    {
        var categoryCount = chart.Categories.Count > 0 ? chart.Categories.Count : chart.Series.Max(s => s.Values.Count);
        if (categoryCount == 0) return;

        // Calculate Y scale
        double minY = 0.0;
        double maxY = 1.0;

        if (is100Percent)
        {
            minY = 0.0;
            maxY = 100.0;
        }
        else if (isStacked)
        {
            minY = 0.0;
            for (int c = 0; c < categoryCount; c++)
            {
                double sum = 0.0;
                for (int s = 0; s < chart.Series.Count; s++)
                {
                    if (c < chart.Series[s].Values.Count) sum += Math.Max(0, chart.Series[s].Values[c]);
                }
                if (sum > maxY) maxY = sum;
            }
        }
        else
        {
            minY = chart.YAxis?.Min ?? 0.0;
            var maxVal = chart.Series.SelectMany(s => s.Values).DefaultIfEmpty(0.0).Max();
            maxY = chart.YAxis?.Max ?? (maxVal > 0 ? maxVal * 1.15 : 10.0);
        }

        var yRange = maxY - minY;
        if (yRange <= 0.0001) yRange = 1.0;

        // Gridlines & Y Axis Ticks
        DrawYGridLinesAndLabels(canvas, chart, plotArea, minY, maxY, is100Percent, fontFamily);

        // Render Columns
        var groupWidth = plotArea.Width / categoryCount;
        var padding = groupWidth * 0.2f;
        var usableWidth = groupWidth - padding;
        var seriesCount = chart.Series.Count;
        var colWidth = isStacked || is100Percent ? usableWidth : usableWidth / seriesCount;

        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(100, 116, 139),
            TextSize = 9f,
            IsAntialias = true,
            Typeface = typeface
        };

        for (int c = 0; c < categoryCount; c++)
        {
            var groupLeft = plotArea.Left + (c * groupWidth) + (padding / 2f);

            // Draw Category label on X Axis
            if (c < chart.Categories.Count)
            {
                var catText = chart.Categories[c];
                var catWidth = labelPaint.MeasureText(catText);
                var catX = groupLeft + (usableWidth / 2f) - (catWidth / 2f);
                var catY = plotArea.Bottom + 14f;
                canvas.DrawText(catText, catX, catY, labelPaint);
            }

            // Stack accumulator
            double currentStackY = 0.0;
            double catTotal = 0.0;
            if (is100Percent)
            {
                for (int s = 0; s < seriesCount; s++)
                {
                    if (c < chart.Series[s].Values.Count) catTotal += Math.Max(0, chart.Series[s].Values[c]);
                }
                if (catTotal <= 0.0001) catTotal = 1.0;
            }

            for (int s = 0; s < seriesCount; s++)
            {
                var series = chart.Series[s];
                if (c >= series.Values.Count) continue;

                var rawValue = series.Values[c];
                var normalizedValue = is100Percent ? (rawValue / catTotal) * 100.0 : rawValue;

                float x, y, w, h;
                w = colWidth;

                if (isStacked || is100Percent)
                {
                    x = groupLeft;
                    var baseVal = currentStackY;
                    currentStackY += normalizedValue;

                    var bottomFrac = (float)((baseVal - minY) / yRange);
                    var topFrac = (float)((currentStackY - minY) / yRange);

                    var bottomY = plotArea.Bottom - (plotArea.Height * bottomFrac);
                    var topY = plotArea.Bottom - (plotArea.Height * topFrac);

                    y = topY;
                    h = bottomY - topY;
                }
                else
                {
                    x = groupLeft + (s * colWidth);
                    var valFrac = (float)((normalizedValue - minY) / yRange);
                    h = plotArea.Height * Math.Clamp(valFrac, 0f, 1f);
                    y = plotArea.Bottom - h;
                }

                var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));
                using var barPaint = new SKPaint
                {
                    Color = color,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                var barRect = new SKRect(x + 1f, y, x + w - 1f, y + h);
                if (barRect.Height > 0)
                {
                    canvas.DrawRoundRect(barRect, 2f, 2f, barPaint);

                    if (series.BorderWidth > 0 && !string.IsNullOrEmpty(series.BorderColor))
                    {
                        using var borderPaint = new SKPaint
                        {
                            Color = ChartColorPalette.ParseColor(series.BorderColor, SKColors.DarkSlateGray),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = series.BorderWidth,
                            IsAntialias = true
                        };
                        canvas.DrawRoundRect(barRect, 2f, 2f, borderPaint);
                    }
                }

                // Data Label
                if (series.ShowDataLabels && h > 12f)
                {
                    var format = series.DataLabelFormat ?? (is100Percent ? "{0:F0}%" : "{0:N0}");
                    var text = string.Format(CultureInfo.InvariantCulture, format, rawValue);
                    using var dataLabelPaint = new SKPaint
                    {
                        Color = SKColors.White,
                        TextSize = 8f,
                        IsAntialias = true,
                        Typeface = typeface
                    };
                    var tWidth = dataLabelPaint.MeasureText(text);
                    if (tWidth < w)
                    {
                        canvas.DrawText(text, x + (w / 2f) - (tWidth / 2f), y + (h / 2f) + 3f, dataLabelPaint);
                    }
                }
            }
        }
    }

    private static void RenderHorizontalBar(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, bool isStacked, bool is100Percent, string? fontFamily)
    {
        var categoryCount = chart.Categories.Count > 0 ? chart.Categories.Count : chart.Series.Max(s => s.Values.Count);
        if (categoryCount == 0) return;

        double minX = 0.0;
        double maxX = 1.0;

        if (is100Percent)
        {
            minX = 0.0;
            maxX = 100.0;
        }
        else if (isStacked)
        {
            minX = 0.0;
            for (int c = 0; c < categoryCount; c++)
            {
                double sum = 0.0;
                for (int s = 0; s < chart.Series.Count; s++)
                {
                    if (c < chart.Series[s].Values.Count) sum += Math.Max(0, chart.Series[s].Values[c]);
                }
                if (sum > maxX) maxX = sum;
            }
        }
        else
        {
            minX = chart.XAxis?.Min ?? 0.0;
            var maxVal = chart.Series.SelectMany(s => s.Values).DefaultIfEmpty(0.0).Max();
            maxX = chart.XAxis?.Max ?? (maxVal > 0 ? maxVal * 1.15 : 10.0);
        }

        var xRange = maxX - minX;
        if (xRange <= 0.0001) xRange = 1.0;

        var rowHeight = plotArea.Height / categoryCount;
        var padding = rowHeight * 0.2f;
        var usableHeight = rowHeight - padding;
        var seriesCount = chart.Series.Count;
        var barHeight = isStacked || is100Percent ? usableHeight : usableHeight / seriesCount;

        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(100, 116, 139),
            TextSize = 9f,
            IsAntialias = true,
            Typeface = typeface
        };

        for (int c = 0; c < categoryCount; c++)
        {
            var rowTop = plotArea.Top + (c * rowHeight) + (padding / 2f);

            // Draw Category label on Y Axis
            if (c < chart.Categories.Count)
            {
                var catText = chart.Categories[c];
                var catWidth = labelPaint.MeasureText(catText);
                var catX = plotArea.Left - catWidth - 6f;
                var catY = rowTop + (usableHeight / 2f) + 3f;
                canvas.DrawText(catText, catX, catY, labelPaint);
            }

            double currentStackX = 0.0;
            double catTotal = 0.0;
            if (is100Percent)
            {
                for (int s = 0; s < seriesCount; s++)
                {
                    if (c < chart.Series[s].Values.Count) catTotal += Math.Max(0, chart.Series[s].Values[c]);
                }
                if (catTotal <= 0.0001) catTotal = 1.0;
            }

            for (int s = 0; s < seriesCount; s++)
            {
                var series = chart.Series[s];
                if (c >= series.Values.Count) continue;

                var rawValue = series.Values[c];
                var normalizedValue = is100Percent ? (rawValue / catTotal) * 100.0 : rawValue;

                float x, y, w, h;
                h = barHeight;

                if (isStacked || is100Percent)
                {
                    y = rowTop;
                    var baseVal = currentStackX;
                    currentStackX += normalizedValue;

                    var leftFrac = (float)((baseVal - minX) / xRange);
                    var rightFrac = (float)((currentStackX - minX) / xRange);

                    var leftX = plotArea.Left + (plotArea.Width * leftFrac);
                    var rightX = plotArea.Left + (plotArea.Width * rightFrac);

                    x = leftX;
                    w = rightX - leftX;
                }
                else
                {
                    y = rowTop + (s * barHeight);
                    var valFrac = (float)((normalizedValue - minX) / xRange);
                    w = plotArea.Width * Math.Clamp(valFrac, 0f, 1f);
                    x = plotArea.Left;
                }

                var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));
                using var barPaint = new SKPaint
                {
                    Color = color,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                var barRect = new SKRect(x, y + 1f, x + w, y + h - 1f);
                if (barRect.Width > 0)
                {
                    canvas.DrawRoundRect(barRect, 2f, 2f, barPaint);
                }
            }
        }
    }

    private static void DrawYGridLinesAndLabels(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, double minY, double maxY, bool is100Percent, string? fontFamily)
    {
        var steps = 4;
        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var gridPaint = new SKPaint
        {
            Color = new SKColor(226, 232, 240),
            StrokeWidth = 1f,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(148, 163, 184),
            TextSize = 8f,
            IsAntialias = true,
            Typeface = typeface
        };

        for (int i = 0; i <= steps; i++)
        {
            var frac = (float)i / steps;
            var y = plotArea.Bottom - (plotArea.Height * frac);
            var val = minY + ((maxY - minY) * frac);

            if (chart.YAxis?.ShowGridLines ?? true)
            {
                canvas.DrawLine(plotArea.Left, y, plotArea.Right, y, gridPaint);
            }

            var text = is100Percent ? $"{val:F0}%" : (val >= 1000 ? $"{val / 1000:N0}k" : $"{val:N0}");
            var textWidth = labelPaint.MeasureText(text);
            canvas.DrawText(text, plotArea.Left - textWidth - 5f, y + 3f, labelPaint);
        }
    }
}
