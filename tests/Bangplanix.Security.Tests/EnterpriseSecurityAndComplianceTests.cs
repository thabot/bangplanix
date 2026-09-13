using System.Text;
using Bangplanix.Engine.Security;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Security.Tests;

public class EnterpriseSecurityAndComplianceTests
{
    [Fact]
    public void TrueVectorRedactorShouldSanitizeStreamAndDrawRedactionBox()
    {
        var rawContentStream = "q\r\nBT /F1 12 Tf (Confidential Customer Financials) Tj ET\r\nQ";
        var areas = new List<RedactionArea>
        {
            new() { PageIndex = 0, X = 50, Y = 100, Width = 200, Height = 30, ReplacementLabel = "[REDACTED]" }
        };

        var sanitized = TrueVectorRedactor.SanitizeContentStream(rawContentStream, areas);
        Assert.Contains("50.00 100.00 200.00 30.00 re", sanitized, StringComparison.OrdinalIgnoreCase);

        // Canvas redaction rendering
        using var surface = SKSurface.Create(new SKImageInfo(300, 300));
        var canvas = surface.Canvas;
        TrueVectorRedactor.RenderRedactionsOnCanvas(canvas, areas, 0);
    }

    [Fact]
    public void RoleBasedDataMaskerShouldMaskSensitiveDataUnlessExempt()
    {
        // Thai National ID
        Assert.Equal("1-XXXX-XXXXX-12-3", RoleBasedDataMasker.MaskThaiNationalId("1-2345-67890-12-3", 'X'));
        Assert.Equal("1-****-*****-12-3", RoleBasedDataMasker.MaskThaiNationalId("1-2345-67890-12-3", '*'));

        // Credit Card
        Assert.Equal("****-****-****-4444", RoleBasedDataMasker.MaskCreditCard("4111-2222-3333-4444"));

        // Email
        var maskedEmail = RoleBasedDataMasker.MaskEmail("john.doe@company.com");
        Assert.StartsWith("j", maskedEmail, StringComparison.Ordinal);
        Assert.EndsWith("e@company.com", maskedEmail, StringComparison.Ordinal);

        // Phone Number
        Assert.Equal("081-XXX-5678", RoleBasedDataMasker.MaskPhoneNumber("0812345678", 'X'));

        // Role-based dataset row masking
        var rows = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["id_card"] = "1-2345-67890-12-3",
                ["salary"] = 150000m,
                ["department"] = "Finance"
            }
        };

        var rules = new List<DataMaskingRule>
        {
            new() { FieldName = "id_card", MaskingType = DataMaskingType.ThaiNationalId, MaskChar = 'X', ExemptRoles = { "Admin", "Auditor" } },
            new() { FieldName = "salary", MaskingType = DataMaskingType.Salary, ExemptRoles = { "PayrollDirector" } }
        };

        // 1. Guest / User context (Non-exempt)
        var userContext = new UserSecurityContext { UserId = "emp123" };
        userContext.Roles.Add("Employee");

        var maskedRows = RoleBasedDataMasker.MaskRows(rows, rules, userContext);
        Assert.Equal("1-XXXX-XXXXX-12-3", maskedRows[0]["id_card"]?.ToString());
        Assert.Equal("***,***.00", maskedRows[0]["salary"]?.ToString());
        Assert.Equal("Finance", maskedRows[0]["department"]?.ToString());

        // 2. Admin context (Exempt on id_card, but not on salary)
        var adminContext = new UserSecurityContext { UserId = "admin1" };
        adminContext.Roles.Add("Admin");

        var adminRows = RoleBasedDataMasker.MaskRows(rows, rules, adminContext);
        Assert.Equal("1-2345-67890-12-3", adminRows[0]["id_card"]?.ToString());
        Assert.Equal("***,***.00", adminRows[0]["salary"]?.ToString());
    }

    [Fact]
    public void SteganographicWatermarkEngineShouldEncodeAndDecodeInvisiblePayload()
    {
        var payload = new ForensicSteganoPayload
        {
            TenantId = "tenant-enterprise-01",
            UserId = "user_9988",
            ClientIp = "192.168.1.100",
            DocumentHash = "SHA256_ABCDEF123456"
        };

        var originalText = "Official Financial Summary Q4";
        var embeddedText = SteganographicWatermarkEngine.EmbedInvisiblePayload(originalText, payload);

        // Visible length check: Contains zero-width invisible markers
        Assert.NotEqual(originalText.Length, embeddedText.Length);
        Assert.Contains("fficial Financial Summary Q4", embeddedText, StringComparison.Ordinal);

        // Decode payload
        var decoded = SteganographicWatermarkEngine.ExtractInvisiblePayload(embeddedText);
        Assert.NotNull(decoded);
        Assert.Equal(payload.TenantId, decoded.TenantId);
        Assert.Equal(payload.UserId, decoded.UserId);
        Assert.Equal(payload.ClientIp, decoded.ClientIp);
        Assert.Equal(payload.DocumentHash, decoded.DocumentHash);
    }

    [Fact]
    public void PdfDigitalSignerShouldEmbedPAdESSignatureDictionary()
    {
        var mockPdf = "%PDF-1.7\r\n1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n%%EOF";
        var pdfBytes = Encoding.Latin1.GetBytes(mockPdf);

        var options = new PdfSigningOptions
        {
            SignerName = "Bangplanix Authorized Signer",
            Reason = "Tax Document Final Approval",
            Location = "Bangkok"
        };

        var signedPdfBytes = PdfDigitalSigner.SignPdf(pdfBytes, options);
        Assert.True(signedPdfBytes.Length > pdfBytes.Length);

        var verification = PdfDigitalSigner.VerifySignature(signedPdfBytes);
        Assert.True(verification.IsValid);
        Assert.Equal("Bangplanix Authorized Signer", verification.SignerName);
        Assert.Equal("Tax Document Final Approval", verification.Reason);
    }

    [Fact]
    public void EInvoicingEngineShouldGenerateFacturXAndPeppolXmlAndEmbedInPdf()
    {
        var invoice = new EInvoiceData
        {
            InvoiceNumber = "INV-2026-0099",
            CurrencyCode = "THB",
            Seller = new EInvoiceParty { Name = "Bangplanix Co., Ltd.", VatId = "TH0105560000001" },
            Buyer = new EInvoiceParty { Name = "Global Enterprises Corp", VatId = "TH0105560000002" },
            LineItems =
            {
                new EInvoiceLineItem { LineId = "1", ItemName = "Reporting Engine License", Quantity = 1, UnitPrice = 100000m, VatRatePercent = 7m }
            }
        };

        // Factur-X
        var facturXXml = EInvoicingEngine.GenerateFacturXXml(invoice, EInvoiceProfile.Basic);
        Assert.Contains("CrossIndustryInvoice", facturXXml, StringComparison.Ordinal);
        Assert.Contains("INV-2026-0099", facturXXml, StringComparison.Ordinal);
        Assert.Contains("100000.00", facturXXml, StringComparison.Ordinal);

        // Peppol BIS 3.0
        var peppolXml = EInvoicingEngine.GeneratePeppolXml(invoice);
        Assert.Contains("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", peppolXml, StringComparison.Ordinal);
        Assert.Contains("INV-2026-0099", peppolXml, StringComparison.Ordinal);

        // Embed into PDF/A-3
        var mockPdf = "%PDF-1.7\r\n1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n%%EOF";
        var pdfBytes = Encoding.Latin1.GetBytes(mockPdf);
        var embeddedPdf = EInvoicingEngine.EmbedEInvoiceIntoPdf(pdfBytes, invoice, EInvoiceStandard.FacturX_ZUGFeRD);

        var resultString = Encoding.Latin1.GetString(embeddedPdf);
        Assert.Contains("/AF", resultString, StringComparison.Ordinal);
        Assert.Contains("factur-x.xml", resultString, StringComparison.Ordinal);
    }
}
