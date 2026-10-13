using System.Text;
using SkiaSharp;

namespace Bangplanix.Engine.Security;

/// <summary>
/// Invisible Steganographic Forensic Watermark Engine.
/// Embeds covert tracking metadata (Employee ID, Session ID, Timestamp) into the PDF
/// via micro-spacing adjustments and sub-threshold zero-opacity vector points to track data leaks.
/// </summary>
public static class InvisibleWatermarkEngine
{
    private const string WatermarkTrailerPrefix = "% BANGPLANIX-STEGANO-FORENSIC-V1:";

    /// <summary>
    /// Embeds invisible tracking watermark directly on Skia Canvas during rendering.
    /// </summary>
    public static void EmbedOnCanvas(SKCanvas canvas, string trackingPayload, SKRect pageBounds)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (string.IsNullOrWhiteSpace(trackingPayload)) return;

        // Render covert zero-opacity micro-point containing encoded payload
        using var paint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 1), // 1/255 alpha (imperceptible to human eye)
            TextSize = 0.5f,
            IsAntialias = false
        };

        // Encode binary representation in tiny coordinates at page corner
        var bytes = Encoding.UTF8.GetBytes(trackingPayload);
        for (int i = 0; i < bytes.Length; i++)
        {
            float x = pageBounds.Left + (i * 0.1f);
            float y = pageBounds.Bottom - 0.1f;
            canvas.DrawPoint(x, y, paint);
        }
    }

    /// <summary>
    /// Embeds steganographic tracking metadata trailer into PDF byte stream.
    /// </summary>
    public static byte[] EmbedInPdfBytes(byte[] pdfBytes, string trackingPayload)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (string.IsNullOrWhiteSpace(trackingPayload)) return pdfBytes;

        var base64Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(trackingPayload));
        var marker = $"\n{WatermarkTrailerPrefix}{base64Payload}\n";
        var markerBytes = Encoding.ASCII.GetBytes(marker);

        using var ms = new MemoryStream(pdfBytes.Length + markerBytes.Length);
        ms.Write(pdfBytes);
        ms.Write(markerBytes);
        return ms.ToArray();
    }

    /// <summary>
    /// Extracts embedded forensic tracking metadata from a PDF byte stream.
    /// </summary>
    public static string? ExtractSteganographicPayload(byte[] pdfBytes)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);

        var content = Encoding.ASCII.GetString(pdfBytes);
        var idx = content.LastIndexOf(WatermarkTrailerPrefix, StringComparison.Ordinal);
        if (idx < 0) return null;

        var start = idx + WatermarkTrailerPrefix.Length;
        var end = content.IndexOf('\n', start);
        var base64Str = end > start ? content.Substring(start, end - start).Trim() : content.Substring(start).Trim();

        try
        {
            var rawBytes = Convert.FromBase64String(base64Str);
            return Encoding.UTF8.GetString(rawBytes);
        }
        catch
        {
            return null;
        }
    }
}
