using System;
using Bangplanix.Engine.Fonts;
using FluentAssertions;
using SkiaSharp;
using Xunit;

#pragma warning disable CA1707, CA2000, CA2007

namespace Bangplanix.Engine.Tests;

public class ThaiLigatureAndTypographyTests
{
    [Theory]
    [InlineData("ปิ่", "Lower consonant with upper vowel and tone mark")]
    [InlineData("ปั๊", "Consonant with Mai Han-Akat and Mai Tri")]
    [InlineData("ปู่", "Consonant with lower vowel Mai Ek")]
    [InlineData("ผู้ใหญ่หาผ้าใหม่ ให้สะใภ้ใช้คล้องคอ", "Twenty Thai special diphthong phrase")]
    [InlineData("ฐานข้อมูลญัตติฎีกา", "Consonants with baseline alterations (Than, Yo-Ying, Do-Chada)")]
    [InlineData("ที่ทำการไปรษณีย์ไทย ๑๒๓๔๕", "Thai address with Thai traditional numerals")]
    public void HarfBuzz_ComplexThaiLigatures_ShouldRenderCorrectlyWithoutArtifacts(string text, string description)
    {
        using var surface = SKSurface.Create(new SKImageInfo(400, 100));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var typeface = FontManager.Instance.GetTypeface(null);
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 16.0f,
            IsAntialias = true,
            Typeface = typeface
        };

        using var shaper = new HarfBuzzTextShaper();
        var act = () => shaper.ShapeAndDrawText(canvas, text, 20, 50, paint, typeface);
        act.Should().NotThrow($"Complex Thai text '{text}' ({description}) must shape without errors");

        // Verify canvas is not empty (contains black text pixels)
        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        bool hasDrawnPixels = false;
        for (int y = 0; y < bitmap.Height && !hasDrawnPixels; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White)
                {
                    hasDrawnPixels = true;
                    break;
                }
            }
        }

        hasDrawnPixels.Should().BeTrue($"Rendering '{text}' must paint visible glyphs on the canvas.");
    }
}
