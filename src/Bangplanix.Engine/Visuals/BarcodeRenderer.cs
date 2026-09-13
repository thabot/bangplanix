using Bangplanix.Core.Models;
using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace Bangplanix.Engine.Visuals;

public static class BarcodeRenderer
{
    public static void RenderBarcode(
        SKCanvas canvas,
        string content,
        BarcodeType barcodeType,
        float x,
        float y,
        float width,
        float height,
        bool showText = true,
        float? textSize = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(canvas);

        var format = MapBarcodeFormat(barcodeType);
        var writer = new MultiFormatWriter();
        var hints = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.CHARACTER_SET, "UTF-8" },
            { EncodeHintType.MARGIN, 0 }
        };

        try
        {
            var bitMatrix = writer.encode(content, format, (int)Math.Max(10, width), (int)Math.Max(10, height), hints);
            DrawBitMatrix(canvas, bitMatrix, x, y, width, height, showText ? content : null, textSize ?? 9.0f);
        }
        catch (Exception ex) when (ex is System.FormatException or ArgumentException or InvalidOperationException or WriterException)
        {
            using var errPaint = new SKPaint { Color = SKColors.Red, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            canvas.DrawRect(SKRect.Create(x, y, width, height), errPaint);
            using var txtPaint = new SKPaint { Color = SKColors.Red, TextSize = 8, TextAlign = SKTextAlign.Center };
            canvas.DrawText("Invalid Barcode", x + (width / 2), y + (height / 2), txtPaint);
        }
    }

    private static void DrawBitMatrix(SKCanvas canvas, BitMatrix bitMatrix, float x, float y, float width, float height, string? text, float textSize)
    {
        var matrixWidth = bitMatrix.Width;
        var textPadding = string.IsNullOrEmpty(text) ? 0f : textSize + 3.0f;
        var barHeight = Math.Max(height - textPadding, 5.0f);
        var scaleX = width / matrixWidth;

        using var blackPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        for (int col = 0; col < matrixWidth; col++)
        {
            if (bitMatrix[col, 0])
            {
                var barRect = SKRect.Create(x + (col * scaleX), y, scaleX, barHeight);
                canvas.DrawRect(barRect, blackPaint);
            }
        }

        if (!string.IsNullOrEmpty(text))
        {
            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = textSize,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true
            };
            canvas.DrawText(text, x + (width / 2.0f), y + height - 1.0f, textPaint);
        }
    }

    private static BarcodeFormat MapBarcodeFormat(BarcodeType type) => type switch
    {
        BarcodeType.Code128 => BarcodeFormat.CODE_128,
        BarcodeType.GS1_128 => BarcodeFormat.CODE_128,
        BarcodeType.Code39 => BarcodeFormat.CODE_39,
        BarcodeType.EAN13 => BarcodeFormat.EAN_13,
        BarcodeType.EAN8 => BarcodeFormat.EAN_8,
        BarcodeType.UPCA => BarcodeFormat.UPC_A,
        BarcodeType.ITF14 => BarcodeFormat.ITF,
        _ => BarcodeFormat.CODE_128
    };
}
