using Bangplanix.Core.Models;
using Bangplanix.Core.Payments;
using Bangplanix.Engine.Visuals;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class VisualsAndBarcodeTests
{
    [Fact]
    public void PromptPayQrGenerator_MobileWithAmount_ShouldProduceValidEmvCoPayload()
    {
        var mobile = "081-234-5678";
        var amount = 87000.00m;

        var payload = PromptPayQrGenerator.GeneratePromptPayPayload(mobile, amount);

        payload.Should().NotBeNullOrWhiteSpace();
        payload.Should().StartWith("000201010212"); // Format 01 + Dynamic QR (12)
        payload.Should().Contain("A000000677010111"); // PromptPay Transfer AID
        payload.Should().Contain("0066812345678"); // Thai international mobile format
        payload.Should().Contain("5303764540887000.00"); // Currency THB (764) + Amount
        payload.Should().Contain("6304"); // CRC tag
        payload.Length.Should().BeGreaterThan(60);
    }

    [Fact]
    public void PromptPayQrGenerator_BillPayment_ShouldProduceValidPayload()
    {
        var billerId = "010753700001001";
        var ref1 = "INV20260001";
        var ref2 = "CUST999";
        var amount = 1500.50m;

        var payload = PromptPayQrGenerator.GenerateBillPaymentPayload(billerId, ref1, ref2, amount);

        payload.Should().StartWith("000201010212");
        payload.Should().Contain("A000000677010112"); // PromptPay Bill Payment AID
        payload.Should().Contain(billerId);
        payload.Should().Contain(ref1);
        payload.Should().Contain(ref2);
    }

    [Theory]
    [InlineData(BarcodeType.Code128, "INV-2026-0001", true, 12.0f)]
    [InlineData(BarcodeType.Code128, "INV-2026-0001", false, null)]
    [InlineData(BarcodeType.Code39, "CODE39TEST", true, 8.0f)]
    [InlineData(BarcodeType.EAN13, "8850123456789", false, null)]
    public void BarcodeRenderer_ShouldRender1DBarcodesWithOptions(BarcodeType barcodeType, string content, bool showText, float? textSize)
    {
        using var surface = SKSurface.Create(new SKImageInfo(300, 100));
        var canvas = surface.Canvas;

        var act = () => BarcodeRenderer.RenderBarcode(canvas, content, barcodeType, 10, 10, 200, 50, showText: showText, textSize: textSize);
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(QrEccLevel.L)]
    [InlineData(QrEccLevel.M)]
    [InlineData(QrEccLevel.Q)]
    [InlineData(QrEccLevel.H)]
    public void QrCodeRenderer_ShouldRender2DMatrixWithoutError(QrEccLevel eccLevel)
    {
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));
        var canvas = surface.Canvas;

        var content = "https://bangplanix.io/reports/verify?id=INV-2026-0001";
        var act = () => QrCodeRenderer.RenderQrCode(canvas, content, eccLevel, 10, 10, 150, 150);
        act.Should().NotThrow();
    }

    [Fact]
    public void ImageRenderer_Base641x1Png_ShouldRenderCorrectly()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        var canvas = surface.Canvas;

        // Valid 1x1 transparent PNG Base64
        var base64Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        var act = () => ImageRenderer.RenderImage(canvas, base64Png, 0, 0, 50, 50, ImageFitMode.Fit, opacity: 0.8f, rotationDegrees: 45.0f, maxDpi: 150, imageQuality: 80);
        act.Should().NotThrow();
    }

    [Fact]
    public void ImageRenderer_HighResDownsampling_ShouldOptimizeBitmapDimensions()
    {
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));
        var canvas = surface.Canvas;

        // Create a large 2000x2000 bitmap
        using var highResBmp = new SKBitmap(2000, 2000);
        using var highResCanvas = new SKCanvas(highResBmp);
        highResCanvas.Clear(SKColors.Blue);

        using var memStream = new MemoryStream();
        highResBmp.Encode(memStream, SKEncodedImageFormat.Png, 100);
        var base64 = Convert.ToBase64String(memStream.ToArray());

        // Target render area is only 50x50 pt with 72 DPI (max 50 px)
        var act = () => ImageRenderer.RenderImage(canvas, base64, 0, 0, 50, 50, ImageFitMode.Fit, maxDpi: 72, imageQuality: 75);
        act.Should().NotThrow();
    }
}
