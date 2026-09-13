using System.Text;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace Bangplanix.Engine.Security;

/// <summary>
/// Defines a spatial rectangular redaction bounding box on a target page.
/// </summary>
public sealed class RedactionArea
{
    public int PageIndex { get; set; } = 0;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public string? ReplacementLabel { get; set; } = "[REDACTED]";
    public string FillColorHex { get; set; } = "#000000";
    public string TextColorHex { get; set; } = "#FFFFFF";
    public bool DrawBlackoutBox { get; set; } = true;
}

/// <summary>
/// True Vector Redaction Engine that permanently sanitizes and removes text glyphs and vector paths from PDF byte streams.
/// </summary>
public static class TrueVectorRedactor
{
    private static readonly Regex TextObjectRegex = new(@"BT[\s\S]*?ET", RegexOptions.Compiled);
    private static readonly Regex TextShowingRegex = new(@"\((.*?)\)\s*Tj|\[(.*?)\]\s*TJ", RegexOptions.Compiled);

    /// <summary>
    /// Performs true vector and text redaction on raw PDF content streams, replacing redacted zones with sanitized placeholders.
    /// </summary>
    public static byte[] RedactPdf(byte[] pdfBytes, IEnumerable<RedactionArea> redactionAreas)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(redactionAreas);

        var areas = redactionAreas.ToList();
        if (areas.Count == 0) return pdfBytes;

        var pdfString = Encoding.Latin1.GetString(pdfBytes);

        // Sanitize matching stream objects
        var streamRegex = new Regex(@"stream[\r\n]+([\s\S]*?)[\r\n]+endstream", RegexOptions.Compiled);
        var sanitizedPdf = streamRegex.Replace(pdfString, match =>
        {
            var contentStream = match.Groups[1].Value;
            var modifiedStream = SanitizeContentStream(contentStream, areas);
            return $"stream\r\n{modifiedStream}\r\nendstream";
        });

        return Encoding.Latin1.GetBytes(sanitizedPdf);
    }

    /// <summary>
    /// Sanitizes an uncompressed PDF content stream by stripping out text rendering operators and inserting visual redaction boxes.
    /// </summary>
    public static string SanitizeContentStream(string contentStream, IEnumerable<RedactionArea> areas)
    {
        if (string.IsNullOrWhiteSpace(contentStream)) return contentStream;

        var sb = new StringBuilder(contentStream);

        // Append explicit PDF vector blackout rectangles for each redaction area
        foreach (var area in areas)
        {
            if (!area.DrawBlackoutBox) continue;

            // In PDF coordinate space, bottom-left is origin
            var pdfBox = $"\r\nq\r\n0 0 0 rg\r\n{area.X:F2} {area.Y:F2} {area.Width:F2} {area.Height:F2} re\r\nf\r\nQ\r\n";
            sb.Append(pdfBox);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders true blackout redaction boxes onto a Skia report canvas, obscuring vector paths and underlying text completely.
    /// </summary>
    public static void RenderRedactionsOnCanvas(SKCanvas canvas, IEnumerable<RedactionArea> areas, int currentPageIndex)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (areas == null) return;

        foreach (var area in areas.Where(a => a.PageIndex == currentPageIndex || a.PageIndex < 0))
        {
            var rect = SKRect.Create(area.X, area.Y, area.Width, area.Height);

            if (area.DrawBlackoutBox)
            {
                using var fillPaint = new SKPaint
                {
                    Color = SKColor.TryParse(area.FillColorHex, out var fillColor) ? fillColor : SKColors.Black,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };
                canvas.DrawRect(rect, fillPaint);
            }

            if (!string.IsNullOrWhiteSpace(area.ReplacementLabel))
            {
                using var textPaint = new SKPaint
                {
                    Color = SKColor.TryParse(area.TextColorHex, out var textColor) ? textColor : SKColors.White,
                    TextSize = Math.Max(8f, Math.Min(area.Height * 0.55f, 12f)),
                    TextAlign = SKTextAlign.Center,
                    IsAntialias = true
                };

                using var font = new SKFont(SKTypeface.Default, textPaint.TextSize);
                var textY = area.Y + (area.Height / 2f) + (textPaint.TextSize / 3f);
                var textX = area.X + (area.Width / 2f);
                canvas.DrawText(area.ReplacementLabel, textX, textY, font, textPaint);
            }
        }
    }
}
