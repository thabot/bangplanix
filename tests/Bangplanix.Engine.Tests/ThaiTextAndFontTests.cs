using Bangplanix.Core.Formatting;
using Bangplanix.Engine.Fonts;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class GlobalTextAndFontTests
{
    [Theory]
    [InlineData(0, "ศูนย์บาทถ้วน")]
    [InlineData(1, "หนึ่งบาทถ้วน")]
    [InlineData(11, "สิบเอ็ดบาทถ้วน")]
    [InlineData(21, "ยี่สิบเอ็ดบาทถ้วน")]
    [InlineData(100, "หนึ่งร้อยบาทถ้วน")]
    [InlineData(1234.50, "หนึ่งพันสองร้อยสามสิบสี่บาทห้าสิบสตางค์")]
    [InlineData(1000000, "หนึ่งล้านบาทถ้วน")]
    [InlineData(1000001, "หนึ่งล้านหนึ่งบาทถ้วน")]
    [InlineData(0.25, "ยี่สิบห้าสตางค์")]
    [InlineData(87000.00, "แปดหมื่นเจ็ดพันบาทถ้วน")]
    public void BahtTextFormatter_ShouldConvertAccurately(decimal amount, string expectedText)
    {
        var result = BahtTextFormatter.ToBahtText(amount);
        result.Should().Be(expectedText);
    }

    [Fact]
    public void ThaiDateFormatter_ShouldFormatBuddhistEraCorrectly()
    {
        var testDate = new DateTime(2026, 9, 12);
        var formatted = ThaiDateFormatter.FormatBuddhistDate(testDate, "d MMMM yyyy");
        formatted.Should().Be("12 กันยายน 2569");

        var shortFormatted = ThaiDateFormatter.FormatBuddhistDate(testDate, "dd/MM/yyyy");
        shortFormatted.Should().Be("12/09/2569");
    }

    [Theory]
    [InlineData("ใบเสร็จรับเงิน / ที่อยู่ผู้เสียภาษีอากร ฿1,234.50", "Thai")]
    [InlineData("Bangplanix 报表引擎 / 企業報表 🚀", "Chinese Simplified & Traditional")]
    [InlineData("エンタープライズ レポート エンジン (日本語テスト)", "Japanese")]
    [InlineData("엔터프라이즈 리포팅 엔진 (한국어)", "Korean")]
    [InlineData("محرك التقارير للمؤسسات العالمية", "Arabic")]
    [InlineData("उद्यम रिपोर्टिंग इंजन (हिंदी)", "Hindi / Devanagari")]
    [InlineData("Báo cáo doanh nghiệp Bangplanix Việt Nam", "Vietnamese")]
    [InlineData("Übermäßige Geschäftsberichte Français & Español (Cyrillic: Отчет)", "European & Cyrillic")]
    public void HarfBuzzTextShaper_ShouldShapeGlobalMultiLanguageScripts(string globalText, string scriptName)
    {
        using var surface = SKSurface.Create(new SKImageInfo(500, 100));
        var canvas = surface.Canvas;

        var typeface = FontManager.Instance.GetTypeface(null);

        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14.0f,
            Typeface = typeface
        };

        using var shaper = new HarfBuzzTextShaper();
        var act = () => shaper.ShapeAndDrawText(canvas, globalText, 10, 50, paint, typeface);
        act.Should().NotThrow($"Shaping should succeed for {scriptName}");
    }

    [Fact]
    public void FontManager_MatchCharacter_ShouldResolveChineseAndArabicGlyphs()
    {
        // '中' (Chinese - U+4E2D)
        var cjkTf = FontManager.Instance.MatchCharacter(0x4E2D);
        cjkTf.Should().NotBeNull();

        // 'ع' (Arabic - U+0639)
        var arabicTf = FontManager.Instance.MatchCharacter(0x0639);
        arabicTf.Should().NotBeNull();

        // 'ก' (Thai - U+0E01)
        var thaiTf = FontManager.Instance.MatchCharacter(0x0E01);
        thaiTf.Should().NotBeNull();
    }
}
