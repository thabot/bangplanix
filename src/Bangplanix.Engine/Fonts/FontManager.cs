using System.Collections.Concurrent;
using SkiaSharp;

namespace Bangplanix.Engine.Fonts;

public sealed class FontManager
{
    private static readonly Lazy<FontManager> LazyInstance = new(() => new FontManager());
    public static FontManager Instance => LazyInstance.Value;

    private readonly ConcurrentDictionary<string, SKTypeface> _typefaces = new(StringComparer.OrdinalIgnoreCase);

    // Comprehensive global multi-script fallback chain
    private readonly List<string> _fallbackChain =
    [
        // Thai & Latin
        "Sarabun", "Prompt", "Noto Sans Thai", "Noto Sans", "Tahoma", "Segoe UI", "Arial", "Calibri",
        // CJK (Simplified Chinese, Traditional Chinese, Japanese, Korean)
        "Microsoft YaHei", "PingFang SC", "SimSun", "Microsoft JhengHei", "Meiryo", "Yu Gothic", "MS Gothic", "Malgun Gothic", "Noto Sans SC", "Noto Sans TC", "Noto Sans JP", "Noto Sans KR", "Noto Sans CJK SC",
        // Arabic, Persian, Urdu
        "Segoe UI", "Traditional Arabic", "Noto Sans Arabic", "Dubai", "Amiri",
        // Indic (Devanagari, Hindi, Tamil, Bengali)
        "Nirmala UI", "Mangal", "Noto Sans Devanagari", "Latha", "Vrinda",
        // Southeast Asian (Khmer, Lao, Myanmar, Vietnamese)
        "Khmer UI", "Lao UI", "Myanmar Text", "Noto Sans Khmer", "Noto Sans Lao", "Noto Sans Myanmar",
        // Hebrew & Middle Eastern
        "David", "Noto Sans Hebrew",
        // Global Universal Unicode Fallback
        "Arial Unicode MS", "Segoe UI Symbol", "Segoe UI Emoji"
    ];

    private FontManager()
    {
        LoadFontsFromDirectory("./volumes/fonts");
    }

    public void RegisterTypeface(string familyName, SKTypeface typeface)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(typeface);
        _typefaces[familyName] = typeface;
    }

    public void RegisterBase64Font(string familyName, string base64FontData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(base64FontData);

        var fontBytes = Convert.FromBase64String(base64FontData);
        using var stream = new SKMemoryStream(fontBytes);
        var typeface = SKTypeface.FromStream(stream);
        if (typeface != null)
        {
            RegisterTypeface(familyName, typeface);
        }
    }

    public void LoadFontsFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        var fontFiles = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase));

        foreach (var file in fontFiles)
        {
            try
            {
                var typeface = SKTypeface.FromFile(file);
                if (typeface != null)
                {
                    _typefaces[typeface.FamilyName] = typeface;
                    var baseName = Path.GetFileNameWithoutExtension(file);
                    _typefaces[baseName] = typeface;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // Continue loading next font
            }
        }
    }

    public SKTypeface GetTypeface(string? familyName, SKFontStyleWeight weight = SKFontStyleWeight.Normal, SKFontStyleSlant slant = SKFontStyleSlant.Upright)
    {
        if (!string.IsNullOrWhiteSpace(familyName) && _typefaces.TryGetValue(familyName, out var customTypeface))
        {
            return customTypeface;
        }

        if (!string.IsNullOrWhiteSpace(familyName))
        {
            var systemTypeface = SKTypeface.FromFamilyName(familyName, (int)weight, (int)SKFontStyleWidth.Normal, slant);
            if (systemTypeface != null && systemTypeface.FamilyName.Equals(familyName, StringComparison.OrdinalIgnoreCase))
            {
                return systemTypeface;
            }
        }

        foreach (var fallback in _fallbackChain)
        {
            if (_typefaces.TryGetValue(fallback, out var fbCustom))
            {
                return fbCustom;
            }

            var fbSystem = SKTypeface.FromFamilyName(fallback, (int)weight, (int)SKFontStyleWidth.Normal, slant);
            if (fbSystem != null && fbSystem.FamilyName.Equals(fallback, StringComparison.OrdinalIgnoreCase))
            {
                return fbSystem;
            }
        }

        return SKTypeface.Default;
    }

    public SKTypeface MatchCharacter(int codepoint, string? preferredFamily = null)
    {
        // Try preferred font first
        var tf = GetTypeface(preferredFamily);
        if (tf.ContainsGlyph(codepoint))
        {
            return tf;
        }

        // Match from global system font manager
        var matched = SKFontManager.Default.MatchCharacter(codepoint);
        if (matched != null)
        {
            return matched;
        }

        // Match from registered custom fonts
        foreach (var custom in _typefaces.Values)
        {
            if (custom.ContainsGlyph(codepoint))
            {
                return custom;
            }
        }

        return tf;
    }
}
