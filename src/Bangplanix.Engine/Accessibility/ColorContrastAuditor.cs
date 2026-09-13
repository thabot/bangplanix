using System;
using System.Globalization;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Accessibility;

/// <summary>
/// WCAG 2.1 Relative Luminance & Contrast Ratio calculation engine.
/// </summary>
public static class ColorContrastAuditor
{
    /// <summary>
    /// Computes the contrast ratio between two hex colors according to WCAG 2.1 formula.
    /// </summary>
    public static ColorContrastResult CalculateContrast(string foregroundHex, string backgroundHex)
    {
        var (fgR, fgG, fgB) = ParseHexRgb(foregroundHex);
        var (bgR, bgG, bgB) = ParseHexRgb(backgroundHex);

        double l1 = ComputeRelativeLuminance(fgR, fgG, fgB);
        double l2 = ComputeRelativeLuminance(bgR, bgG, bgB);

        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);

        double ratio = Math.Round((lighter + 0.05) / (darker + 0.05), 2);

        return new ColorContrastResult
        {
            Ratio = ratio,
            ForegroundHex = foregroundHex,
            BackgroundHex = backgroundHex
        };
    }

    /// <summary>
    /// Computes sRGB relative luminance per WCAG 2.1 specs.
    /// L = 0.2126 * R + 0.7152 * G + 0.0722 * B
    /// </summary>
    public static double ComputeRelativeLuminance(byte r, byte g, byte b)
    {
        double rs = LinearizeComponent(r / 255.0);
        double gs = LinearizeComponent(g / 255.0);
        double bs = LinearizeComponent(b / 255.0);

        return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs;
    }

    private static double LinearizeComponent(double c)
    {
        return c <= 0.04045
            ? c / 12.92
            : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    private static (byte R, byte G, byte B) ParseHexRgb(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return (0, 0, 0);

        string cleaned = hex.Trim().TrimStart('#');
        if (cleaned.Length == 3)
        {
            cleaned = string.Concat(cleaned[0], cleaned[0], cleaned[1], cleaned[1], cleaned[2], cleaned[2]);
        }
        else if (cleaned.Length == 8) // ARGB or RGBA, take RGB
        {
            cleaned = cleaned.Substring(2, 6);
        }

        if (cleaned.Length >= 6 &&
            byte.TryParse(cleaned.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) &&
            byte.TryParse(cleaned.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) &&
            byte.TryParse(cleaned.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return (r, g, b);
        }

        return (0, 0, 0);
    }
}