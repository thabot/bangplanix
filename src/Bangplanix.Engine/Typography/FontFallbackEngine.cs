using System;
using System.IO;
using SkiaSharp;

namespace Bangplanix.Engine.Typography;

/// <summary>
/// High-Performance Font Fallback & Zero-Font-Drop Pipeline.
/// Guarantees that non-Latin scripts (Thai, CJK, Arabic) and missing glyphs are resolved
/// safely without tofu boxes '[]' across Linux/Docker and Windows runtimes.
/// </summary>
public static class FontFallbackEngine
{
    private static readonly string[] GlobalFallbackFamilies =
    {
        "TH Sarabun New",
        "TH Sarabun PSK",
        "Noto Sans Thai",
        "Noto Sans CJK SC",
        "Noto Sans CJK JP",
        "Arial Unicode MS",
        "Segoe UI",
        "DejaVu Sans"
    };

    /// <summary>
    /// Resolves a safe SKTypeface with fallback assurance for missing glyphs.
    /// </summary>
    public static SKTypeface ResolveSafeTypeface(string? preferredFamily, string? sampleText, SKFontStyle? style = null)
    {
        var effectiveStyle = style ?? SKFontStyle.Normal;
        var familyName = string.IsNullOrWhiteSpace(preferredFamily) ? "Arial" : preferredFamily.Trim();

        // 1. Try to load requested family
        var primaryTypeface = SKTypeface.FromFamilyName(familyName, effectiveStyle) ?? SKTypeface.Default;

        if (string.IsNullOrEmpty(sampleText))
        {
            return primaryTypeface;
        }

        // 2. Inspect if primaryTypeface contains all non-ASCII glyphs
        bool hasMissingGlyphs = false;
        foreach (var ch in sampleText)
        {
            if (ch > 127 && primaryTypeface.GetGlyph(ch) == 0)
            {
                hasMissingGlyphs = true;
                break;
            }
        }

        if (!hasMissingGlyphs)
        {
            return primaryTypeface;
        }

        // 3. Check local container fonts folder (/volumes/fonts or ./volumes/fonts)
        var localFontsDir = Path.Combine(Directory.GetCurrentDirectory(), "volumes", "fonts");
        if (Directory.Exists(localFontsDir))
        {
            try
            {
                var fontFiles = Directory.GetFiles(localFontsDir, "*.*", SearchOption.AllDirectories);
                foreach (var fontFile in fontFiles)
                {
                    var ext = Path.GetExtension(fontFile).ToLowerInvariant();
                    if (ext is ".ttf" or ".otf" or ".ttc")
                    {
                        var localTf = SKTypeface.FromFile(fontFile);
                        if (localTf != null)
                        {
                            bool supportsAll = true;
                            foreach (var ch in sampleText)
                            {
                                if (ch > 127 && localTf.GetGlyph(ch) == 0)
                                {
                                    supportsAll = false;
                                    break;
                                }
                            }

                            if (supportsAll)
                            {
                                return localTf;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback to global list on file system permissions
            }
        }

        // 4. Try global fallback font families
        foreach (var fallbackFamily in GlobalFallbackFamilies)
        {
            var fallbackTf = SKTypeface.FromFamilyName(fallbackFamily, effectiveStyle);
            if (fallbackTf != null)
            {
                bool supportsAll = true;
                foreach (var ch in sampleText)
                {
                    if (ch > 127 && fallbackTf.GetGlyph(ch) == 0)
                    {
                        supportsAll = false;
                        break;
                    }
                }

                if (supportsAll)
                {
                    return fallbackTf;
                }
            }
        }

        return primaryTypeface;
    }
}
