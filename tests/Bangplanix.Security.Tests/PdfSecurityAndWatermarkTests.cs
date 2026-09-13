using System.Text;
using Bangplanix.Engine.Security;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Security.Tests;

public class PdfSecurityAndWatermarkTests
{
    [Fact]
    public void ApplySecurityEnvelopeShouldEmbedEncryptionDictionaryAndDRMFlags()
    {
        var rawPdf = "%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF"u8.ToArray();
        var policy = new PdfSecurityPolicy
        {
            UserPassword = "user123",
            OwnerPassword = "masterAdmin456",
            Permissions = PdfPermissions.PrintHighQuality | PdfPermissions.CopyContent,
            EnableAes256Encryption = true
        };

        var securedPdf = PdfSecurityEngine.ApplySecurityEnvelope(rawPdf, policy);

        var securedText = Encoding.ASCII.GetString(securedPdf);
        Assert.Contains("BANGPLANIX-DRM-AES256-SECURED", securedText, System.StringComparison.Ordinal);
        Assert.Contains("/Filter /Standard", securedText, System.StringComparison.Ordinal);
        Assert.Contains("/V 5", securedText, System.StringComparison.Ordinal);
        Assert.Contains("/R 6", securedText, System.StringComparison.Ordinal);
        Assert.Contains("/Length 256", securedText, System.StringComparison.Ordinal);
        Assert.Contains("/P ", securedText, System.StringComparison.Ordinal);
    }

    [Fact]
    public void RenderForensicWatermarkShouldDrawWatermarkOnCanvasWithoutExceptions()
    {
        using var surface = SKSurface.Create(new SKImageInfo(595, 842));
        var canvas = surface.Canvas;

        ForensicWatermarkEngine.RenderForensicWatermark(
            canvas,
            "CONFIDENTIAL - TENANT-99 - 192.168.1.10",
            595f,
            842f,
            opacity: 0.15f,
            angleDegrees: -30f
        );

        surface.Canvas.Flush();
        Assert.NotNull(surface);
    }
}
