using System;
using System.Collections.Generic;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class LineAreaChartRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;

        var categoryCount = chart.Categories.Count > 0 ? chart.Categories.Count : chart.Series.Max(s => s.Values.Count);
        if (categoryCount < 2) return;

        var isArea = chart.ChartType is ChartType.Area or ChartType.SplineArea or ChartType.StackedArea;
        var isSpline = chart.ChartType is ChartType.Spline or ChartType.SplineArea;
        var isStep = chart.ChartType is ChartType.StepLine;
        var isStacked = chart.ChartType is ChartType.StackedArea;

        // Calculate Y Range
        double minY = chart.YAxis?.Min ?? 0.0;
        double maxY;

        if (isStacked)
        {
            maxY = 0.0;
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
            var maxVal = chart.Series.SelectMany(s => s.Values).DefaultIfEmpty(0.0).Max();
            maxY = chart.YAxis?.Max ?? (maxVal > 0 ? maxVal * 1.15 : 10.0);
        }

        var yRange = maxY - minY;
        if (yRange <= 0.0001) yRange = 1.0;

        // Draw Y Grid & Labels
        DrawGridAndAxes(canvas, chart, plotArea, minY, maxY, categoryCount, fontFamily);

        var xStep = plotArea.Width / (categoryCount - 1);

        // Render series (if stacked area, bottom to top or top to bottom)
        var previousPoints = new SKPoint[categoryCount];
        for (int i = 0; i < categoryCount; i++)
        {
            previousPoints[i] = new SKPoint(plotArea.Left + (i * xStep), plotArea.Bottom);
        }

        for (int s = 0; s < chart.Series.Count; s++)
        {
            var series = chart.Series[s];
            var currentPoints = new SKPoint[categoryCount];

            for (int c = 0; c < categoryCount; c++)
            {
                var val = c < series.Values.Count ? series.Values[c] : 0.0;
                var x = plotArea.Left + (c * xStep);

                float y;
                if (isStacked)
                {
                    var valFrac = (float)((val - minY) / yRange);
                    var deltaY = plotArea.Height * valFrac;
                    y = previousPoints[c].Y - deltaY;
                }
                else
                {
                    var valFrac = (float)((val - minY) / yRange);
                    y = plotArea.Bottom - (plotArea.Height * Math.Clamp(valFrac, 0f, 1f));
                }

                currentPoints[c] = new SKPoint(x, y);
            }

            var color = ChartColorPalette.ParseColor(series.Color, ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors));

            // Draw Area Fill if area chart
            if (isArea)
            {
                using var areaPath = new SKPath();
                if (isSpline)
                {
                    BuildSplinePath(areaPath, currentPoints);
                }
                else if (isStep)
                {
                    BuildStepPath(areaPath, currentPoints);
                }
                else
                {
                    BuildLinearPath(areaPath, currentPoints);
                }

                // Connect back to bottom/previous points
                if (isStacked)
                {
                    for (int i = categoryCount - 1; i >= 0; i--)
                    {
                        areaPath.LineTo(previousPoints[i]);
                    }
                }
                else
                {
                    areaPath.LineTo(plotArea.Right, plotArea.Bottom);
                    areaPath.LineTo(plotArea.Left, plotArea.Bottom);
                }
                areaPath.Close();

                var fillColorTop = color.WithAlpha(120);
                var fillColorBottom = color.WithAlpha(20);
                using var gradient = SKShader.CreateLinearGradient(
                    new SKPoint(0, plotArea.Top),
                    new SKPoint(0, plotArea.Bottom),
                    [fillColorTop, fillColorBottom],
                    null,
                    SKShaderTileMode.Clamp);

                using var areaPaint = new SKPaint
                {
                    Shader = gradient,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawPath(areaPath, areaPaint);
            }

            // Draw Stroke Line
            using var linePath = new SKPath();
            if (isSpline)
            {
                BuildSplinePath(linePath, currentPoints);
            }
            else if (isStep)
            {
                BuildStepPath(linePath, currentPoints);
            }
            else
            {
                BuildLinearPath(linePath, currentPoints);
            }

            using var linePaint = new SKPaint
            {
                Color = color,
                StrokeWidth = Math.Max(2f, series.BorderWidth),
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };
            canvas.DrawPath(linePath, linePaint);

            // Draw Point Markers
            using var markerFillPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true };
            using var markerStrokePaint = new SKPaint { Color = color, Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };

            for (int c = 0; c < categoryCount; c++)
            {
                var pt = currentPoints[c];
                canvas.DrawCircle(pt.X, pt.Y, 3.5f, markerFillPaint);
                canvas.DrawCircle(pt.X, pt.Y, 3.5f, markerStrokePaint);
            }

            if (isStacked)
            {
                previousPoints = currentPoints;
            }
        }
    }

    private static void BuildLinearPath(SKPath path, SKPoint[] points)
    {
        if (points.Length == 0) return;
        path.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++)
        {
            path.LineTo(points[i]);
        }
    }

    private static void BuildStepPath(SKPath path, SKPoint[] points)
    {
        if (points.Length == 0) return;
        path.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++)
        {
            var midX = (points[i - 1].X + points[i].X) / 2f;
            path.LineTo(midX, points[i - 1].Y);
            path.LineTo(midX, points[i].Y);
            path.LineTo(points[i].X, points[i].Y);
        }
    }

    private static void BuildSplinePath(SKPath path, SKPoint[] points)
    {
        if (points.Length < 2)
        {
            BuildLinearPath(path, points);
            return;
        }

        path.MoveTo(points[0]);
        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : points[i];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = (i + 2 < points.Length) ? points[i + 2] : p2;

            var cp1X = p1.X + (p2.X - p0.X) / 6f;
            var cp1Y = p1.Y + (p2.Y - p0.Y) / 6f;
            var cp2X = p2.X - (p3.X - p1.X) / 6f;
            var cp2Y = p2.Y - (p3.Y - p1.Y) / 6f;

            path.CubicTo(cp1X, cp1Y, cp2X, cp2Y, p2.X, p2.Y);
        }
    }

    private static void DrawGridAndAxes(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, double minY, double maxY, int categoryCount, string? fontFamily)
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

        // Y Grid
        for (int i = 0; i <= steps; i++)
        {
            var frac = (float)i / steps;
            var y = plotArea.Bottom - (plotArea.Height * frac);
            var val = minY + ((maxY - minY) * frac);

            if (chart.YAxis?.ShowGridLines ?? true)
            {
                canvas.DrawLine(plotArea.Left, y, plotArea.Right, y, gridPaint);
            }

            var text = val >= 1000 ? $"{val / 1000:N0}k" : $"{val:N0}";
            var textWidth = labelPaint.MeasureText(text);
            canvas.DrawText(text, plotArea.Left - textWidth - 5f, y + 3f, labelPaint);
        }

        // X Axis Ticks
        var xStep = plotArea.Width / (categoryCount - 1);
        for (int c = 0; c < categoryCount; c++)
        {
            if (c < chart.Categories.Count)
            {
                var catText = chart.Categories[c];
                var catWidth = labelPaint.MeasureText(catText);
                var catX = plotArea.Left + (c * xStep) - (catWidth / 2f);
                var catY = plotArea.Bottom + 14f;
                canvas.DrawText(catText, catX, catY, labelPaint);
            }
        }
    }
}
