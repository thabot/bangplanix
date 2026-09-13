using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Bangplanix.Engine.Visuals.Charts;

public static class ChartColorPalette
{
    private static readonly string[] DefaultColors =
    [
        "#2563eb", "#3b82f6", "#06b6d4", "#10b981", "#84cc16",
        "#eab308", "#f97316", "#ef4444", "#8b5cf6", "#ec4899"
    ];

    private static readonly string[] CorporateColors =
    [
        "#1e3a8a", "#0284c7", "#0d9488", "#16a34a", "#ca8a04",
        "#d97706", "#dc2626", "#475569", "#4f46e5", "#0891b2"
    ];

    private static readonly string[] VibrantColors =
    [
        "#ec4899", "#8b5cf6", "#3b82f6", "#06b6d4", "#10b981",
        "#f59e0b", "#f97316", "#ef4444", "#6366f1", "#14b8a6"
    ];

    private static readonly string[] PastelColors =
    [
        "#93c5fd", "#a7f3d0", "#fde68a", "#fbcfe8", "#c4b5fd",
        "#fed7aa", "#bbf7d0", "#bae6fd", "#e9d5ff", "#fecdd3"
    ];

    private static readonly string[] EmeraldColors =
    [
        "#064e3b", "#047857", "#059669", "#10b981", "#34d399",
        "#6ee7b7", "#a7f3d0", "#14532d", "#166534", "#15803d"
    ];

    private static readonly string[] ThaiHeritageColors =
    [
        "#b45309", "#d97706", "#0f766e", "#0369a1", "#831843",
        "#7c2d12", "#431407", "#78350f", "#1e3a5f", "#4c1d95"
    ];

    public static SKColor GetColor(string? paletteName, int seriesIndex, List<string>? customColors = null)
    {
        if (customColors != null && customColors.Count > 0)
        {
            var hex = customColors[seriesIndex % customColors.Count];
            if (SKColor.TryParse(hex, out var customColor)) return customColor;
        }

        var palette = (paletteName?.ToLowerInvariant()) switch
        {
            "corporate" => CorporateColors,
            "vibrant" => VibrantColors,
            "pastel" => PastelColors,
            "emerald" => EmeraldColors,
            "thaiheritage" or "thaisilk" => ThaiHeritageColors,
            _ => DefaultColors
        };

        var selectedHex = palette[seriesIndex % palette.Length];
        return SKColor.TryParse(selectedHex, out var color) ? color : SKColors.DodgerBlue;
    }

    public static SKColor ParseColor(string? hex, SKColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        return SKColor.TryParse(hex, out var color) ? color : fallback;
    }
}
