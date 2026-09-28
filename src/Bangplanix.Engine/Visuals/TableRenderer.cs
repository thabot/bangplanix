using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals;

public static class TableRenderer
{
    public static float[] CalculateColumnWidths(IReadOnlyList<TableColumnDefinition> columns, float totalTableWidthPt, UnitType unit)
    {
        if (columns == null || columns.Count == 0)
        {
            return [totalTableWidthPt];
        }

        var colCount = columns.Count;
        var widths = new float[colCount];
        var relativeWeights = new float[colCount];
        float totalFixed = 0f;
        float totalRelativeWeight = 0f;

        for (int i = 0; i < colCount; i++)
        {
            var raw = columns[i].Width?.Trim() ?? "1*";
            if (raw.EndsWith('*'))
            {
                var numStr = raw[..^1].Trim();
                var weight = string.IsNullOrEmpty(numStr) ? 1.0f : (float.TryParse(numStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var w) ? w : 1.0f);
                relativeWeights[i] = weight;
                totalRelativeWeight += weight;
            }
            else if (raw.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(raw[..^2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var mm))
                {
                    widths[i] = UnitConverter.ToPoints(mm, UnitType.Mm);
                    totalFixed += widths[i];
                }
            }
            else if (raw.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
            {
                if (float.TryParse(raw[..^2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pt))
                {
                    widths[i] = pt;
                    totalFixed += widths[i];
                }
            }
            else if (float.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
            {
                widths[i] = UnitConverter.ToPoints(val, unit);
                totalFixed += widths[i];
            }
            else
            {
                relativeWeights[i] = 1.0f;
                totalRelativeWeight += 1.0f;
            }
        }

        var remainingSpace = Math.Max(0f, totalTableWidthPt - totalFixed);
        if (totalRelativeWeight > 0)
        {
            for (int i = 0; i < colCount; i++)
            {
                if (relativeWeights[i] > 0)
                {
                    widths[i] = (relativeWeights[i] / totalRelativeWeight) * remainingSpace;
                }
            }
        }

        return widths;
    }

    public static float MeasureRowHeight(
        TableRowDefinition row,
        float[] columnWidths,
        UnitType unit,
        BandContext context,
        ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(columnWidths);

        var baseHeightPt = row.Height > 0 ? UnitConverter.ToPoints(row.Height, unit) : 18.0f;
        if (!row.CanGrow)
        {
            return baseHeightPt;
        }

        float maxContentHeight = baseHeightPt;
        int colIndex = 0;

        foreach (var cell in row.Cells)
        {
            if (colIndex >= columnWidths.Length) break;
            var span = Math.Max(1, cell.ColumnSpan);
            float cellWidth = 0f;
            for (int s = 0; s < span && (colIndex + s) < columnWidths.Length; s++)
            {
                cellWidth += columnWidths[colIndex + s];
            }

            var pad = cell.Padding ?? new TablePaddingDefinition();
            var padLeftPt = UnitConverter.ToPoints(pad.Left, unit);
            var padRightPt = UnitConverter.ToPoints(pad.Right, unit);
            var padTopPt = UnitConverter.ToPoints(pad.Top, unit);
            var padBottomPt = UnitConverter.ToPoints(pad.Bottom, unit);

            var availableTextWidth = Math.Max(10f, cellWidth - padLeftPt - padRightPt);
            var text = context.ResolveExpressionOrValue(cell.Text, cell.Expression)?.ToString() ?? cell.Text ?? string.Empty;

            if (!string.IsNullOrEmpty(text))
            {
                var style = cell.Style ?? (cell.StyleRef != null && report.Styles.TryGetValue(cell.StyleRef, out var refStyle) ? refStyle : row.Style);
                var measuredTextHeight = SkiaReportCanvas.MeasureTextHeight(text, availableTextWidth, style);
                var totalCellHeight = padTopPt + measuredTextHeight + padBottomPt;
                maxContentHeight = Math.Max(maxContentHeight, totalCellHeight);
            }

            colIndex += span;
        }

        return maxContentHeight;
    }

    public static void RenderRow(
        SKCanvas canvas,
        TableRowDefinition row,
        float xPt,
        float yPt,
        float[] columnWidths,
        float rowHeightPt,
        UnitType unit,
        BandContext context,
        ReportDefinition report,
        string? overrideBackground = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(row);

        var bg = overrideBackground ?? row.BackgroundColor;
        if (!string.IsNullOrWhiteSpace(bg))
        {
            using var bgPaint = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = ParseColor(bg)
            };
            float totalW = 0;
            foreach (var w in columnWidths) totalW += w;
            canvas.DrawRect(SKRect.Create(xPt, yPt, totalW, rowHeightPt), bgPaint);
        }

        float currentX = xPt;
        int colIndex = 0;

        foreach (var cell in row.Cells)
        {
            if (colIndex >= columnWidths.Length) break;
            var span = Math.Max(1, cell.ColumnSpan);
            float cellWidth = 0f;
            for (int s = 0; s < span && (colIndex + s) < columnWidths.Length; s++)
            {
                cellWidth += columnWidths[colIndex + s];
            }

            var cellRect = SKRect.Create(currentX, yPt, cellWidth, rowHeightPt);

            // Cell background
            if (!string.IsNullOrWhiteSpace(cell.BackgroundColor))
            {
                using var cellBgPaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    Color = ParseColor(cell.BackgroundColor)
                };
                canvas.DrawRect(cellRect, cellBgPaint);
            }

            // Cell border
            if (cell.Border != null)
            {
                using var borderPaint = new SKPaint
                {
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (float)cell.Border.Width,
                    Color = ParseColor(cell.Border.Color ?? "#CCCCCC")
                };
                canvas.DrawRect(cellRect, borderPaint);
            }

            // Cell content
            var pad = cell.Padding ?? new TablePaddingDefinition();
            var padLeftPt = UnitConverter.ToPoints(pad.Left, unit);
            var padRightPt = UnitConverter.ToPoints(pad.Right, unit);
            var padTopPt = UnitConverter.ToPoints(pad.Top, unit);

            var text = context.ResolveExpressionOrValue(cell.Text, cell.Expression)?.ToString() ?? cell.Text ?? string.Empty;
            if (!string.IsNullOrEmpty(text))
            {
                var style = cell.Style ?? (cell.StyleRef != null && report.Styles.TryGetValue(cell.StyleRef, out var refStyle) ? refStyle : row.Style);
                var textX = currentX + padLeftPt;
                var textY = yPt + padTopPt;
                var textWidth = Math.Max(10f, cellWidth - padLeftPt - padRightPt);
                var textHeight = rowHeightPt - padTopPt;

                SkiaReportCanvas.DrawMultiLineText(canvas, text, textX, textY, textWidth, textHeight, style);
            }

            currentX += cellWidth;
            colIndex += span;
        }
    }

    private static SKColor ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return SKColors.Transparent;
        if (SKColor.TryParse(hex, out var color)) return color;
        return SKColors.Transparent;
    }
}
