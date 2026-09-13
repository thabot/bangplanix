using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Bangplanix.Engine.Fonts;

public sealed class HarfBuzzTextShaper : IDisposable
{
    private readonly LruGlyphCache _cache = new();
    private bool _disposed;

    public void ShapeAndDrawText(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKPaint paint,
        SKTypeface typeface)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(paint);
        ArgumentNullException.ThrowIfNull(typeface);

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var cacheKey = $"{typeface.FamilyName}_{paint.TextSize}_{paint.TextAlign}_{text}";
        if (_cache.TryGet(cacheKey, out var cachedBlob) && cachedBlob != null)
        {
            canvas.DrawText(cachedBlob, x, y, paint);
            return;
        }

        using var shaper = new SKShaper(typeface);
        var result = shaper.Shape(text, paint);

        if (result != null && result.Points.Length > 0)
        {
            using var builder = new SKTextBlobBuilder();
            using var font = new SKFont(typeface, paint.TextSize);
            var run = builder.AllocatePositionedRun(font, result.Codepoints.Length);

            var glyphSpan = run.GetGlyphSpan();
            var posSpan = run.GetPositionSpan();

            for (int i = 0; i < result.Codepoints.Length; i++)
            {
                glyphSpan[i] = (ushort)result.Codepoints[i];
                posSpan[i] = result.Points[i];
            }

            var textBlob = builder.Build();
            if (textBlob != null)
            {
                _cache.Add(cacheKey, textBlob);
                canvas.DrawText(textBlob, x, y, paint);
                return;
            }
        }

        // Fallback standard draw
        canvas.DrawText(text, x, y, paint);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cache.Clear();
            _disposed = true;
        }
    }
}
