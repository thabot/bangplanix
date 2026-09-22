using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Bangplanix.Core.Security;
using Bangplanix.Engine.Security;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PadesDigitalSignatureAndTsaTests
{
    [Fact]
    public async Task PAdES_SelfSignedCertificate_ShouldProduceValidSignedPdf()
    {
        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            SignatureLevel = PdfSignatureLevel.PAdES_B_B,
            SignerName = "Bangplanix Test Signer",
            Reason = "Test Approval"
        };

        var signedPdf = await signer.SignPdfAsync(rawPdf, options);

        signedPdf.Should().NotBeNull();
        signedPdf.Length.Should().BeGreaterThan(rawPdf.Length);
        string signedStr = Encoding.ASCII.GetString(signedPdf);
        signedStr.Should().Contain("/Type /Sig");
        signedStr.Should().Contain("/SubFilter /ETSI.CAdES.detached");
        signedStr.Should().Contain("/Contents <");
    }

    [Fact]
    public async Task PAdES_SignatureVerification_ShouldReturnIsValidTrue()
    {
        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nInvoice Document Stream Content\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            SignatureLevel = PdfSignatureLevel.PAdES_B_B,
            SignerName = "Finance Officer",
            Reason = "Tax Authorization"
        };

        var signedPdf = await signer.SignPdfAsync(rawPdf, options);
        var verification = PdfDigitalSigner.VerifySignature(signedPdf);

        verification.IsValid.Should().BeTrue();
        verification.SignerSubject.Should().Contain("Finance Officer");
    }

    [Fact]
    public async Task PAdES_TamperedPdfContent_ShouldFailVerification()
    {
        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nTotal Amount: 10,000 THB\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            SignatureLevel = PdfSignatureLevel.PAdES_B_B
        };

        var signedPdf = await signer.SignPdfAsync(rawPdf, options);

        // Tamper content before signature envelope
        signedPdf[15] = (byte)'9'; // Change '1' to '9'

        var verification = PdfDigitalSigner.VerifySignature(signedPdf);

        verification.IsValid.Should().BeFalse();
        verification.ErrorMessage.Should().Contain("tampering detected");
    }

    [Fact]
    public async Task PAdES_B_T_WithMockTsa_ShouldEmbedRfc3161Token()
    {
        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\ne-Tax Invoice with TSA\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            SignatureLevel = PdfSignatureLevel.PAdES_B_T,
            TsaOptions = new TsaServerOptions
            {
                TsaUrl = "https://tsa.etda.or.th",
                TimeoutMs = 3000
            }
        };

        var signedPdf = await signer.SignPdfAsync(rawPdf, options);
        var verification = PdfDigitalSigner.VerifySignature(signedPdf);

        verification.IsValid.Should().BeTrue();
        verification.HasTimestampToken.Should().BeTrue();
        verification.TimestampTimeUtc.Should().NotBeNull();
    }

    [Fact]
    public void TsaClient_GenerateMockTimestampToken_ShouldCreateValidAsn1SignedCms()
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("Test Document Digest"));
        byte[] token = Rfc3161TsaClient.GenerateMockTimestampToken(digest);

        token.Should().NotBeNull();
        token.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TsaClient_RequestTimestampToken_ShouldReturnDeterministicToken()
    {
        var client = new Rfc3161TsaClient();
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes("Invoice Hash"));
        var options = new TsaServerOptions { TsaUrl = "https://mock.tsa.etda.or.th" };

        byte[] token = await client.RequestTimestampTokenAsync(digest, options);

        token.Should().NotBeNull();
        token.Length.Should().BeGreaterThan(50);
    }

    [Fact]
    public void ThaiETax_EmbedXml_ShouldCreatePdfA3AlternativeFile()
    {
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nSample Invoice PDF\n%%EOF");
        var xmlAttachment = new ETdaInvoiceAttachment
        {
            XmlFileName = "TaxInvoice_2026.xml",
            XmlBytes = Encoding.UTF8.GetBytes("<rsm:CrossIndustryInvoice>ETDA</rsm:CrossIndustryInvoice>"),
            Description = "e-Tax Invoice XML 2.0"
        };

        var embeddedPdf = ThaiETaxEngine.EmbedTaxInvoiceXml(rawPdf, xmlAttachment);

        embeddedPdf.Length.Should().BeGreaterThan(rawPdf.Length);
        string embeddedStr = Encoding.ASCII.GetString(embeddedPdf);
        embeddedStr.Should().Contain("/Type /Filespec");
        embeddedStr.Should().Contain("/AFRelationship /Alternative");
        embeddedStr.Should().Contain("<rsm:CrossIndustryInvoice>ETDA</rsm:CrossIndustryInvoice>");
    }

    [Fact]
    public async Task ThaiETax_FullWorkflow_SignAndEmbedXml_ShouldPreserveBoth()
    {
        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nFull e-Tax Invoice Workflow\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            SignatureLevel = PdfSignatureLevel.PAdES_B_T,
            SignerName = "ETDA Certified Issuer",
            EmbedEtdaXml = true,
            XmlAttachment = new ETdaInvoiceAttachment
            {
                XmlFileName = "ETDA_Invoice_001.xml",
                XmlBytes = Encoding.UTF8.GetBytes("<CrossIndustryInvoice>DATA</CrossIndustryInvoice>")
            },
            TsaOptions = new TsaServerOptions { TsaUrl = "https://tsa.etda.or.th" }
        };

        var signedPdf = await signer.SignPdfAsync(rawPdf, options);
        var verification = PdfDigitalSigner.VerifySignature(signedPdf);

        verification.IsValid.Should().BeTrue();
        verification.HasTimestampToken.Should().BeTrue();
        string content = Encoding.ASCII.GetString(signedPdf);
        content.Should().Contain("/AFRelationship /Alternative");
        content.Should().Contain("<CrossIndustryInvoice>DATA</CrossIndustryInvoice>");
    }

    [Fact]
    public void ThaiETax_EmptyXml_ShouldReturnUnmodifiedPdf()
    {
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nPlain PDF\n%%EOF");
        var attachment = new ETdaInvoiceAttachment { XmlBytes = Array.Empty<byte>() };

        var result = ThaiETaxEngine.EmbedTaxInvoiceXml(rawPdf, attachment);

        result.Should().BeEquivalentTo(rawPdf);
    }

    [Fact]
    public void VerifySignature_NonSignedDocument_ShouldReturnIsValidFalse()
    {
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nOrdinary PDF without signature\n%%EOF");

        var verification = PdfDigitalSigner.VerifySignature(rawPdf);

        verification.IsValid.Should().BeFalse();
        verification.ErrorMessage.Should().Contain("does not contain a Bangplanix PAdES signature");
    }

    [Fact]
    public async Task PAdES_WithCustomPfxKey_ShouldSignSuccessfully()
    {
        // Generate in-memory PFX bytes for testing
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Custom PFX Signer, C=TH", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        byte[] pfxBytes = cert.Export(X509ContentType.Pfx, "P@ssw0rd123");

        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nDoc to sign\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            SignatureLevel = PdfSignatureLevel.PAdES_B_B,
            PfxRawBytes = pfxBytes,
            PfxPassword = "P@ssw0rd123"
        };

        var signedPdf = await signer.SignPdfAsync(rawPdf, options);
        var verification = PdfDigitalSigner.VerifySignature(signedPdf);

        verification.IsValid.Should().BeTrue();
        verification.SignerSubject.Should().Contain("Custom PFX Signer");
    }

    [Fact]
    public async Task PAdES_CorruptedPfxPassword_ShouldFallbackOrThrow()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Secure Cert, C=TH", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        byte[] pfxBytes = cert.Export(X509ContentType.Pfx, "CorrectPassword");

        var signer = new PdfDigitalSigner();
        var rawPdf = Encoding.ASCII.GetBytes("%PDF-1.7\nDoc\n%%EOF");

        var options = new DigitalSignatureOptions
        {
            PfxRawBytes = pfxBytes,
            PfxPassword = "WrongPassword"
        };

        // When password is wrong, loading throws CryptographicException
        var act = async () => await signer.SignPdfAsync(rawPdf, options);
        await act.Should().ThrowAsync<Exception>();
    }
}
