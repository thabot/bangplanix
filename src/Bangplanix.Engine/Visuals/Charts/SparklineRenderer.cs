using System;
using System.Collections.Generic;
using System.Linq;
using Bangplanix.Core.Models;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class SparklineRenderer
{
    public static void RenderSparkline(SKCanvas canvas, SparklineDefinition sparkline, SKRect bounds)
    {
        if (sparkline.Values == null || sparkline.Values.Count == 0) return;

        var count = sparkline.Values.Count;
        var padding = 2f;
        var plotRect = new SKRect(bounds.Left + padding, bounds.Top + padding, bounds.Right - padding, bounds.Bottom - padding);
        if (plotRect.Width <= 2 || plotRect.Height <= 2) return;

        var minVal = sparkline.Values.Min();
        var maxVal = sparkline.Values.Max();
        var range = maxVal - minVal;
        if (range <= 0.0001) range = 1.0;

        var lineColor = ChartColorPalette.ParseColor(sparkline.LineColor, new SKColor(37, 99, 235));
        var fillColor = ChartColorPalette.ParseColor(sparkline.FillColor, new SKColor(147, 197, 253, 60));
        var minColor = ChartColorPalette.ParseColor(sparkline.MinColor, new SKColor(239, 68, 68));
        var maxColor = ChartColorPalette.ParseColor(sparkline.MaxColor, new SKColor(16, 185, 129));
        var lastColor = ChartColorPalette.ParseColor(sparkline.LastColor, new SKColor(245, 158, 11));

        switch (sparkline.Type)
        {
            case SparklineType.Line:
            case SparklineType.Area:
                RenderLineOrArea(canvas, sparkline, plotRect, minVal, range, lineColor, fillColor, minColor, maxColor, lastColor);
                break;

            case SparklineType.Bar:
                RenderMicroBar(canvas, sparkline, plotRect, minVal, maxVal, range, lineColor, minColor, maxColor);
                break;

            case SparklineType.WinLoss:
                RenderWinLoss(canvas, sparkline, plotRect, maxColor, minColor);
                break;
        }
    }

    private static void RenderLineOrArea(SKCanvas canvas, SparklineDefinition sparkline, SKRect plotRect, double minVal, double range, SKColor lineColor, SKColor fillColor, SKColor minColor, SKColor maxColor, SKColor lastColor)
    {
        var count = sparkline.Values.Count;
        var xStep = count > 1 ? plotRect.Width / (count - 1) : plotRect.Width;
        var points = new SKPoint[count];

        int minIdx = 0;
        int maxIdx = 0;

        for (int i = 0; i < count; i++)
        {
            var v = sparkline.Values[i];
            if (v == sparkline.Values.Min()) minIdx = i;
            if (v == sparkline.Values.Max()) maxIdx = i;

            var x = plotRect.Left + (i * xStep);
            var yFrac = (float)((v - minVal) / range);
            var y = plotRect.Bottom - (plotRect.Height * Math.Clamp(yFrac, 0f, 1f));
            points[i] = new SKPoint(x, y);
        }

        // Draw Area Fill if SparklineType.Area
        if (sparkline.Type == SparklineType.Area && count > 1)
        {
            using var areaPath = new SKPath();
            areaPath.MoveTo(points[0]);
            for (int i = 1; i < count; i++) areaPath.LineTo(points[i]);
            areaPath.LineTo(points[^1].X, plotRect.Bottom);
            areaPath.LineTo(points[0].X, plotRect.Bottom);
            areaPath.Close();

            using var fillPaint = new SKPaint { Color = fillColor, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawPath(areaPath, fillPaint);
        }

        // Draw Polyline
        using var linePath = new SKPath();
        linePath.MoveTo(points[0]);
        for (int i = 1; i < count; i++) linePath.LineTo(points[i]);

        using var linePaint = new SKPaint
        {
            Color = lineColor,
            StrokeWidth = 1.5f,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };
        canvas.DrawPath(linePath, linePaint);

        // Highlight Min, Max, and Last Points
        if (sparkline.HighlightMinMax && count > 1)
        {
            using var dotPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

            // Min Dot
            dotPaint.Color = minColor;
            canvas.DrawCircle(points[minIdx], 2.2f, dotPaint);

            // Max Dot
            dotPaint.Color = maxColor;
            canvas.DrawCircle(points[maxIdx], 2.2f, dotPaint);

            // Last Dot
            dotPaint.Color = lastColor;
            canvas.DrawCircle(points[^1], 2.2f, dotPaint);
        }
    }

    private static void RenderMicroBar(SKCanvas canvas, SparklineDefinition sparkline, SKRect plotRect, double minVal, double maxVal, double range, SKColor baseColor, SKColor minColor, SKColor maxColor)
    {
        var count = sparkline.Values.Count;
        var barWidth = plotRect.Width / count;
        var barSpacing = Math.Max(1f, barWidth * 0.2f);
        var actualBarW = Math.Max(1f, barWidth - barSpacing);

        using var barPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        for (int i = 0; i < count; i++)
        {
            var v = sparkline.Values[i];
            var x = plotRect.Left + (i * barWidth) + (barSpacing / 2f);
            var yFrac = (float)((v - minVal) / range);
            var h = Math.Max(1f, plotRect.Height * Math.Clamp(yFrac, 0f, 1f));
            var y = plotRect.Bottom - h;

            if (sparkline.HighlightMinMax && v == minVal) barPaint.Color = minColor;
            else if (sparkline.HighlightMinMax && v == maxVal) barPaint.Color = maxColor;
            else barPaint.Color = baseColor;

            canvas.DrawRect(x, y, actualBarW, h, barPaint);
        }
    }

    private static void RenderWinLoss(SKCanvas canvas, SparklineDefinition sparkline, SKRect plotRect, SKColor winColor, SKColor lossColor)
    {
        var count = sparkline.Values.Count;
        var barWidth = plotRect.Width / count;
        var barSpacing = Math.Max(1f, barWidth * 0.2f);
        var actualBarW = Math.Max(1f, barWidth - barSpacing);
        var midY = plotRect.MidY;
        var halfH = (plotRect.Height / 2f) - 1f;

        using var barPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };

        for (int i = 0; i < count; i++)
        {
            var v = sparkline.Values[i];
            var x = plotRect.Left + (i * barWidth) + (barSpacing / 2f);

            if (v > 0)
            {
                barPaint.Color = winColor;
                canvas.DrawRect(x, midY - halfH, actualBarW, halfH, barPaint);
            }
            else if (v < 0)
            {
                barPaint.Color = lossColor;
                canvas.DrawRect(x, midY, actualBarW, halfH, barPaint);
            }
            else
            {
                barPaint.Color = SKColors.LightGray;
                canvas.DrawRect(x, midY - 1f, actualBarW, 2f, barPaint);
            }
        }
    }
}
