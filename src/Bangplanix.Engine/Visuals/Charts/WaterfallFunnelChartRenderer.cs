using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class WaterfallFunnelChartRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.ChartType == ChartType.Waterfall)
        {
            RenderWaterfall(canvas, chart, plotArea, fontFamily);
        }
        else if (chart.ChartType == ChartType.Funnel)
        {
            RenderFunnel(canvas, chart, plotArea, fontFamily);
        }
    }

    private static void RenderWaterfall(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;
        var series = chart.Series[0];
        var count = series.Values.Count;
        if (count == 0) return;

        var options = chart.WaterfallOptions ?? new WaterfallOptions();
        var posColor = ChartColorPalette.ParseColor(options.PositiveColor, new SKColor(16, 185, 129));
        var negColor = ChartColorPalette.ParseColor(options.NegativeColor, new SKColor(239, 68, 68));
        var totalColor = ChartColorPalette.ParseColor(options.TotalColor, new SKColor(59, 130, 246));
        var connColor = ChartColorPalette.ParseColor(options.ConnectorColor, new SKColor(148, 163, 184));

        // Calculate Min and Max Cumulative Values
        double runningTotal = 0.0;
        double minVal = 0.0;
        double maxVal = 0.0;

        var barData = new List<(double Start, double End, double Delta, bool IsTotal)>();

        for (int i = 0; i < count; i++)
        {
            var delta = series.Values[i];
            var isTotal = i < series.DataPoints.Count && (series.DataPoints[i].IsTotal || series.DataPoints[i].IsSubtotal);

            double start, end;
            if (isTotal)
            {
                start = 0.0;
                end = runningTotal;
            }
            else
            {
                start = runningTotal;
                end = runningTotal + delta;
                runningTotal = end;
            }

            minVal = Math.Min(minVal, Math.Min(start, end));
            maxVal = Math.Max(maxVal, Math.Max(start, end));

            barData.Add((start, end, delta, isTotal));
        }

        if (maxVal <= minVal) maxVal = minVal + 10.0;
        var yRange = maxVal - minVal;

        // Draw Y Axis Gridlines
        DrawWaterfallGrid(canvas, plotArea, minVal, maxVal, fontFamily);

        var colWidth = plotArea.Width / count;
        var barPadding = colWidth * 0.2f;
        var actualBarW = colWidth - barPadding;

        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var barPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        using var textPaint = new SKPaint { Color = new SKColor(51, 65, 85), TextSize = 8.5f, IsAntialias = true, Typeface = typeface };
        using var connPaint = new SKPaint
        {
            Color = connColor,
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash([3f, 3f], 0f),
            IsAntialias = true
        };

        float? prevConnectorY = null;
        float? prevConnectorX = null;

        for (int i = 0; i < count; i++)
        {
            var (start, end, delta, isTotal) = barData[i];
            var x = plotArea.Left + (i * colWidth) + (barPadding / 2f);

            var topVal = Math.Max(start, end);
            var bottomVal = Math.Min(start, end);

            var topFrac = (float)((topVal - minVal) / yRange);
            var bottomFrac = (float)((bottomVal - minVal) / yRange);

            var yTop = plotArea.Bottom - (plotArea.Height * Math.Clamp(topFrac, 0f, 1f));
            var yBottom = plotArea.Bottom - (plotArea.Height * Math.Clamp(bottomFrac, 0f, 1f));
            var h = Math.Max(2f, yBottom - yTop);

            if (isTotal) barPaint.Color = totalColor;
            else if (delta >= 0) barPaint.Color = posColor;
            else barPaint.Color = negColor;

            var barRect = new SKRect(x, yTop, x + actualBarW, yBottom);
            canvas.DrawRoundRect(barRect, 2f, 2f, barPaint);

            // Draw Connector Line from previous bar's end to current bar's start
            var curConnectorY = plotArea.Bottom - (float)(((end - minVal) / yRange) * plotArea.Height);
            if (options.ShowConnectorLines && prevConnectorY.HasValue && prevConnectorX.HasValue && !isTotal)
            {
                canvas.DrawLine(prevConnectorX.Value, prevConnectorY.Value, x, prevConnectorY.Value, connPaint);
            }

            prevConnectorY = curConnectorY;
            prevConnectorX = x + actualBarW;

            // Category Label
            if (i < chart.Categories.Count)
            {
                var catText = chart.Categories[i];
                var catW = textPaint.MeasureText(catText);
                var catX = x + (actualBarW / 2f) - (catW / 2f);
                canvas.DrawText(catText, catX, plotArea.Bottom + 14f, textPaint);
            }

            // Value Label
            var valLabel = isTotal ? $"{end:N0}" : (delta >= 0 ? $"+{delta:N0}" : $"{delta:N0}");
            var valW = textPaint.MeasureText(valLabel);
            var valX = x + (actualBarW / 2f) - (valW / 2f);
            var valY = yTop - 4f;
            canvas.DrawText(valLabel, valX, valY, textPaint);
        }
    }

    private static void RenderFunnel(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;
        var series = chart.Series[0];
        var count = series.Values.Count;
        if (count == 0) return;

        var options = chart.FunnelOptions ?? new FunnelOptions();
        var topMax = series.Values.First();
        if (topMax <= 0.0001) topMax = 1.0;

        var stageHeight = plotArea.Height / count;
        var stagePadding = Math.Max(2f, stageHeight * 0.08f);
        var usableStageH = stageHeight - stagePadding;

        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var textPaint = new SKPaint { Color = SKColors.White, TextSize = 9f, FakeBoldText = true, IsAntialias = true, Typeface = typeface };
        using var subTextPaint = new SKPaint { Color = new SKColor(71, 85, 105), TextSize = 8f, IsAntialias = true, Typeface = typeface };

        for (int i = 0; i < count; i++)
        {
            var curVal = series.Values[i];
            var nextVal = (i + 1 < count) ? series.Values[i + 1] : curVal * options.NeckWidthRatio;

            var topRatio = (float)Math.Clamp(curVal / topMax, 0.15, 1.0);
            var bottomRatio = (float)Math.Clamp(nextVal / topMax, 0.1, 1.0);

            var topW = plotArea.Width * topRatio;
            var botW = plotArea.Width * bottomRatio;

            var yTop = plotArea.Top + (i * stageHeight);
            var yBot = yTop + usableStageH;

            var p1 = new SKPoint(plotArea.MidX - (topW / 2f), yTop);
            var p2 = new SKPoint(plotArea.MidX + (topW / 2f), yTop);
            var p3 = new SKPoint(plotArea.MidX + (botW / 2f), yBot);
            var p4 = new SKPoint(plotArea.MidX - (botW / 2f), yBot);

            using var path = new SKPath();
            path.MoveTo(p1);
            path.LineTo(p2);
            path.LineTo(p3);
            path.LineTo(p4);
            path.Close();

            var color = ChartColorPalette.GetColor(chart.Palette, i, chart.CustomColors);
            using var stagePaint = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(path, stagePaint);

            // Stage Label & Number
            var catName = i < chart.Categories.Count ? chart.Categories[i] : $"Stage {i + 1}";
            var conversion = (curVal / topMax) * 100.0;
            var label = $"{catName}: {curVal:N0} ({conversion:F1}%)";

            var labelW = textPaint.MeasureText(label);
            var labelX = plotArea.MidX - (labelW / 2f);
            var labelY = (yTop + yBot) / 2f + 3f;

            if (labelW < botW)
            {
                canvas.DrawText(label, labelX, labelY, textPaint);
            }
            else
            {
                // Fallback outside label
                canvas.DrawText(label, plotArea.Right + 8f, labelY, subTextPaint);
            }
        }
    }

    private static void DrawWaterfallGrid(SKCanvas canvas, SKRect plotArea, double minVal, double maxVal, string? fontFamily)
    {
        var steps = 4;
        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var gridPaint = new SKPaint { Color = new SKColor(226, 232, 240), StrokeWidth = 1f, IsAntialias = true, Style = SKPaintStyle.Stroke };
        using var labelPaint = new SKPaint { Color = new SKColor(148, 163, 184), TextSize = 8f, IsAntialias = true, Typeface = typeface };

        for (int i = 0; i <= steps; i++)
        {
            var frac = (float)i / steps;
            var y = plotArea.Bottom - (plotArea.Height * frac);
            var val = minVal + ((maxVal - minVal) * frac);

            canvas.DrawLine(plotArea.Left, y, plotArea.Right, y, gridPaint);
            var text = $"{val:N0}";
            var tw = labelPaint.MeasureText(text);
            canvas.DrawText(text, plotArea.Left - tw - 5f, y + 3f, labelPaint);
        }
    }
}
