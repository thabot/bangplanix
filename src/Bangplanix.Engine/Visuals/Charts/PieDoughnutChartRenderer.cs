using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class PieDoughnutChartRenderer
{
    public static void Render(SKCanvas canvas, ChartDefinition chart, SKRect plotArea, string? fontFamily)
    {
        if (chart.Series.Count == 0) return;

        var isDoughnut = chart.ChartType == ChartType.Doughnut;
        var firstSeries = chart.Series[0];
        if (firstSeries.Values.Count == 0) return;

        var total = firstSeries.Values.Sum();
        if (total <= 0.0001) total = 1.0;

        var center = new SKPoint(plotArea.MidX, plotArea.MidY);
        var radius = Math.Min(plotArea.Width, plotArea.Height) / 2f * 0.85f;
        var innerRadius = isDoughnut ? radius * Math.Clamp(chart.DonutHoleSize, 0.2f, 0.8f) : 0f;

        var currentAngle = -90f; // Start at 12 o'clock
        var typeface = FontManager.Instance.GetTypeface(fontFamily);

        using var slicePaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        using var strokePaint = new SKPaint
        {
            Color = SKColors.White,
            StrokeWidth = 2f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var labelPaint = new SKPaint
        {
            Color = new SKColor(51, 65, 85),
            TextSize = 8.5f,
            IsAntialias = true,
            Typeface = typeface
        };
        using var leaderLinePaint = new SKPaint
        {
            Color = new SKColor(148, 163, 184),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        for (int i = 0; i < firstSeries.Values.Count; i++)
        {
            var val = firstSeries.Values[i];
            var sweep = (float)((val / total) * 360.0);
            if (sweep <= 0.001f) continue;

            var sliceColor = ChartColorPalette.ParseColor(
                i < firstSeries.DataPoints.Count ? firstSeries.DataPoints[i].Color : null,
                ChartColorPalette.GetColor(chart.Palette, i, chart.CustomColors));
            slicePaint.Color = sliceColor;

            // Exploded slice offset
            var isExploded = chart.ExplodedSliceIndex.HasValue && chart.ExplodedSliceIndex.Value == i;
            var midAngle = currentAngle + (sweep / 2f);
            var midRad = midAngle * (float)Math.PI / 180f;

            var sliceCenter = center;
            if (isExploded)
            {
                var explodeDistance = radius * 0.12f;
                sliceCenter = new SKPoint(
                    center.X + (explodeDistance * (float)Math.Cos(midRad)),
                    center.Y + (explodeDistance * (float)Math.Sin(midRad)));
            }

            var outerRect = new SKRect(sliceCenter.X - radius, sliceCenter.Y - radius, sliceCenter.X + radius, sliceCenter.Y + radius);

            using var path = new SKPath();
            if (isDoughnut)
            {
                var innerRect = new SKRect(sliceCenter.X - innerRadius, sliceCenter.Y - innerRadius, sliceCenter.X + innerRadius, sliceCenter.Y + innerRadius);
                path.ArcTo(outerRect, currentAngle, sweep, false);
                path.ArcTo(innerRect, currentAngle + sweep, -sweep, false);
                path.Close();
            }
            else
            {
                path.MoveTo(sliceCenter);
                path.ArcTo(outerRect, currentAngle, sweep, false);
                path.Close();
            }

            canvas.DrawPath(path, slicePaint);
            canvas.DrawPath(path, strokePaint);

            // Calculate percentage & label
            var percentage = (val / total) * 100.0;
            var categoryName = i < chart.Categories.Count ? chart.Categories[i] : $"Item {i + 1}";
            var labelText = $"{categoryName}: {percentage:F1}%";

            // Leader Line & Outer Label
            if (percentage >= 3.0) // Only for non-trivial slices
            {
                var labelRadius = radius * 1.08f;
                var anchorX = sliceCenter.X + (labelRadius * (float)Math.Cos(midRad));
                var anchorY = sliceCenter.Y + (labelRadius * (float)Math.Sin(midRad));

                var isRight = Math.Cos(midRad) >= 0;
                var endX = anchorX + (isRight ? 12f : -12f);

                canvas.DrawLine(
                    sliceCenter.X + (radius * 0.95f * (float)Math.Cos(midRad)),
                    sliceCenter.Y + (radius * 0.95f * (float)Math.Sin(midRad)),
                    anchorX, anchorY, leaderLinePaint);
                canvas.DrawLine(anchorX, anchorY, endX, anchorY, leaderLinePaint);

                var textWidth = labelPaint.MeasureText(labelText);
                var textX = isRight ? endX + 4f : endX - textWidth - 4f;
                canvas.DrawText(labelText, textX, anchorY + 3f, labelPaint);
            }

            currentAngle += sweep;
        }

        // Doughnut Center Text (e.g. Total)
        if (isDoughnut)
        {
            using var totalLabelPaint = new SKPaint
            {
                Color = new SKColor(100, 116, 139),
                TextSize = 8f,
                IsAntialias = true,
                Typeface = typeface
            };
            using var totalValuePaint = new SKPaint
            {
                Color = new SKColor(15, 23, 42),
                TextSize = 12f,
                IsAntialias = true,
                Typeface = typeface
            };

            var subText = "TOTAL";
            var valText = total >= 1000 ? $"{total:N0}" : $"{total:F0}";

            var subW = totalLabelPaint.MeasureText(subText);
            var valW = totalValuePaint.MeasureText(valText);

            canvas.DrawText(subText, center.X - (subW / 2f), center.Y - 2f, totalLabelPaint);
            canvas.DrawText(valText, center.X - (valW / 2f), center.Y + 12f, totalValuePaint);
        }
    }
}
