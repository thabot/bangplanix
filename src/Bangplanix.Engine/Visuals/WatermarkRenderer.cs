using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Fonts;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals;

public static class WatermarkRenderer
{
    public static void RenderWatermark(
        SKCanvas canvas,
        WatermarkDefinition watermark,
        float pageWidthPt,
        float pageHeightPt,
        BandContext context)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(watermark);
        ArgumentNullException.ThrowIfNull(context);

        var opacity = Math.Clamp(watermark.Opacity, 0.01f, 1.0f);
        var text = context.ResolveExpressionOrValue(watermark.Text, watermark.Expression)?.ToString() ?? watermark.Text;

        canvas.Save();
        try
        {
            if (!string.IsNullOrEmpty(text))
            {
                var style = watermark.Style;
                var fontSize = (float)(style?.FontSize > 0 ? style.FontSize : 54.0);
                var typeface = FontManager.Instance.GetTypeface(style?.FontFamily ?? "Sarabun");

                var color = ParseColor(style?.Color ?? "#888888");
                var alpha = (byte)(color.Alpha * opacity);
                var watermarkColor = color.WithAlpha(alpha);

                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    Color = watermarkColor,
                    TextSize = fontSize,
                    Typeface = typeface,
                    TextAlign = SKTextAlign.Center
                };

                // Center of page
                var centerX = pageWidthPt / 2.0f;
                var centerY = pageHeightPt / 2.0f;

                canvas.Translate(centerX, centerY);
                canvas.RotateDegrees(watermark.RotationAngle);

                var fontMetrics = paint.FontMetrics;
                var textBaselineY = -(fontMetrics.Ascent + fontMetrics.Descent) / 2.0f;

                using var shaper = new HarfBuzzTextShaper();
                shaper.ShapeAndDrawText(canvas, text, 0f, textBaselineY, paint, typeface);
            }
            else if (!string.IsNullOrEmpty(watermark.ImageSource))
            {
                var src = context.ResolveExpressionOrValue(watermark.ImageSource, watermark.Expression)?.ToString() ?? watermark.ImageSource;
                var centerX = pageWidthPt / 2.0f;
                var centerY = pageHeightPt / 2.0f;

                canvas.Translate(centerX, centerY);
                canvas.RotateDegrees(watermark.RotationAngle);

                var imgSize = Math.Min(pageWidthPt, pageHeightPt) * 0.5f;
                var rect = SKRect.Create(-imgSize / 2.0f, -imgSize / 2.0f, imgSize, imgSize);

                ImageRenderer.RenderImage(canvas, src, rect.Left, rect.Top, rect.Width, rect.Height, ImageFitMode.Fit, opacity);
            }
        }
        finally
        {
            canvas.Restore();
        }
    }

    private static SKColor ParseColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return new SKColor(128, 128, 128);
        if (SKColor.TryParse(hex, out var skColor)) return skColor;
        return new SKColor(128, 128, 128);
    }
}
