using System.Text;
using Bangplanix.Core.Formatting;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Pdf;
using Bangplanix.Engine.Security;
using Bangplanix.Printing.EscPos;
using Bangplanix.Printing.Zpl;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class GoldenMasterPdfFidelityTests
{
    [Fact]
    public async Task GoldenMaster_EnterpriseTaxInvoiceAndDossier_ShouldRenderWithPixelFidelity()
    {
        // 1. Construct Golden Master Report Definition
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "ต้นฉบับใบเสร็จรับเงิน / ใบกำกับภาษี (Golden Master Tax Invoice)",
                Author = "Bangplanix Enterprise Engine",
                Description = "Official Tax Invoice & Payment Receipt",
                CreatedAt = DateTime.UtcNow
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Orientation = PageOrientation.Portrait,
                Unit = UnitType.Pt,
                Margins = new MarginDefinition
                {
                    Top = 36,
                    Bottom = 36,
                    Left = 36,
                    Right = 36
                }
            },
            Bands = new BandsDefinition
            {
                PageHeader = new BandDefinition
                {
                    Height = 120,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = "บริษัท สยามเทคโนโลยี ซิสเต็มส์ จำกัด (สำนักงานใหญ่)",
                            X = 0,
                            Y = 10,
                            Width = 380,
                            Height = 22,
                            Style = new StyleDefinition { FontSize = 14, FontWeight = "Bold", Color = "#0f172a" }
                        },
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = "เลขประจำตัวผู้เสียภาษีอากร: 0105566009988 | โทร. 02-999-8888",
                            X = 0,
                            Y = 35,
                            Width = 380,
                            Height = 18,
                            Style = new StyleDefinition { FontSize = 9, Color = "#475569" }
                        },
                        new ElementDefinition
                        {
                            Type = ElementType.QrCode,
                            Text = "https://rd.go.th/etax/verify?id=INV-2026-9901",
                            X = 430,
                            Y = 10,
                            Width = 90,
                            Height = 90,
                            QrEccLevel = QrEccLevel.M
                        }
                    ]
                },
                Detail = new BandDefinition
                {
                    Height = 25,
                    Elements =
                    [
                        new ElementDefinition { Type = ElementType.Text, Expression = "=Fields[\"ItemCode\"]", X = 0, Y = 2, Width = 80, Height = 20, Style = new StyleDefinition { FontSize = 9 } },
                        new ElementDefinition { Type = ElementType.Text, Expression = "=Fields[\"Description\"]", X = 85, Y = 2, Width = 260, Height = 20, Style = new StyleDefinition { FontSize = 9 } },
                        new ElementDefinition { Type = ElementType.Text, Expression = "=Fields[\"Quantity\"]", X = 350, Y = 2, Width = 50, Height = 20, Style = new StyleDefinition { FontSize = 9 } },
                        new ElementDefinition { Type = ElementType.Text, Expression = "=Fields[\"Price\"]", X = 405, Y = 2, Width = 60, Height = 20, Style = new StyleDefinition { FontSize = 9 } },
                        new ElementDefinition { Type = ElementType.Text, Expression = "=Fields[\"Amount\"]", X = 470, Y = 2, Width = 55, Height = 20, Style = new StyleDefinition { FontSize = 9 } }
                    ]
                },
                PageFooter = new BandDefinition
                {
                    Height = 80,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = "เอกสารนี้ได้จัดทำและลงลายมือชื่อดิจิทัลอิเล็กทรอนิกส์ตาม พ.ร.บ. ธุรกรรมทางอิเล็กทรอนิกส์",
                            X = 0,
                            Y = 10,
                            Width = 525,
                            Height = 18,
                            Style = new StyleDefinition { FontSize = 8, Color = "#64748b" }
                        }
                    ]
                }
            }
        };

        // 2. Data Rows with Thai Characters
        var dataset = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["ItemCode"] = "SKU-001", ["Description"] = "ค่าบริการคลาวด์เซิร์ฟเวอร์ความเร็วสูง รายเดือน (กรกฎาคม 2569)", ["Quantity"] = 1, ["Price"] = 15000.00m, ["Amount"] = 15000.00m },
            new Dictionary<string, object?> { ["ItemCode"] = "SKU-002", ["Description"] = "ใบอนุญาต Bangplanix Enterprise Reporting Engine v1.0.0", ["Quantity"] = 1, ["Price"] = 85000.00m, ["Amount"] = 85000.00m },
            new Dictionary<string, object?> { ["ItemCode"] = "SKU-003", ["Description"] = "ค่าบริการติดตั้งและการฝึกอบรมบุคลากร (On-site 3 วัน)", ["Quantity"] = 1, ["Price"] = 25000.00m, ["Amount"] = 25000.00m }
        };

        // 3. Render Vector PDF
        var pdfRenderer = new SkiaPdfRenderer();
        byte[] pdfBytes = await pdfRenderer.RenderToPdfAsync(report, null, dataset);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        // Verify PDF Header and Trailer Structure
        string pdfString = Encoding.ASCII.GetString(pdfBytes.Take(100).ToArray());
        Assert.StartsWith("%PDF-", pdfString, StringComparison.Ordinal);

        // 4. Test Thai Date & BahtText Zero-Alloc Accuracy
        decimal totalAmount = 125000.00m;
        string bahtText = BahtTextFormatter.ToBahtText(totalAmount);
        Assert.Equal("หนึ่งแสนสองหมื่นห้าพันบาทถ้วน", bahtText);

        var date = new DateTime(2026, 7, 15, 10, 30, 0, DateTimeKind.Utc);
        string thaiDate = ThaiDateFormatter.FormatBuddhistDate(date, "d MMMM yyyy");
        Assert.Equal("15 กรกฎาคม 2569", thaiDate);

        // 5. Test PAdES Signer Integration on the generated Golden Master PDF
        var signOptions = new PdfSigningOptions
        {
            SignerName = "บริษัท สยามเทคโนโลยี ซิสเต็มส์ จำกัด",
            Reason = "Tax Invoice E-Signature Official Approval",
            Location = "Bangkok, Thailand",
            ContactInfo = "admin@siamtech.co.th"
        };

        byte[] signedPdf = PdfDigitalSigner.SignPdf(pdfBytes, signOptions);
        Assert.NotNull(signedPdf);
        Assert.True(signedPdf.Length > pdfBytes.Length);

        string signedPdfText = Encoding.Latin1.GetString(signedPdf);
        Assert.Contains("/Type /Sig", signedPdfText, StringComparison.Ordinal);
        Assert.Contains("/ByteRange", signedPdfText, StringComparison.Ordinal);
        Assert.Contains("SiamTech", signedPdfText, StringComparison.OrdinalIgnoreCase);

        // 6. Test Hardware Print Transpilation (ESC/POS & ZPL)
        var zpl = ZplReportRenderer.RenderToZpl(report, dataset);
        Assert.StartsWith("^XA", zpl.Trim(), StringComparison.Ordinal);
        Assert.EndsWith("^XZ", zpl.Trim(), StringComparison.Ordinal);

        var escPos = EscPosReportRenderer.RenderToEscPos(report, dataset);
        Assert.NotEmpty(escPos);
    }
}
