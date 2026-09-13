using System;
using System.Collections.Generic;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class ComboChartRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;

        var categoryCount = chart.Categories.Count > 0 ? chart.Categories.Count : chart.Series.Max(s => s.Values.Count);
        if (categoryCount == 0) return;

        var primarySeries = chart.Series.Where(s => s.AxisTarget == AxisTarget.Primary).ToList();
        var secondarySeries = chart.Series.Where(s => s.AxisTarget == AxisTarget.Secondary).ToList();

        if (primarySeries.Count == 0 && secondarySeries.Count > 0)
        {
            primarySeries = secondarySeries;
            secondarySeries = [];
        }

        // 1. Calculate Primary Y Range (Left Axis)
        var minPrimary = chart.YAxis?.Min ?? 0.0;
        var maxPrimary = chart.YAxis?.Max ?? (primarySeries.SelectMany(s => s.Values).DefaultIfEmpty(0.0).Max() * 1.15);
        if (maxPrimary <= minPrimary) maxPrimary = minPrimary + 10.0;
        var primaryRange = maxPrimary - minPrimary;

        // 2. Calculate Secondary Y Range (Right Axis)
        double minSecondary = 0.0;
        double maxSecondary = 100.0;
        double secondaryRange = 100.0;
        var hasSecondary = secondarySeries.Count > 0;

        if (hasSecondary)
        {
            minSecondary = chart.SecondaryYAxis?.Min ?? 0.0;
            maxSecondary = chart.SecondaryYAxis?.Max ?? (secondarySeries.SelectMany(s => s.Values).DefaultIfEmpty(0.0).Max() * 1.15);
            if (maxSecondary <= minSecondary) maxSecondary = minSecondary + 10.0;
            secondaryRange = maxSecondary - minSecondary;
        }

        // 3. Draw Gridlines & Axes
        DrawAxesAndGrid(canvas, chart, plotArea, minPrimary, maxPrimary, minSecondary, maxSecondary, hasSecondary, categoryCount, fontFamily);

        // 4. Render Primary Bar/Column series
        var columnSeries = primarySeries.Where(s => (s.SeriesType ?? chart.ChartType) != ChartType.Line && (s.SeriesType ?? chart.ChartType) != ChartType.Spline).ToList();
        var lineSeries = chart.Series.Where(s => (s.SeriesType ?? chart.ChartType) == ChartType.Line || (s.SeriesType ?? chart.ChartType) == ChartType.Spline).ToList();

        // If no explicit line series, treat secondary as line
        if (lineSeries.Count == 0 && secondarySeries.Count > 0)
        {
            lineSeries = secondarySeries;
            columnSeries = primarySeries;
        }

        var groupWidth = plotArea.Width / categoryCount;
        var padding = groupWidth * 0.25f;
        var usableWidth = groupWidth - padding;
        var colWidth = columnSeries.Count > 0 ? usableWidth / columnSeries.Count : usableWidth;

        for (int c = 0; c < categoryCount; c++)
        {
            var groupLeft = plotArea.Left + (c * groupWidth) + (padding / 2f);

            for (int s = 0; s < columnSeries.Count; s++)
            {
                var series = columnSeries[s];
                if (c >= series.Values.Count) continue;

                var val = series.Values[c];
                var valFrac = (float)((val - minPrimary) / primaryRange);
                var h = plotArea.Height * Math.Clamp(valFrac, 0f, 1f);
                var x = groupLeft + (s * colWidth);
                var y = plotArea.Bottom - h;

                var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));
                using var barPaint = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
                var barRect = new SKRect(x, y, x + colWidth - 1f, y + h);
                if (barRect.Height > 0)
                {
                    canvas.DrawRoundRect(barRect, 2f, 2f, barPaint);
                }
            }
        }

        // 5. Render Line / Spline Series on Secondary (or Primary) Axis
        var xStep = categoryCount > 1 ? plotArea.Width / (categoryCount - 1) : plotArea.Width;
        var midColOffset = groupWidth / 2f;

        for (int s = 0; s < lineSeries.Count; s++)
        {
            var series = lineSeries[s];
            var isSec = series.AxisTarget == AxisTarget.Secondary;
            var minY = isSec ? minSecondary : minPrimary;
            var yR = isSec ? secondaryRange : primaryRange;

            var points = new List<SKPoint>();
            for (int c = 0; c < categoryCount; c++)
            {
                var val = c < series.Values.Count ? series.Values[c] : 0.0;
                var x = plotArea.Left + (c * groupWidth) + midColOffset;
                var valFrac = (float)((val - minY) / yR);
                var y = plotArea.Bottom - (plotArea.Height * Math.Clamp(valFrac, 0f, 1f));
                points.Add(new SKPoint(x, y));
            }

            var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, columnSeries.Count + s, chart.CustomColors));

            // Line Path
            using var linePath = new SKPath();
            linePath.MoveTo(points[0]);
            for (int i = 1; i < points.Count; i++) linePath.LineTo(points[i]);

            using var linePaint = new SKPaint
            {
                Color = color,
                StrokeWidth = 2.5f,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };
            canvas.DrawPath(linePath, linePaint);

            // Data Points
            using var ptFill = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var ptStroke = new SKPaint { Color = color, StrokeWidth = 2f, Style = SKPaintStyle.Stroke, IsAntialias = true };

            foreach (var pt in points)
            {
                canvas.DrawCircle(pt.X, pt.Y, 3.5f, ptFill);
                canvas.DrawCircle(pt.X, pt.Y, 3.5f, ptStroke);
            }
        }
    }

    private static void DrawAxesAndGrid(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, double minP, double maxP, double minS, double maxS, bool hasSec, int categoryCount, string? fontFamily)
    {
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

        var steps = 4;

        for (int i = 0; i <= steps; i++)
        {
            var frac = (float)i / steps;
            var y = plotArea.Bottom - (plotArea.Height * frac);

            // Gridline
            if (chart.YAxis?.ShowGridLines ?? true)
            {
                canvas.DrawLine(plotArea.Left, y, plotArea.Right, y, gridPaint);
            }

            // Left Primary Y Axis Labels
            var valP = minP + ((maxP - minP) * frac);
            var textP = valP >= 1000 ? $"{valP / 1000:N0}k" : $"{valP:N0}";
            var twP = labelPaint.MeasureText(textP);
            canvas.DrawText(textP, plotArea.Left - twP - 5f, y + 3f, labelPaint);

            // Right Secondary Y Axis Labels
            if (hasSec)
            {
                var valS = minS + ((maxS - minS) * frac);
                var textS = $"{valS:F0}%";
                canvas.DrawText(textS, plotArea.Right + 5f, y + 3f, labelPaint);
            }
        }

        // X Axis Category Ticks
        var groupWidth = plotArea.Width / categoryCount;
        for (int c = 0; c < categoryCount; c++)
        {
            if (c < chart.Categories.Count)
            {
                var catText = chart.Categories[c];
                var catW = labelPaint.MeasureText(catText);
                var catX = plotArea.Left + (c * groupWidth) + (groupWidth / 2f) - (catW / 2f);
                canvas.DrawText(catText, catX, plotArea.Bottom + 14f, labelPaint);
            }
        }
    }
}
