using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Canvas;

public sealed class SkiaReportCanvas
{
    private readonly SKCanvas _canvas;
    private readonly UnitType _unit;

    public SkiaReportCanvas(SKCanvas canvas, UnitType unit = UnitType.Mm)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _unit = unit;
    }

    public void RenderBand(BandDefinition band, float offsetYPt, BandContext context)
    {
        if (band == null || band.Elements.Count == 0)
        {
            return;
        }

        if (!IsPageDisplayMatch(band.ShowOnPages, context))
        {
            return;
        }

        foreach (var element in band.Elements)
        {
            RenderElement(element, offsetYPt, context);
        }
    }

    public void RenderElement(ElementDefinition element, float offsetYPt, BandContext context)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(context);

        if (!IsPageDisplayMatch(element.ShowOnPages, context))
        {
            return;
        }

        var xPt = UnitConverter.ToPoints(element.X, _unit);
        var yPt = offsetYPt + UnitConverter.ToPoints(element.Y, _unit);
        var wPt = UnitConverter.ToPoints(element.Width, _unit);
        var hPt = UnitConverter.ToPoints(element.Height, _unit);

        switch (element.Type)
        {
            case ElementType.Text:
                RenderTextElement(element, xPt, yPt, wPt, hPt, context);
                break;

            case ElementType.Shape:
                RenderShapeElement(element, xPt, yPt, wPt, hPt);
                break;

            case ElementType.Barcode:
                RenderBarcodeElement(element, xPt, yPt, wPt, hPt, context);
                break;

            case ElementType.QrCode:
                RenderQrCodeElement(element, xPt, yPt, wPt, hPt, context);
                break;

            case ElementType.Image:
                RenderImageElement(element, xPt, yPt, wPt, hPt, context);
                break;

            case ElementType.Chart:
                RenderChartElement(element, xPt, yPt, wPt, hPt, context);
                break;

            case ElementType.Sparkline:
                RenderSparklineElement(element, xPt, yPt, wPt, hPt, context);
                break;

            case ElementType.Table:
                RenderTableElement(element, xPt, yPt, wPt, hPt, context);
                break;
        }
    }

    private void RenderChartElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        if (element.Chart == null) return;
        if (element.Chart.DataBinding != null && context.MainDataRows.Count > 0)
        {
            Visuals.Charts.ChartDataBinder.Bind(element.Chart, context.MainDataRows);
        }
        var style = ResolveStyle(element, context.Report);
        var bounds = new SKRect(xPt, yPt, xPt + wPt, yPt + hPt);
        Visuals.Charts.SkiaChartRenderer.RenderChart(_canvas, element.Chart, bounds, style?.FontFamily);
    }

    private void RenderSparklineElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        if (element.Sparkline == null) return;
        var bounds = new SKRect(xPt, yPt, xPt + wPt, yPt + hPt);
        Visuals.Charts.SparklineRenderer.RenderSparkline(_canvas, element.Sparkline, bounds);
    }

    private void RenderBarcodeElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        var val = context.ResolveExpressionOrValue(element.Text, element.Expression)?.ToString() ?? string.Empty;
        var barcodeType = element.BarcodeType ?? BarcodeType.Code128;
        var showText = element.ShowBarcodeText ?? true;
        var textSize = element.BarcodeTextSize.HasValue ? (float)element.BarcodeTextSize.Value : (float?)null;
        Visuals.BarcodeRenderer.RenderBarcode(_canvas, val, barcodeType, xPt, yPt, wPt, hPt, showText, textSize);
    }

    private void RenderQrCodeElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        var val = context.ResolveExpressionOrValue(element.Text, element.Expression)?.ToString() ?? string.Empty;
        var ecc = element.QrEccLevel ?? QrEccLevel.M;
        Visuals.QrCodeRenderer.RenderQrCode(_canvas, val, ecc, xPt, yPt, wPt, hPt);
    }

    private void RenderImageElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        var source = context.ResolveExpressionOrValue(element.ImageSource, element.Expression)?.ToString() ?? element.ImageSource ?? string.Empty;
        Visuals.ImageRenderer.RenderImage(_canvas, source, xPt, yPt, wPt, hPt, Visuals.ImageFitMode.Fit, 1.0f, 0.0f, element.MaxDpi, element.ImageQuality);
    }

    private void RenderTableElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        if (element.Table == null) return;
        var colWidths = Visuals.TableRenderer.CalculateColumnWidths(element.Table.Columns, wPt, _unit);
        float currentY = yPt;

        // Header
        if (element.Table.Header != null)
        {
            var headerHeight = Visuals.TableRenderer.MeasureRowHeight(element.Table.Header, colWidths, _unit, context, context.Report);
            Visuals.TableRenderer.RenderRow(_canvas, element.Table.Header, xPt, currentY, colWidths, headerHeight, _unit, context, context.Report);
            currentY += headerHeight;
        }

        // Rows
        for (int i = 0; i < element.Table.Rows.Count; i++)
        {
            var row = element.Table.Rows[i];
            var rowHeight = Visuals.TableRenderer.MeasureRowHeight(row, colWidths, _unit, context, context.Report);
            var bg = (i % 2 == 1 && !string.IsNullOrWhiteSpace(element.Table.AlternatingRowBackground))
                ? element.Table.AlternatingRowBackground
                : null;

            Visuals.TableRenderer.RenderRow(_canvas, row, xPt, currentY, colWidths, rowHeight, _unit, context, context.Report, bg);
            currentY += rowHeight;
        }

        // Footer
        if (element.Table.Footer != null)
        {
            var footerHeight = Visuals.TableRenderer.MeasureRowHeight(element.Table.Footer, colWidths, _unit, context, context.Report);
            Visuals.TableRenderer.RenderRow(_canvas, element.Table.Footer, xPt, currentY, colWidths, footerHeight, _unit, context, context.Report);
        }
    }

    private void RenderTextElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        var style = ResolveStyle(element, context.Report);
        var textValue = context.ResolveExpressionOrValue(element.Text, element.Expression)?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(textValue))
        {
            return;
        }

        if (element.CanGrow || textValue.Contains('\n'))
        {
            DrawMultiLineText(_canvas, textValue, xPt, yPt, wPt, hPt, style);
        }
        else
        {
            var typeface = FontManager.Instance.GetTypeface(style?.FontFamily);
            using var paint = new SKPaint
            {
                IsAntialias = true,
                Color = ParseColor(style?.Color ?? "#000000"),
                TextSize = (float)(style?.FontSize ?? 10.0),
                Typeface = typeface
            };

            var fontMetrics = paint.FontMetrics;
            var baselineY = yPt + hPt - fontMetrics.Descent;

            var align = style?.Align ?? HorizontalAlign.Left;
            float textX = xPt;

            if (align == HorizontalAlign.Center)
            {
                paint.TextAlign = SKTextAlign.Center;
                textX = xPt + (wPt / 2.0f);
            }
            else if (align == HorizontalAlign.Right)
            {
                paint.TextAlign = SKTextAlign.Right;
                textX = xPt + wPt;
            }
            else
            {
                paint.TextAlign = SKTextAlign.Left;
            }

            using var shaper = new Fonts.HarfBuzzTextShaper();
            shaper.ShapeAndDrawText(_canvas, textValue, textX, baselineY, paint, typeface);
        }
    }

    public static void DrawMultiLineText(SKCanvas canvas, string text, float xPt, float yPt, float wPt, float hPt, StyleDefinition? style)
    {
        if (string.IsNullOrEmpty(text)) return;

        var typeface = FontManager.Instance.GetTypeface(style?.FontFamily);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(style?.Color ?? "#000000"),
            TextSize = (float)(style?.FontSize > 0 ? style.FontSize : 10.0),
            Typeface = typeface
        };

        var fontMetrics = paint.FontMetrics;
        var lineHeight = fontMetrics.Descent - fontMetrics.Ascent + fontMetrics.Leading;
        if (lineHeight <= 0) lineHeight = paint.TextSize * 1.25f;

        var lines = WrapTextLines(text, paint, wPt);
        var align = style?.Align ?? HorizontalAlign.Left;

        using var shaper = new Fonts.HarfBuzzTextShaper();
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            float textX = xPt;

            if (align == HorizontalAlign.Center)
            {
                paint.TextAlign = SKTextAlign.Center;
                textX = xPt + (wPt / 2.0f);
            }
            else if (align == HorizontalAlign.Right)
            {
                paint.TextAlign = SKTextAlign.Right;
                textX = xPt + wPt;
            }
            else
            {
                paint.TextAlign = SKTextAlign.Left;
            }

            var lineY = yPt + (i + 1) * lineHeight - fontMetrics.Descent;
            shaper.ShapeAndDrawText(canvas, line, textX, lineY, paint, typeface);
        }
    }

    public static float MeasureTextHeight(string text, float widthPt, StyleDefinition? style)
    {
        if (string.IsNullOrEmpty(text) || widthPt <= 0) return 0f;

        var typeface = FontManager.Instance.GetTypeface(style?.FontFamily);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            TextSize = (float)(style?.FontSize > 0 ? style.FontSize : 10.0),
            Typeface = typeface
        };

        var fontMetrics = paint.FontMetrics;
        var lineHeight = fontMetrics.Descent - fontMetrics.Ascent + fontMetrics.Leading;
        if (lineHeight <= 0) lineHeight = paint.TextSize * 1.25f;

        var lines = WrapTextLines(text, paint, widthPt);
        return Math.Max(lineHeight, lines.Count * lineHeight);
    }

    public static List<string> WrapTextLines(string text, SKPaint paint, float maxWidthPt)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;

        var rawParagraphs = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        foreach (var paragraph in rawParagraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ');
            var currentLine = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                var testLine = currentLine.Length == 0 ? word : currentLine.ToString() + " " + word;
                var measuredWidth = paint.MeasureText(testLine);

                if (measuredWidth > maxWidthPt && currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();

                    if (paint.MeasureText(word) > maxWidthPt)
                    {
                        var wordChars = word.ToCharArray();
                        var charLine = new System.Text.StringBuilder();
                        foreach (var ch in wordChars)
                        {
                            if (paint.MeasureText(charLine.ToString() + ch) > maxWidthPt && charLine.Length > 0)
                            {
                                lines.Add(charLine.ToString());
                                charLine.Clear();
                            }
                            charLine.Append(ch);
                        }
                        if (charLine.Length > 0)
                        {
                            currentLine.Append(charLine.ToString());
                        }
                    }
                    else
                    {
                        currentLine.Append(word);
                    }
                }
                else if (measuredWidth > maxWidthPt && currentLine.Length == 0)
                {
                    var wordChars = word.ToCharArray();
                    var charLine = new System.Text.StringBuilder();
                    foreach (var ch in wordChars)
                    {
                        if (paint.MeasureText(charLine.ToString() + ch) > maxWidthPt && charLine.Length > 0)
                        {
                            lines.Add(charLine.ToString());
                            charLine.Clear();
                        }
                        charLine.Append(ch);
                    }
                    if (charLine.Length > 0)
                    {
                        currentLine.Append(charLine.ToString());
                    }
                }
                else
                {
                    currentLine.Append(currentLine.Length == 0 ? word : " " + word);
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }
        }

        return lines;
    }

    public void DrawDirectText(string text, float xPt, float yPt, float wPt, float hPt, StyleDefinition? style)
    {
        if (string.IsNullOrEmpty(text)) return;
        var typeface = FontManager.Instance.GetTypeface(style?.FontFamily);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(style?.Color ?? "#666666"),
            TextSize = (float)(style?.FontSize > 0 ? style.FontSize : 9.0),
            Typeface = typeface
        };

        var fontMetrics = paint.FontMetrics;
        var baselineY = yPt + hPt - fontMetrics.Descent;

        var align = style?.Align ?? HorizontalAlign.Left;
        float textX = xPt;

        if (align == HorizontalAlign.Center)
        {
            paint.TextAlign = SKTextAlign.Center;
            textX = xPt + (wPt / 2.0f);
        }
        else if (align == HorizontalAlign.Right)
        {
            paint.TextAlign = SKTextAlign.Right;
            textX = xPt + wPt;
        }
        else
        {
            paint.TextAlign = SKTextAlign.Left;
        }

        using var shaper = new Fonts.HarfBuzzTextShaper();
        shaper.ShapeAndDrawText(_canvas, text, textX, baselineY, paint, typeface);
    }

    public static bool IsPageDisplayMatch(PageDisplayMode mode, BandContext context)
    {
        return mode switch
        {
            PageDisplayMode.All => true,
            PageDisplayMode.FirstPageOnly => context.CurrentPageNumber == 1,
            PageDisplayMode.NotFirstPage => context.CurrentPageNumber > 1,
            PageDisplayMode.LastPageOnly => context.CurrentPageNumber == context.TotalPages,
            PageDisplayMode.NotLastPage => context.CurrentPageNumber < context.TotalPages,
            PageDisplayMode.OddPages => (context.CurrentPageNumber % 2) != 0,
            PageDisplayMode.EvenPages => (context.CurrentPageNumber % 2) == 0,
            _ => true
        };
    }

    private void RenderShapeElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.LightGray
        };

        var shapeType = element.ShapeType ?? ShapeType.Rectangle;
        var rect = SKRect.Create(xPt, yPt, wPt, hPt);

        switch (shapeType)
        {
            case ShapeType.Rectangle:
                _canvas.DrawRect(rect, paint);
                break;

            case ShapeType.RoundedRect:
                var radius = (float)element.CornerRadius;
                if (radius <= 0) radius = 4.0f;
                _canvas.DrawRoundRect(rect, radius, radius, paint);
                break;

            case ShapeType.Ellipse:
                _canvas.DrawOval(rect, paint);
                break;

            case ShapeType.Line:
                paint.Style = SKPaintStyle.Stroke;
                paint.StrokeWidth = 1.0f;
                _canvas.DrawLine(xPt, yPt, xPt + wPt, yPt + hPt, paint);
                break;
        }
    }

    private static StyleDefinition? ResolveStyle(ElementDefinition element, ReportDefinition report)
    {
        if (element.Style != null)
        {
            return element.Style;
        }

        if (!string.IsNullOrEmpty(element.StyleRef) && report.Styles.TryGetValue(element.StyleRef, out var refStyle))
        {
            return refStyle;
        }

        return null;
    }

    private static SKColor ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return SKColors.Black;
        }

        if (SKColor.TryParse(hex, out var skColor))
        {
            return skColor;
        }

        return SKColors.Black;
    }

    public static SKPicture RecordBandToPicture(BandDefinition band, UnitType unit, ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(band);
        ArgumentNullException.ThrowIfNull(report);

        using var recorder = new SKPictureRecorder();
        var hPt = UnitConverter.ToPoints(band.Height, unit);
        var (wPt, _) = UnitConverter.GetPageDimensionsInPoints(report.PageSetup);

        var canvas = recorder.BeginRecording(SKRect.Create(0, 0, wPt, hPt));
        var reportCanvas = new SkiaReportCanvas(canvas, unit);
        var context = new BandContext { Report = report };
        reportCanvas.RenderBand(band, 0, context);

        return recorder.EndRecording();
    }
}
