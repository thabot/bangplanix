using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;
using Bangplanix.Engine.Pdf;
using Bangplanix.Engine.Security;
using Bangplanix.Engine.Typography;
using Xunit;

namespace Bangplanix.Security.Tests;

public class PadesSignatureStandardComplianceTests
{
    private static byte[] CreateSamplePdf()
    {
        return Encoding.UTF8.GetBytes(
            "%PDF-1.7\n" +
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n" +
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n" +
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>\nendobj\n" +
            "xref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \n" +
            "trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n185\n%%EOF"
        );
    }

    private static X509Certificate2 GenerateSelfSignedCert(string subjectName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={subjectName}, O=Bangplanix Test, C=TH", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return cert;
    }

    [Fact]
    public void SignPdf_WithDigitalSignatureOptions_ProducesValidPadesSignature()
    {
        var rawPdf = CreateSamplePdf();
        using var cert = GenerateSelfSignedCert("Bangplanix Sovereign Signer");
        var certBytes = cert.Export(X509ContentType.Pfx, "Secret123!");

        var options = new DigitalSignatureOptions
        {
            PfxRawBytes = certBytes,
            PfxPassword = "Secret123!",
            SignerName = "Dr. Thabot Analyst",
            Reason = "Quarterly Compliance Verification",
            Location = "Bangkok Financial District",
            ContactInfo = "compliance@bangplanix.io",
            SubFilter = PadesSubFilter.EtsiCadesDetached,
            VisualAppearance = new VisualSignatureAppearance
            {
                PageNumber = 1,
                X = 100,
                Y = 200,
                Width = 220,
                Height = 80,
                Title = "Chief Security Officer",
                ShowSignerDetails = true
            }
        };

        var signedPdf = PdfDigitalSigner.SignPdf(rawPdf, options);
        Assert.NotNull(signedPdf);
        Assert.True(signedPdf.Length > rawPdf.Length);

        var pdfString = Encoding.UTF8.GetString(signedPdf);
        Assert.Contains("/Type /Sig", pdfString);
        Assert.Contains("/Filter /Adobe.PPKLite", pdfString);
        Assert.Contains("/SubFilter /ETSI.CAdES.detached", pdfString);
        Assert.Contains("/ByteRange", pdfString);
        Assert.Contains("/Type /Annot", pdfString);
        Assert.Contains("/Subtype /Widget", pdfString);

        var verifyResult = PdfDigitalSigner.VerifySignature(signedPdf);
        Assert.True(verifyResult.IsValid);
        Assert.Equal("Dr. Thabot Analyst", verifyResult.SignerName);
        Assert.Equal("Quarterly Compliance Verification", verifyResult.Reason);
        Assert.Equal("Bangkok Financial District", verifyResult.Location);
    }

    [Fact]
    public void SignPdf_WithPostQuantumAndInvisibleWatermark_EmbedsForensicsAndPqcEnvelope()
    {
        var rawPdf = CreateSamplePdf();
        using var cert = GenerateSelfSignedCert("Quantum Auditor");
        var certBytes = cert.Export(X509ContentType.Pfx, "quantum");

        var options = new DigitalSignatureOptions
        {
            PfxRawBytes = certBytes,
            PfxPassword = "quantum",
            SignerName = "Quantum Auditor",
            EnableQuantumSafeMldsa = true,
            ForensicWatermark = new InvisibleWatermarkOptions
            {
                EnableTracking = true,
                TrackingPayload = "AUDIT-TOKEN-998877"
            }
        };

        var signedPdf = PdfDigitalSigner.SignPdf(rawPdf, options);
        Assert.NotNull(signedPdf);

        var pdfString = Encoding.UTF8.GetString(signedPdf);
        Assert.Contains("/PQC", pdfString);

        // Verify watermark extraction via InvisibleWatermarkEngine
        var extractedWatermark = InvisibleWatermarkEngine.ExtractSteganographicPayload(signedPdf);
        Assert.NotNull(extractedWatermark);
        Assert.Equal("AUDIT-TOKEN-998877", extractedWatermark);
    }

    [Fact]
    public void SignPdf_WithAuditTrailAndXmlAttachment_AppendsAuditPageAndAttachment()
    {
        var rawPdf = CreateSamplePdf();
        using var cert = GenerateSelfSignedCert("Enterprise Issuer");
        var certBytes = cert.Export(X509ContentType.Pfx, "enterprise");

        var options = new DigitalSignatureOptions
        {
            PfxRawBytes = certBytes,
            PfxPassword = "enterprise",
            SignerName = "Enterprise Issuer",
            AuditTrailOptions = new AuditTrailReportOptions
            {
                SignerIpAddress = "10.0.0.42",
                SignerDeviceUserAgent = "Bangplanix Sovereign Agent 2026",
                IncludeVerificationQrCode = true,
                QrVerificationBaseUrl = "http://localhost:9545/verify?docId="
            },
            ExtraXmlAttachments = new List<PdfXmlAttachmentPayload>
            {
                new()
                {
                    FileName = "invoice.xml",
                    Description = "ETDA e-Tax Invoice Data",
                    XmlContent = "<Invoice><Total>1500.00</Total></Invoice>"
                }
            }
        };

        var signedPdf = PdfDigitalSigner.SignPdf(rawPdf, options);
        Assert.NotNull(signedPdf);

        var pdfString = Encoding.UTF8.GetString(signedPdf);
        Assert.Contains("invoice.xml", pdfString);
    }

    [Fact]
    public void SmartAnchorTagScanner_DetectsTagsAndResolvesAppearance()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Smart Sign Test" },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 100,
                    Elements = new List<ElementDefinition>
                    {
                        new()
                        {
                            Type = ElementType.Text,
                            X = 50,
                            Y = 30,
                            Width = 200,
                            Height = 40,
                            Text = "Approved: [[SIG:Approver]]"
                        }
                    }
                }
            }
        };

        var placementOptions = new AnchorTagPlacementOptions
        {
            AnchorTagName = "[[SIG:Approver]]",
            SignatureBoxWidth = 180,
            SignatureBoxHeight = 50,
            Title = "Authorized Approver"
        };

        var tags = SmartAnchorTagScanner.ScanAnchorTags(report, placementOptions);
        Assert.NotEmpty(tags);
        Assert.Equal("[[SIG:Approver]]", tags[0].TagName);
        Assert.Equal("Approver", tags[0].TagArgument);

        var appearance = SmartAnchorTagScanner.ResolveAppearanceFromAnchor(report, placementOptions);
        Assert.NotNull(appearance);
        Assert.Equal(50, appearance.X);
        Assert.Equal(180, appearance.Width);
        Assert.Equal("Authorized Approver", appearance.Title);
    }

    [Fact]
    public void FontFallbackEngine_ShouldDetectAvailableGlyphsAndResolveFont()
    {
        // Standard Latin character should resolve
        var latinFont = FontFallbackEngine.ResolveSafeTypeface("Arial", "Bangplanix High Performance");
        Assert.NotNull(latinFont);

        // Thai script should resolve without throwing
        var thaiFont = FontFallbackEngine.ResolveSafeTypeface("Arial", "ระบบรายงานและลงนามดิจิทัล");
        Assert.NotNull(thaiFont);

        // Rare Unicode symbol should resolve without throwing
        var symbolFont = FontFallbackEngine.ResolveSafeTypeface("Arial", "🔒 🛡️ 📝");
        Assert.NotNull(symbolFont);
    }
}
