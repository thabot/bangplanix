using SkiaSharp;

namespace Bangplanix.Engine.Visuals;

public enum ImageFitMode
{
    Fit,       // Preserve aspect ratio, fits entirely inside bounds
    Fill,      // Preserve aspect ratio, covers entire bounds (may crop)
    Stretch,   // Ignore aspect ratio, stretch to fill
    Center     // Actual size, centered
}

public static class ImageRenderer
{
    public static void RenderImage(
        SKCanvas canvas,
        string imageSource,
        float x,
        float y,
        float width,
        float height,
        ImageFitMode fitMode = ImageFitMode.Fit,
        float opacity = 1.0f,
        float rotationDegrees = 0.0f,
        int? maxDpi = null,
        int? imageQuality = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        if (string.IsNullOrWhiteSpace(imageSource))
        {
            return;
        }

        using var rawBitmap = LoadBitmap(imageSource);
        if (rawBitmap == null)
        {
            using var errPaint = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
            canvas.DrawRect(SKRect.Create(x, y, width, height), errPaint);
            return;
        }

        // Apply Resolution Downsampling & Compression to save space
        using var bitmap = OptimizeBitmap(rawBitmap, width, height, maxDpi, imageQuality);

        canvas.Save();

        using var paint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Color = SKColors.White.WithAlpha((byte)(Math.Clamp(opacity, 0f, 1f) * 255))
        };

        if (Math.Abs(rotationDegrees) > 0.01f)
        {
            var centerX = x + (width / 2.0f);
            var centerY = y + (height / 2.0f);
            canvas.RotateDegrees(rotationDegrees, centerX, centerY);
        }

        var destRect = CalculateDestRect(bitmap.Width, bitmap.Height, x, y, width, height, fitMode);
        canvas.DrawBitmap(bitmap, destRect, paint);

        canvas.Restore();
    }

    private static SKBitmap OptimizeBitmap(SKBitmap source, float targetWidthPt, float targetHeightPt, int? maxDpi, int? quality)
    {
        var effectiveDpi = maxDpi ?? 300; // Default 300 DPI print quality
        var maxPixelWidth = (int)Math.Ceiling(targetWidthPt * (effectiveDpi / 72.0f));
        var maxPixelHeight = (int)Math.Ceiling(targetHeightPt * (effectiveDpi / 72.0f));

        // Downsample if source is significantly larger than target resolution
        if (source.Width > maxPixelWidth * 1.1f || source.Height > maxPixelHeight * 1.1f)
        {
            var aspect = (float)source.Width / source.Height;
            int newW, newH;
            if (source.Width > source.Height)
            {
                newW = maxPixelWidth;
                newH = Math.Max(1, (int)(newW / aspect));
            }
            else
            {
                newH = maxPixelHeight;
                newW = Math.Max(1, (int)(newH * aspect));
            }

            var resizedInfo = new SKImageInfo(newW, newH, source.ColorType, source.AlphaType);
            var resizedBitmap = new SKBitmap(resizedInfo);
            if (source.ScalePixels(resizedBitmap.PeekPixels(), SKFilterQuality.Medium))
            {
                return ApplyQualityCompression(resizedBitmap, quality);
            }
            resizedBitmap.Dispose();
        }

        return ApplyQualityCompression(source.Copy(), quality);
    }

    private static SKBitmap ApplyQualityCompression(SKBitmap bitmap, int? quality)
    {
        if (!quality.HasValue || quality.Value >= 100 || quality.Value <= 0)
        {
            return bitmap;
        }

        try
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, Math.Clamp(quality.Value, 10, 95));
            if (data != null)
            {
                var compressedBitmap = SKBitmap.Decode(data);
                if (compressedBitmap != null)
                {
                    bitmap.Dispose();
                    return compressedBitmap;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or InvalidOperationException)
        {
            // Fallback to uncompressed bitmap
        }

        return bitmap;
    }

    private static SKRect CalculateDestRect(int bmpWidth, int bmpHeight, float x, float y, float w, float h, ImageFitMode mode)
    {
        if (mode == ImageFitMode.Stretch)
        {
            return SKRect.Create(x, y, w, h);
        }

        var aspectBmp = (float)bmpWidth / bmpHeight;
        var aspectDest = w / h;

        switch (mode)
        {
            case ImageFitMode.Fit:
                if (aspectBmp > aspectDest)
                {
                    var targetH = w / aspectBmp;
                    return SKRect.Create(x, y + ((h - targetH) / 2.0f), w, targetH);
                }
                else
                {
                    var targetW = h * aspectBmp;
                    return SKRect.Create(x + ((w - targetW) / 2.0f), y, targetW, h);
                }

            case ImageFitMode.Fill:
                if (aspectBmp > aspectDest)
                {
                    var targetW = h * aspectBmp;
                    return SKRect.Create(x + ((w - targetW) / 2.0f), y, targetW, h);
                }
                else
                {
                    var targetH = w / aspectBmp;
                    return SKRect.Create(x, y + ((h - targetH) / 2.0f), w, targetH);
                }

            case ImageFitMode.Center:
            default:
                return SKRect.Create(x + ((w - bmpWidth) / 2.0f), y + ((h - bmpHeight) / 2.0f), bmpWidth, bmpHeight);
        }
    }

    private static SKBitmap? LoadBitmap(string source)
    {
        try
        {
            if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var imageBytes = Bangplanix.Core.Security.AntiSsrfValidator.DownloadBytesSafelyAsync(source).GetAwaiter().GetResult();
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    return SKBitmap.Decode(imageBytes);
                }
            }

            if (source.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && source.Contains(";base64,", StringComparison.OrdinalIgnoreCase))
            {
                var base64 = source[(source.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase) + 8)..];
                var bytes = Convert.FromBase64String(base64);
                return SKBitmap.Decode(bytes);
            }

            if (File.Exists(source))
            {
                return SKBitmap.Decode(source);
            }

            if (source.Length > 100 && !source.Contains(' ', StringComparison.Ordinal) && !source.Contains('\\', StringComparison.Ordinal) && !source.Contains('/', StringComparison.Ordinal))
            {
                var bytes = Convert.FromBase64String(source);
                return SKBitmap.Decode(bytes);
            }
        }
        catch (Exception ex) when (ex is IOException or FormatException or System.Net.Http.HttpRequestException or ArgumentException)
        {
            // Silently fallback on invalid format
        }

        return null;
    }
}
