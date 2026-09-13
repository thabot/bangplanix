using System;
using System.Collections.Generic;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class GaugeAndBulletRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.ChartType is ChartType.Gauge or ChartType.RadialSpeedometer)
        {
            RenderRadialGauge(canvas, chart, plotArea, fontFamily);
        }
        else if (chart.ChartType == ChartType.Bullet)
        {
            RenderBulletChart(canvas, chart, plotArea, fontFamily);
        }
    }

    private static void RenderRadialGauge(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        var options = chart.GaugeOptions ?? new GaugeOptions();
        var minVal = options.MinValue;
        var maxVal = options.MaxValue > minVal ? options.MaxValue : 100.0;
        var curVal = Math.Clamp(options.Value, minVal, maxVal);

        var center = new SKPoint(plotArea.MidX, plotArea.Bottom - (plotArea.Height * 0.15f));
        var radius = Math.Min(plotArea.Width / 2f, plotArea.Height * 0.9f) * 0.85f;
        var strokeWidth = radius * 0.22f;

        var arcRect = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

        // Gauge spans 180 degrees (from 180° to 360°/0°)
        var startAngle = 180f;
        var totalSweep = 180f;

        var defaultRanges = options.Ranges.Count > 0
            ? options.Ranges
            : new List<GaugeRange>
            {
                new() { Start = minVal, End = minVal + ((maxVal - minVal) * 0.6), Color = "#22c55e" },
                new() { Start = minVal + ((maxVal - minVal) * 0.6), End = minVal + ((maxVal - minVal) * 0.85), Color = "#eab308" },
                new() { Start = minVal + ((maxVal - minVal) * 0.85), End = maxVal, Color = "#ef4444" }
            };

        var rangeVal = maxVal - minVal;

        // Draw Arc Ranges
        using var arcPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Butt
        };

        foreach (var r in defaultRanges)
        {
            var rStart = Math.Clamp(r.Start, minVal, maxVal);
            var rEnd = Math.Clamp(r.End, minVal, maxVal);
            if (rEnd <= rStart) continue;

            var rStartFrac = (float)((rStart - minVal) / rangeVal);
            var rSweepFrac = (float)((rEnd - rStart) / rangeVal);

            var aStart = startAngle + (rStartFrac * totalSweep);
            var aSweep = rSweepFrac * totalSweep;

            arcPaint.Color = ChartColorPalette.ParseColor(r.Color, SKColors.LightGray);
            using var arcPath = new SKPath();
            arcPath.ArcTo(arcRect, aStart, aSweep, false);
            canvas.DrawPath(arcPath, arcPaint);
        }

        // Draw Needle Indicator
        var valFrac = (float)((curVal - minVal) / rangeVal);
        var needleAngle = startAngle + (valFrac * totalSweep);
        var needleRad = needleAngle * (float)Math.PI / 180f;

        var needleLen = radius * 0.85f;
        var needleTip = new SKPoint(
            center.X + (needleLen * (float)Math.Cos(needleRad)),
            center.Y + (needleLen * (float)Math.Sin(needleRad)));

        var perpRad = needleRad + ((float)Math.PI / 2f);
        var baseWidth = 4f;
        var p1 = new SKPoint(center.X + (baseWidth * (float)Math.Cos(perpRad)), center.Y + (baseWidth * (float)Math.Sin(perpRad)));
        var p2 = new SKPoint(center.X - (baseWidth * (float)Math.Cos(perpRad)), center.Y - (baseWidth * (float)Math.Sin(perpRad)));

        using var needlePath = new SKPath();
        needlePath.MoveTo(p1);
        needlePath.LineTo(needleTip);
        needlePath.LineTo(p2);
        needlePath.Close();

        using var needlePaint = new SKPaint
        {
            Color = new SKColor(30, 41, 59),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawPath(needlePath, needlePaint);

        // Center Pivot Circle
        canvas.DrawCircle(center.X, center.Y, 6f, needlePaint);

        // Readout Text
        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var valTextPaint = new SKPaint
        {
            Color = new SKColor(15, 23, 42),
            TextSize = 14f,
            IsAntialias = true,
            Typeface = typeface
        };
        var unitSuffix = string.IsNullOrEmpty(options.Unit) ? "" : $" {options.Unit}";
        var valText = $"{curVal:F1}{unitSuffix}";
        var valW = valTextPaint.MeasureText(valText);
        canvas.DrawText(valText, center.X - (valW / 2f), center.Y + 18f, valTextPaint);
    }

    private static void RenderBulletChart(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        var options = chart.BulletOptions ?? new BulletOptions();
        var maxScale = Math.Max(options.GoodRange, Math.Max(options.Target, options.Actual)) * 1.1;
        if (maxScale <= 0.0001) maxScale = 100.0;

        var trackTop = plotArea.MidY - 14f;
        var trackHeight = 28f;

        // Qualitative background ranges (Bad -> Satisfactory -> Good)
        var badW = (float)(Math.Clamp(options.BadRange, 0, maxScale) / maxScale) * plotArea.Width;
        var satW = (float)(Math.Clamp(options.SatisfactoryRange, 0, maxScale) / maxScale) * plotArea.Width;
        var goodW = (float)(Math.Clamp(options.GoodRange, 0, maxScale) / maxScale) * plotArea.Width;

        using var rangePaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        // Good range (lightest background)
        rangePaint.Color = new SKColor(226, 232, 240);
        canvas.DrawRect(plotArea.Left, trackTop, goodW, trackHeight, rangePaint);

        // Satisfactory range (medium gray)
        rangePaint.Color = new SKColor(203, 213, 225);
        canvas.DrawRect(plotArea.Left, trackTop, satW, trackHeight, rangePaint);

        // Bad range (darkest gray)
        rangePaint.Color = new SKColor(148, 163, 184);
        canvas.DrawRect(plotArea.Left, trackTop, badW, trackHeight, rangePaint);

        // Actual Performance Bar (Thinner inside bar)
        var actualW = (float)(Math.Clamp(options.Actual, 0, maxScale) / maxScale) * plotArea.Width;
        var actualTop = trackTop + (trackHeight * 0.25f);
        var actualHeight = trackHeight * 0.5f;

        using var actualPaint = new SKPaint
        {
            Color = new SKColor(30, 41, 59),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawRect(plotArea.Left, actualTop, actualW, actualHeight, actualPaint);

        // Target Benchmark Line
        var targetX = plotArea.Left + ((float)(Math.Clamp(options.Target, 0, maxScale) / maxScale) * plotArea.Width);
        using var targetPaint = new SKPaint
        {
            Color = new SKColor(220, 38, 38),
            StrokeWidth = 3f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        canvas.DrawLine(targetX, trackTop - 3f, targetX, trackTop + trackHeight + 3f, targetPaint);

        // Labels & Numbers
        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(100, 116, 139),
            TextSize = 8f,
            IsAntialias = true,
            Typeface = typeface
        };

        var unit = string.IsNullOrEmpty(options.Unit) ? "" : $" ({options.Unit})";
        var actualLabel = $"Actual: {options.Actual:N0}{unit}";
        var targetLabel = $"Target: {options.Target:N0}";

        canvas.DrawText(actualLabel, plotArea.Left, trackTop + trackHeight + 14f, labelPaint);
        var tW = labelPaint.MeasureText(targetLabel);
        canvas.DrawText(targetLabel, plotArea.Right - tW, trackTop + trackHeight + 14f, labelPaint);
    }
}
