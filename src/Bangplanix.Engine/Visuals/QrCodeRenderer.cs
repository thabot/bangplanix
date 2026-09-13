using Bangplanix.Core.Models;
using QRCoder;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals;

public static class QrCodeRenderer
{
    public static void RenderQrCode(
        SKCanvas canvas,
        string content,
        QrEccLevel eccLevel,
        float x,
        float y,
        float width,
        float height,
        SKColor? foregroundColor = null,
        SKColor? backgroundColor = null,
        SKBitmap? centerLogo = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        var qrEcc = MapEccLevel(eccLevel);
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(content, qrEcc);
        var matrix = qrData.ModuleMatrix;
        var moduleCount = matrix.Count;

        var fgColor = foregroundColor ?? SKColors.Black;
        var bgColor = backgroundColor ?? SKColors.White;

        // Draw background
        using var bgPaint = new SKPaint { Color = bgColor, Style = SKPaintStyle.Fill };
        canvas.DrawRect(SKRect.Create(x, y, width, height), bgPaint);

        using var fgPaint = new SKPaint { Color = fgColor, Style = SKPaintStyle.Fill, IsAntialias = false };
        var moduleSize = Math.Min(width, height) / moduleCount;

        var offsetX = x + ((width - (moduleCount * moduleSize)) / 2.0f);
        var offsetY = y + ((height - (moduleCount * moduleSize)) / 2.0f);

        for (int r = 0; r < moduleCount; r++)
        {
            for (int c = 0; c < moduleCount; c++)
            {
                if (matrix[r][c])
                {
                    var modRect = SKRect.Create(offsetX + (c * moduleSize), offsetY + (r * moduleSize), moduleSize, moduleSize);
                    canvas.DrawRect(modRect, fgPaint);
                }
            }
        }

        // Draw Center Logo if provided
        if (centerLogo != null)
        {
            var logoSize = Math.Min(width, height) * 0.22f;
            var logoX = offsetX + ((moduleCount * moduleSize - logoSize) / 2.0f);
            var logoY = offsetY + ((moduleCount * moduleSize - logoSize) / 2.0f);

            var logoBgRect = SKRect.Create(logoX - 2, logoY - 2, logoSize + 4, logoSize + 4);
            canvas.DrawRect(logoBgRect, bgPaint);

            var logoDestRect = SKRect.Create(logoX, logoY, logoSize, logoSize);
            canvas.DrawBitmap(centerLogo, logoDestRect);
        }
    }

    private static QRCodeGenerator.ECCLevel MapEccLevel(QrEccLevel eccLevel) => eccLevel switch
    {
        QrEccLevel.L => QRCodeGenerator.ECCLevel.L,
        QrEccLevel.M => QRCodeGenerator.ECCLevel.M,
        QrEccLevel.Q => QRCodeGenerator.ECCLevel.Q,
        QrEccLevel.H => QRCodeGenerator.ECCLevel.H,
        _ => QRCodeGenerator.ECCLevel.M
    };
}
