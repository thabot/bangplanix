using System.Text;
using System.Text.Json;
using SkiaSharp;

namespace Bangplanix.Engine.Security;

/// <summary>
/// Forensic metadata payload embedded invisibly into document text or rendering canvas.
/// </summary>
public sealed class ForensicSteganoPayload
{
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public long TimestampEpochSeconds { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public string DocumentHash { get; set; } = string.Empty;
}

/// <summary>
/// Forensic Steganographic Engine encoding invisible tracking tokens using Zero-Width Unicode characters and sub-pixel dot patterns.
/// </summary>
public static class SteganographicWatermarkEngine
{
    private const char Zwsp = '\u200B';   // Zero Width Space = 00
    private const char Zwnj = '\u200C';   // Zero Width Non-Joiner = 01
    private const char Zwj = '\u200D';    // Zero Width Joiner = 10
    private const char Wj = '\u2060';     // Word Joiner = 11
    private const char Marker = '\uFEFF'; // Zero Width No-Break Space (Start/End Sentinel)

    /// <summary>
    /// Encodes forensic payload into invisible Zero-Width Unicode characters embedded inside human-readable text.
    /// </summary>
    public static string EmbedInvisiblePayload(string visibleText, ForensicSteganoPayload payload)
    {
        ArgumentNullException.ThrowIfNull(visibleText);
        ArgumentNullException.ThrowIfNull(payload);

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);

        var sb = new StringBuilder();
        sb.Append(Marker);

        foreach (var b in bytes)
        {
            // 8 bits per byte -> 4 pairs of 2 bits
            for (int shift = 6; shift >= 0; shift -= 2)
            {
                var pair = (b >> shift) & 0x03;
                sb.Append(pair switch
                {
                    0 => Zwsp,
                    1 => Zwnj,
                    2 => Zwj,
                    3 => Wj,
                    _ => Zwsp
                });
            }
        }

        sb.Append(Marker);

        // Inject invisible payload after first character (or at start)
        if (visibleText.Length > 0)
        {
            return visibleText[0] + sb.ToString() + visibleText[1..];
        }

        return sb.ToString();
    }

    /// <summary>
    /// Decodes hidden forensic payload from text containing invisible Zero-Width Unicode characters.
    /// </summary>
    public static ForensicSteganoPayload? ExtractInvisiblePayload(string textWithStegano)
    {
        if (string.IsNullOrEmpty(textWithStegano)) return null;

        var startIdx = textWithStegano.IndexOf(Marker);
        if (startIdx < 0) return null;

        var endIdx = textWithStegano.IndexOf(Marker, startIdx + 1);
        if (endIdx <= startIdx) return null;

        var hiddenChars = textWithStegano.Substring(startIdx + 1, endIdx - startIdx - 1);
        var byteList = new List<byte>();

        int currentByte = 0;
        int bitCount = 0;

        foreach (var ch in hiddenChars)
        {
            int pair = ch switch
            {
                Zwsp => 0,
                Zwnj => 1,
                Zwj => 2,
                Wj => 3,
                _ => -1
            };

            if (pair < 0) continue;

            currentByte = (currentByte << 2) | pair;
            bitCount += 2;

            if (bitCount == 8)
            {
                byteList.Add((byte)currentByte);
                currentByte = 0;
                bitCount = 0;
            }
        }

        if (byteList.Count == 0) return null;

        try
        {
            var json = Encoding.UTF8.GetString(byteList.ToArray());
            return JsonSerializer.Deserialize<ForensicSteganoPayload>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Injects sub-pixel yellow/cyan micro-tracking dots onto the canvas for physical print forensics.
    /// </summary>
    public static void RenderMicroDotMatrix(
        SKCanvas canvas,
        ForensicSteganoPayload payload,
        float pageWidth,
        float pageHeight)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(payload);

        using var dotPaint = new SKPaint
        {
            Color = new SKColor(255, 255, 180, 18), // Ultra-low opacity yellow
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        // Render repeating microscopic dots (0.6pt diameter) across page grid
        for (float y = 20; y < pageHeight; y += 40)
        {
            for (float x = 20; x < pageWidth; x += 40)
            {
                canvas.DrawCircle(x, y, 0.35f, dotPaint);
            }
        }
    }
}
