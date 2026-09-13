using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Security;
using Xunit;

namespace Bangplanix.Security.Tests;

public class SecurityExtensionsTests
{
    [Fact]
    public void ThaiETaxEngineShouldGenerateValidETDAXmlAndEmbedInPdf()
    {
        var invoice = new ThaiETaxInvoiceData
        {
            InvoiceNumber = "TAX-2026-0001",
            DocumentType = ThaiETaxDocType.TaxInvoice388,
            Seller = new ThaiTaxPayerInfo
            {
                Name = "บริษัท บางพลานิกซ์ จำกัด (สำนักงานใหญ่)",
                TaxId = "0105560012345",
                BranchId = "00000"
            },
            Buyer = new ThaiTaxPayerInfo
            {
                Name = "บริษัท ลูกค้าจำกัด (สาขาที่ 00001)",
                TaxId = "0105559098765",
                BranchId = "00001"
            },
            LineItems =
            {
                new ThaiETaxLineItem { ItemCode = "LIC-01", ItemDescription = "Bangplanix Enterprise License", Quantity = 1, UnitPrice = 50000m, VatRate = 7m }
            }
        };

        var xml = ThaiETaxEngine.GenerateThaiETaxXml(invoice);
        Assert.Contains("TaxInvoice_CrossIndustryInvoice", xml, StringComparison.Ordinal);
        Assert.Contains("ER3-2560", xml, StringComparison.Ordinal);
        Assert.Contains("010556001234500000", xml, StringComparison.Ordinal);
        Assert.Equal(50000m, invoice.SubTotal);
        Assert.Equal(3500m, invoice.VatTotal);
        Assert.Equal(53500m, invoice.GrandTotal);

        // TSA Token
        var digest = Encoding.UTF8.GetBytes(xml);
        var tsaToken = ThaiETaxEngine.GenerateTsaTimestampToken(digest);
        Assert.Contains("BEGIN TSA TIMESTAMP TOKEN", Encoding.UTF8.GetString(tsaToken), StringComparison.Ordinal);

        // PDF embedding
        var mockPdf = "%PDF-1.7\r\n1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n%%EOF";
        var pdfBytes = Encoding.Latin1.GetBytes(mockPdf);
        var embeddedPdf = ThaiETaxEngine.EmbedThaiETaxIntoPdf(pdfBytes, invoice, tsaToken);

        var resultStr = Encoding.Latin1.GetString(embeddedPdf);
        Assert.Contains("TaxInvoice.xml", resultStr, StringComparison.Ordinal);
    }

    [Fact]
    public void PiiAutoDiscoveryScannerShouldDetectPiiAndGenerateMaskingRules()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "PayrollAndCustomerReport" },
            Parameters =
            {
                new ParameterDefinition { Name = "customer_email" },
                new ParameterDefinition { Name = "report_year" }
            },
            Datasets =
            {
                new DatasetDefinition
                {
                    Name = "EmployeeData",
                    StaticData = new List<Dictionary<string, object?>>
                    {
                        new()
                        {
                            ["emp_id"] = "EMP001",
                            ["citizen_id"] = "1-2345-67890-12-3",
                            ["salary"] = 120000m,
                            ["phone_number"] = "089-123-4567"
                        }
                    }
                }
            }
        };

        var scanReport = PiiAutoDiscoveryScanner.ScanReport(report);

        Assert.Equal(4, scanReport.PiiFieldsCount);
        Assert.Contains(scanReport.Items, i => i.Category == PiiCategory.ThaiNationalId);
        Assert.Contains(scanReport.Items, i => i.Category == PiiCategory.SalaryFinancial);
        Assert.Contains(scanReport.Items, i => i.Category == PiiCategory.PhoneNumber);
        Assert.Contains(scanReport.Items, i => i.Category == PiiCategory.Email);

        // Auto-generate masking rules
        var rules = PiiAutoDiscoveryScanner.GenerateAutoMaskingRules(scanReport, ["Admin", "HRDirector"]);
        Assert.Equal(4, rules.Count);
        Assert.All(rules, r => Assert.Contains("Admin", r.ExemptRoles));
    }

    [Fact]
    public void DocumentTamperSealEngineShouldSealAndDetectPageTampering()
    {
        var page1 = Encoding.UTF8.GetBytes("Page 1: Contract Agreement Terms");
        var page2 = Encoding.UTF8.GetBytes("Page 2: Pricing and Financial Terms ($500,000)");
        var page3 = Encoding.UTF8.GetBytes("Page 3: Signatures and Execution");

        var pages = new List<byte[]> { page1, page2, page3 };

        var mockPdf = "%PDF-1.7\r\n1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n%%EOF";
        var pdfBytes = Encoding.Latin1.GetBytes(mockPdf);

        var sealedPdf = DocumentTamperSealEngine.SealPdfDocument(pdfBytes, pages, "SecretHMACKey");
        Assert.True(sealedPdf.Length > pdfBytes.Length);

        // 1. Verify authentic document
        var verifyValid = DocumentTamperSealEngine.VerifyPdfDocument(sealedPdf, pages, "SecretHMACKey");
        Assert.True(verifyValid.IsIntact);
        Assert.Empty(verifyValid.TamperedPageNumbers);

        // 2. Simulate malicious tamper on Page 2
        var tamperedPage2 = Encoding.UTF8.GetBytes("Page 2: Maliciously Altered Pricing ($50,000)");
        var tamperedPages = new List<byte[]> { page1, tamperedPage2, page3 };

        var verifyTampered = DocumentTamperSealEngine.VerifyPdfDocument(sealedPdf, tamperedPages, "SecretHMACKey");
        Assert.False(verifyTampered.IsIntact);
        Assert.Single(verifyTampered.TamperedPageNumbers);
        Assert.Equal(2, verifyTampered.TamperedPageNumbers[0]); // Page 2 detected as tampered!
    }
}
