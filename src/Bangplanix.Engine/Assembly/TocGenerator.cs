using System;
using System.Collections.Generic;
using System.Globalization;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Assembly;

public sealed record TocEntry(string Title, int PageNumber, int Level = 1);

public static class TocGenerator
{
    public static void RenderTocPage(SKCanvas canvas, TocDefinition toc, IReadOnlyList<TocEntry> entries, float pageWidthPt, float pageHeightPt)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(toc);
        ArgumentNullException.ThrowIfNull(entries);

        canvas.Clear(SKColors.White);

        var typeface = FontManager.Instance.GetTypeface(toc.FontFamily);
        var marginX = 54f; // 0.75 in
        var curY = 72f;

        // Title
        using var titlePaint = new SKPaint
        {
            Color = new SKColor(15, 23, 42),
            TextSize = 18f,
            FakeBoldText = true,
            IsAntialias = true,
            Typeface = typeface
        };

        var titleW = titlePaint.MeasureText(toc.Title);
        canvas.DrawText(toc.Title, marginX, curY, titlePaint);
        curY += 12f;

        // Header Divider Line
        using var linePaint = new SKPaint
        {
            Color = new SKColor(226, 232, 240),
            StrokeWidth = 1.5f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        canvas.DrawLine(marginX, curY, pageWidthPt - marginX, curY, linePaint);
        curY += 28f;

        // Entries
        using var entryPaint = new SKPaint
        {
            Color = new SKColor(30, 41, 59),
            TextSize = toc.FontSize,
            IsAntialias = true,
            Typeface = typeface
        };
        using var pageNumPaint = new SKPaint
        {
            Color = new SKColor(71, 85, 105),
            TextSize = toc.FontSize,
            FakeBoldText = true,
            IsAntialias = true,
            Typeface = typeface
        };
        using var dotPaint = new SKPaint
        {
            Color = new SKColor(148, 163, 184),
            TextSize = toc.FontSize,
            IsAntialias = true,
            Typeface = typeface
        };

        var rowHeight = 24f;
        var dotString = ". ";
        var dotWidth = dotPaint.MeasureText(dotString);

        foreach (var entry in entries)
        {
            var indent = (entry.Level - 1) * 16f;
            var textX = marginX + indent;
            var pageText = entry.PageNumber.ToString(CultureInfo.InvariantCulture);
            var pageTextW = pageNumPaint.MeasureText(pageText);
            var pageTextX = pageWidthPt - marginX - pageTextW;

            // Draw Section Title
            canvas.DrawText(entry.Title, textX, curY, entryPaint);
            var titleTextW = entryPaint.MeasureText(entry.Title);

            // Draw Page Number
            if (toc.ShowPageNumbers)
            {
                canvas.DrawText(pageText, pageTextX, curY, pageNumPaint);

                // Draw Dotted Leader Line
                var dotStartX = textX + titleTextW + 8f;
                var dotEndX = pageTextX - 8f;

                if (dotEndX > dotStartX)
                {
                    var countDots = (int)((dotEndX - dotStartX) / dotWidth);
                    var sb = new System.Text.StringBuilder();
                    for (int d = 0; d < countDots; d++) sb.Append(dotString);
                    canvas.DrawText(sb.ToString(), dotStartX, curY, dotPaint);
                }
            }

            curY += rowHeight;

            // Page overflow check
            if (curY > pageHeightPt - 72f)
            {
                break;
            }
        }
    }
}
