using System;
using SkiaSharp;

namespace Bangplanix.Engine.Security;

public static class ForensicWatermarkEngine
{
    public static void RenderForensicWatermark(
        SKCanvas canvas,
        string watermarkText,
        float pageWidthPt,
        float pageHeightPt,
        float opacity = 0.12f,
        float angleDegrees = -35f,
        string? typefaceName = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (string.IsNullOrWhiteSpace(watermarkText))
        {
            return;
        }

        canvas.Save();

        var centerX = pageWidthPt / 2.0f;
        var centerY = pageHeightPt / 2.0f;

        canvas.Translate(centerX, centerY);
        canvas.RotateDegrees(angleDegrees);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.DarkSlateGray.WithAlpha((byte)(Math.Clamp(opacity, 0.01f, 1.0f) * 255)),
            TextSize = Math.Max(16f, Math.Min(pageWidthPt, pageHeightPt) * 0.055f),
            TextAlign = SKTextAlign.Center
        };

        if (!string.IsNullOrWhiteSpace(typefaceName))
        {
            using var tf = SKTypeface.FromFamilyName(typefaceName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            if (tf != null)
            {
                using var font = new SKFont(tf, paint.TextSize);
                // Draw multiple diagonal repeat lines
                canvas.DrawText(watermarkText, 0, -pageHeightPt * 0.25f, font, paint);
                canvas.DrawText(watermarkText, 0, 0, font, paint);
                canvas.DrawText(watermarkText, 0, pageHeightPt * 0.25f, font, paint);
                canvas.Restore();
                return;
            }
        }

        using var defaultTf = SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
        using var defaultFont = new SKFont(defaultTf ?? SKTypeface.Default, paint.TextSize);
        canvas.DrawText(watermarkText, 0, -pageHeightPt * 0.25f, defaultFont, paint);
        canvas.DrawText(watermarkText, 0, 0, defaultFont, paint);
        canvas.DrawText(watermarkText, 0, pageHeightPt * 0.25f, defaultFont, paint);

        canvas.Restore();
    }
}
