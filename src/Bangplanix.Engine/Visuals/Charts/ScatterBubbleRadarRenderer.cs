using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class ScatterBubbleRadarRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.ChartType == ChartType.Radar)
        {
            RenderRadarChart(canvas, chart, plotArea, fontFamily);
        }
        else if (chart.ChartType == ChartType.Bubble)
        {
            RenderScatterBubble(canvas, chart, plotArea, isBubble: true, fontFamily);
        }
        else
        {
            RenderScatterBubble(canvas, chart, plotArea, isBubble: false, fontFamily);
        }
    }

    private static void RenderScatterBubble(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, bool isBubble, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;

        // Calculate Min and Max for X and Y
        double minX = chart.XAxis?.Min ?? double.MaxValue;
        double maxX = chart.XAxis?.Max ?? double.MinValue;
        double minY = chart.YAxis?.Min ?? double.MaxValue;
        double maxY = chart.YAxis?.Max ?? double.MinValue;

        var allPoints = chart.Series.SelectMany(s => s.DataPoints).ToList();
        if (allPoints.Count == 0)
        {
            // If DataPoints empty, map from values
            for (int s = 0; s < chart.Series.Count; s++)
            {
                var ser = chart.Series[s];
                for (int i = 0; i < ser.Values.Count; i++)
                {
                    allPoints.Add(new ChartDataPoint { X = (double)i, Y = ser.Values[i] });
                }
            }
        }

        foreach (var pt in allPoints)
        {
            var xVal = Convert.ToDouble(pt.X ?? 0.0, CultureInfo.InvariantCulture);
            if (xVal < minX) minX = xVal;
            if (xVal > maxX) maxX = xVal;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        if (minX == double.MaxValue || maxX == double.MinValue) { minX = 0; maxX = 10; }
        if (minY == double.MaxValue || maxY == double.MinValue) { minY = 0; maxY = 10; }
        if (maxX <= minX) maxX = minX + 1.0;
        if (maxY <= minY) maxY = minY + 1.0;

        var xRange = maxX - minX;
        var yRange = maxY - minY;

        // Draw Axes & Gridlines
        DrawCartesianGrid(canvas, chart, plotArea, minX, maxX, minY, maxY, fontFamily);

        // Render Series
        var maxBubbleZ = allPoints.Select(p => p.Z ?? 1.0).DefaultIfEmpty(1.0).Max();
        if (maxBubbleZ <= 0.001) maxBubbleZ = 1.0;

        for (int s = 0; s < chart.Series.Count; s++)
        {
            var series = chart.Series[s];
            var pts = series.DataPoints.Count > 0 ? series.DataPoints : series.Values.Select((v, idx) => new ChartDataPoint { X = (double)idx, Y = v }).ToList();
            var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));

            using var fillPaint = new SKPaint
            {
                Color = color.WithAlpha(isBubble ? (byte)160 : (byte)220),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            using var strokePaint = new SKPaint
            {
                Color = color,
                StrokeWidth = 1.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            var xCoords = new List<double>();
            var yCoords = new List<double>();

            foreach (var pt in pts)
            {
                var xVal = Convert.ToDouble(pt.X ?? 0.0, CultureInfo.InvariantCulture);
                var yVal = pt.Y;

                xCoords.Add(xVal);
                yCoords.Add(yVal);

                var xFrac = (float)((xVal - minX) / xRange);
                var yFrac = (float)((yVal - minY) / yRange);

                var px = plotArea.Left + (plotArea.Width * xFrac);
                var py = plotArea.Bottom - (plotArea.Height * yFrac);

                var radius = 4f;
                if (isBubble)
                {
                    var zVal = pt.Z ?? 1.0;
                    radius = Math.Clamp((float)(zVal / maxBubbleZ) * 20f, 4f, 24f);
                }

                canvas.DrawCircle(px, py, radius, fillPaint);
                canvas.DrawCircle(px, py, radius, strokePaint);
            }

            // Optional Linear Trendline (y = mx + b)
            if (chart.ShowTrendline && xCoords.Count >= 2)
            {
                DrawTrendline(canvas, plotArea, xCoords, yCoords, minX, maxX, minY, maxY, color);
            }
        }
    }

    private static void DrawTrendline(SKCanvas canvas, SKRect plotArea, List<double> xVals, List<double> yVals, double minX, double maxX, double minY, double maxY, SKColor color)
    {
        int n = xVals.Count;
        double sumX = xVals.Sum();
        double sumY = yVals.Sum();
        double sumXY = xVals.Zip(yVals, (x, y) => x * y).Sum();
        double sumXX = xVals.Select(x => x * x).Sum();

        double denominator = (n * sumXX) - (sumX * sumX);
        if (Math.Abs(denominator) < 0.0001) return;

        double m = ((n * sumXY) - (sumX * sumY)) / denominator;
        double b = (sumY - (m * sumX)) / n;

        double y1 = (m * minX) + b;
        double y2 = (m * maxX) + b;

        var xRange = maxX - minX;
        var yRange = maxY - minY;

        var p1 = new SKPoint(
            plotArea.Left,
            plotArea.Bottom - (float)(((y1 - minY) / yRange) * plotArea.Height));
        var p2 = new SKPoint(
            plotArea.Right,
            plotArea.Bottom - (float)(((y2 - minY) / yRange) * plotArea.Height));

        using var trendPaint = new SKPaint
        {
            Color = color,
            StrokeWidth = 1.8f,
            Style = SKPaintStyle.Stroke,
            PathEffect = SKPathEffect.CreateDash([4f, 4f], 0f),
            IsAntialias = true
        };
        canvas.DrawLine(p1, p2, trendPaint);
    }

    private static void RenderRadarChart(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        var categories = chart.Categories.Count > 0 ? chart.Categories : chart.Series.FirstOrDefault()?.Categories ?? [];
        var numAxes = categories.Count;
        if (numAxes < 3) return;

        var center = new SKPoint(plotArea.MidX, plotArea.MidY);
        var radius = Math.Min(plotArea.Width, plotArea.Height) / 2f * 0.8f;
        var angleStep = 360f / numAxes;

        var maxVal = chart.Series.SelectMany(s => s.Values).DefaultIfEmpty(10.0).Max();
        if (maxVal <= 0.0001) maxVal = 10.0;

        var typeface = FontManager.Instance.GetTypeface(fontFamily);
        using var webPaint = new SKPaint
        {
            Color = new SKColor(226, 232, 240),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(100, 116, 139),
            TextSize = 8.5f,
            IsAntialias = true,
            Typeface = typeface
        };

        // Draw Concentric Web Rings (4 levels)
        var levels = 4;
        for (int lvl = 1; lvl <= levels; lvl++)
        {
            var ringRadius = radius * ((float)lvl / levels);
            using var ringPath = new SKPath();
            for (int i = 0; i < numAxes; i++)
            {
                var angle = (-90f + (i * angleStep)) * (float)Math.PI / 180f;
                var pt = new SKPoint(center.X + (ringRadius * (float)Math.Cos(angle)), center.Y + (ringRadius * (float)Math.Sin(angle)));
                if (i == 0) ringPath.MoveTo(pt);
                else ringPath.LineTo(pt);
            }
            ringPath.Close();
            canvas.DrawPath(ringPath, webPaint);
        }

        // Draw Spoke Lines & Axis Labels
        for (int i = 0; i < numAxes; i++)
        {
            var angle = (-90f + (i * angleStep)) * (float)Math.PI / 180f;
            var endPoint = new SKPoint(center.X + (radius * (float)Math.Cos(angle)), center.Y + (radius * (float)Math.Sin(angle)));
            canvas.DrawLine(center, endPoint, webPaint);

            var labelPoint = new SKPoint(center.X + ((radius + 12f) * (float)Math.Cos(angle)), center.Y + ((radius + 12f) * (float)Math.Sin(angle)));
            var catName = categories[i];
            var catW = labelPaint.MeasureText(catName);
            canvas.DrawText(catName, labelPoint.X - (catW / 2f), labelPoint.Y + 3f, labelPaint);
        }

        // Draw Series Polygons
        for (int s = 0; s < chart.Series.Count; s++)
        {
            var series = chart.Series[s];
            var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));

            using var seriesPath = new SKPath();
            var polyPoints = new List<SKPoint>();

            for (int i = 0; i < numAxes; i++)
            {
                var val = i < series.Values.Count ? series.Values[i] : 0.0;
                var frac = Math.Clamp((float)(val / maxVal), 0f, 1f);
                var curRadius = radius * frac;

                var angle = (-90f + (i * angleStep)) * (float)Math.PI / 180f;
                var pt = new SKPoint(center.X + (curRadius * (float)Math.Cos(angle)), center.Y + (curRadius * (float)Math.Sin(angle)));
                polyPoints.Add(pt);

                if (i == 0) seriesPath.MoveTo(pt);
                else seriesPath.LineTo(pt);
            }
            seriesPath.Close();

            // Translucent Fill
            using var fillPaint = new SKPaint
            {
                Color = color.WithAlpha(80),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawPath(seriesPath, fillPaint);

            // Perimeter Stroke
            using var strokePaint = new SKPaint
            {
                Color = color,
                StrokeWidth = 2f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };
            canvas.DrawPath(seriesPath, strokePaint);

            // Vertex Markers
            using var markerPaint = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
            foreach (var pt in polyPoints)
            {
                canvas.DrawCircle(pt.X, pt.Y, 3f, markerPaint);
            }
        }
    }

    private static void DrawCartesianGrid(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, double minX, double maxX, double minY, double maxY, string? fontFamily)
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

        // Y Grid
        for (int i = 0; i <= 4; i++)
        {
            var frac = (float)i / 4;
            var y = plotArea.Bottom - (plotArea.Height * frac);
            var val = minY + ((maxY - minY) * frac);

            if (chart.YAxis?.ShowGridLines ?? true)
            {
                canvas.DrawLine(plotArea.Left, y, plotArea.Right, y, gridPaint);
            }
            var text = $"{val:N0}";
            var tw = labelPaint.MeasureText(text);
            canvas.DrawText(text, plotArea.Left - tw - 5f, y + 3f, labelPaint);
        }

        // X Grid
        for (int i = 0; i <= 4; i++)
        {
            var frac = (float)i / 4;
            var x = plotArea.Left + (plotArea.Width * frac);
            var val = minX + ((maxX - minX) * frac);

            if (chart.XAxis?.ShowGridLines ?? true)
            {
                canvas.DrawLine(x, plotArea.Top, x, plotArea.Bottom, gridPaint);
            }
            var text = $"{val:N0}";
            var tw = labelPaint.MeasureText(text);
            canvas.DrawText(text, x - (tw / 2f), plotArea.Bottom + 12f, labelPaint);
        }
    }
}
