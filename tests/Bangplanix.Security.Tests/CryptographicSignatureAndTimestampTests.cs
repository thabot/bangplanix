using System.Security.Cryptography;
using System.Text;
using Bangplanix.Engine.Security;
using Xunit;

namespace Bangplanix.Security.Tests;

public class CryptographicSignatureAndTimestampTests
{
    [Fact]
    public void PdfDigitalSigner_ShouldComputeByteRangeAndSignPdf()
    {
        var dummyPdfBytes = Encoding.UTF8.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");

        var signedPdfBytes = PdfDigitalSigner.SignPdf(dummyPdfBytes, new PdfSigningOptions
        {
            SignerName = "Thabot Certified Signer",
            Reason = "Official Approval & Issuance",
            Location = "Bangkok, Thailand",
            ContactInfo = "thabot47@gmail.com"
        });

        Assert.NotNull(signedPdfBytes);
        Assert.True(signedPdfBytes.Length > dummyPdfBytes.Length);

        // Verify signed PDF contains PAdES / Adobe.PPKLite signature dictionary
        var signedPdfString = Encoding.UTF8.GetString(signedPdfBytes);
        Assert.Contains("/Type /Sig", signedPdfString, StringComparison.Ordinal);
        Assert.Contains("/Filter /Adobe.PPKLite", signedPdfString, StringComparison.Ordinal);
        Assert.Contains("/SubFilter /ETSI.CAdES.detached", signedPdfString, StringComparison.Ordinal);
        Assert.Contains("/ByteRange", signedPdfString, StringComparison.Ordinal);

        var verification = PdfDigitalSigner.VerifySignature(signedPdfBytes);
        Assert.True(verification.IsValid);
        Assert.Equal("Thabot Certified Signer", verification.SignerName);
        Assert.Equal("Official Approval & Issuance", verification.Reason);
    }

    [Fact]
    public void ThaiETaxEngine_ShouldGenerateEtdaCompliantXmlAndTsaTimestamp()
    {
        var invoiceModel = new ThaiETaxInvoiceData
        {
            InvoiceNumber = "TAX-INV-2026-00088",
            IssueDateTime = DateTime.UtcNow,
            Seller = new ThaiTaxPayerInfo
            {
                TaxId = "0105558123456",
                Name = "Bangplanix Technology Co., Ltd.",
                BranchId = "00000"
            },
            Buyer = new ThaiTaxPayerInfo
            {
                TaxId = "0105559876543",
                Name = "Customer Enterprise Public Company Limited",
                BranchId = "00001"
            }
        };

        invoiceModel.LineItems.Add(new ThaiETaxLineItem
        {
            ItemCode = "SRV-001",
            ItemDescription = "Enterprise Analytics Subscription",
            Quantity = 1,
            UnitPrice = 100_000.00m,
            VatRate = 7.0m
        });

        var xml = ThaiETaxEngine.GenerateThaiETaxXml(invoiceModel);

        Assert.NotNull(xml);
        Assert.Contains("TaxInvoice_CrossIndustryInvoice", xml, StringComparison.Ordinal);
        Assert.Contains("<ram:ID>TAX-INV-2026-00088</ram:ID>", xml, StringComparison.Ordinal);
        Assert.Contains("010555812345600000", xml, StringComparison.Ordinal);
        Assert.Contains("107000.00", xml, StringComparison.Ordinal);

        // Verify TSA Timestamp Token generation
        var dummyPdf = Encoding.UTF8.GetBytes("%PDF-1.7\nSample content for TSA sealing\n%%EOF");
        using var sha256 = SHA256.Create();
        var digest = sha256.ComputeHash(dummyPdf);
        var tsaToken = ThaiETaxEngine.GenerateTsaTimestampToken(digest, "ETDA-Certified-TSA");

        Assert.NotNull(tsaToken);
        Assert.True(tsaToken.Length > 0);
        var tsaString = Encoding.UTF8.GetString(tsaToken);
        Assert.Contains("-----BEGIN TSA TIMESTAMP TOKEN-----", tsaString, StringComparison.Ordinal);
        Assert.Contains("ETDA-Certified-TSA", tsaString, StringComparison.Ordinal);
    }

    [Fact]
    public void EInvoicingEngine_ShouldProduceFacturXAndPeppolXmlInvoices()
    {
        var invoiceData = new EInvoiceData
        {
            InvoiceNumber = "FACTURX-2026-099",
            IssueDate = DateTime.UtcNow,
            Buyer = new EInvoiceParty
            {
                Name = "European Commerce Corp",
                VatId = "FR12345678901"
            },
            Seller = new EInvoiceParty
            {
                Name = "Bangplanix Global Ltd",
                VatId = "TH0105558123456"
            }
        };

        invoiceData.LineItems.Add(new EInvoiceLineItem
        {
            ItemName = "Consulting Services",
            Quantity = 1,
            UnitPrice = 1000.00m,
            VatRatePercent = 25.0m
        });

        var facturXXml = EInvoicingEngine.GenerateFacturXXml(invoiceData);
        Assert.NotNull(facturXXml);
        Assert.Contains("CrossIndustryInvoice", facturXXml, StringComparison.Ordinal);
        Assert.Contains("FACTURX-2026-099", facturXXml, StringComparison.Ordinal);

        var peppolXml = EInvoicingEngine.GeneratePeppolXml(invoiceData);
        Assert.NotNull(peppolXml);
        Assert.Contains("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", peppolXml, StringComparison.Ordinal);
        Assert.Contains("urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0", peppolXml, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentTamperSealEngine_ShouldVerifyDocumentAuthenticity()
    {
        var pages = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("PAGE_1_FINANCIAL_SUMMARY_ORIGINAL"),
            Encoding.UTF8.GetBytes("PAGE_2_DETAIL_TRANSACTIONS_ORIGINAL"),
            Encoding.UTF8.GetBytes("PAGE_3_AUDIT_SIGN_OFF_ORIGINAL")
        };

        var secretKey = "secret-audit-tamper-key-42";
        var dummyPdf = Encoding.UTF8.GetBytes("%PDF-1.7\n<< /Type /Catalog >>\n%%EOF");

        var sealedPdf = DocumentTamperSealEngine.SealPdfDocument(dummyPdf, pages, secretKey);
        Assert.NotNull(sealedPdf);
        Assert.True(sealedPdf.Length > dummyPdf.Length);

        // Verify valid unaltered pages
        var validResult = DocumentTamperSealEngine.VerifyPdfDocument(sealedPdf, pages, secretKey);
        Assert.True(validResult.IsIntact);
        Assert.Empty(validResult.TamperedPageNumbers);

        // Tamper with Page 2
        var tamperedPages = new List<byte[]>
        {
            Encoding.UTF8.GetBytes("PAGE_1_FINANCIAL_SUMMARY_ORIGINAL"),
            Encoding.UTF8.GetBytes("PAGE_2_DETAIL_TRANSACTIONS_MALICIOUS_MODIFICATION"),
            Encoding.UTF8.GetBytes("PAGE_3_AUDIT_SIGN_OFF_ORIGINAL")
        };

        var tamperedResult = DocumentTamperSealEngine.VerifyPdfDocument(sealedPdf, tamperedPages, secretKey);
        Assert.False(tamperedResult.IsIntact);
        Assert.Contains(2, tamperedResult.TamperedPageNumbers); // Page 2 is 1-based (index 1 -> page 2)
    }
}
