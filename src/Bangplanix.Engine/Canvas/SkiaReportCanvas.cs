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

        foreach (var element in band.Elements)
        {
            RenderElement(element, offsetYPt, context);
        }
    }

    public void RenderElement(ElementDefinition element, float offsetYPt, BandContext context)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(context);

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

    private void RenderTextElement(ElementDefinition element, float xPt, float yPt, float wPt, float hPt, BandContext context)
    {
        var style = ResolveStyle(element, context.Report);
        var textValue = context.ResolveExpressionOrValue(element.Text, element.Expression)?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(textValue))
        {
            return;
        }

        var typeface = FontManager.Instance.GetTypeface(style?.FontFamily);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(style?.Color ?? "#000000"),
            TextSize = (float)(style?.FontSize ?? 10.0),
            Typeface = typeface
        };

        // Text positioning
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

        // Shape with HarfBuzz to ensure Thai vowels and tone marks never overlap or float
        using var shaper = new Fonts.HarfBuzzTextShaper();
        shaper.ShapeAndDrawText(_canvas, textValue, textX, baselineY, paint, typeface);
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
