using Bangplanix.Core.Models;
using Bangplanix.Fluent;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class FluentDocumentTests
{
    private class TestInvoiceHeaderComponent : IReportComponent
    {
        public void Compose(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("บริษัท ตัวอย่าง อินโนเวชั่น จำกัด").Bold().FontSize(16);
                    col.Item().Text("123 อาคารทาวเวอร์ กรุงเทพฯ 10110").FontSize(10);
                });
                row.ConstantItem(100).AlignRight().Text("INVOICE #2026-001").Bold();
            });
        }
    }

    [Fact]
    public async Task MinimalDocument_RendersValidPdfBytes()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A4);
                page.Margin(15, UnitType.Mm);
                page.Content().Text("สวัสดีบางพลานิกซ์ - สวัสดีชาวโลก").FontSize(18).Bold();
            });
        });

        var pdfBytes = await doc.GeneratePdfAsync();

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void PageSetupAndMargins_ConfiguredCorrectly()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.Letter);
                page.Margin(20, UnitType.Mm);
                page.Orientation(PageOrientation.Landscape);
                page.Watermark("DRAFT - CONFIDENTIAL", 0.15f, -30f);
                page.Content().Text("Sample content");
            });
        });

        var report = doc.ToReportDefinition();

        report.PageSetup.PaperKind.Should().Be(PaperKind.Letter);
        report.PageSetup.Orientation.Should().Be(PageOrientation.Landscape);
        report.PageSetup.Margins.Left.Should().Be(20.0);
        report.PageSetup.Margins.Right.Should().Be(20.0);
        report.PageSetup.Margins.Top.Should().Be(20.0);
        report.PageSetup.Margins.Bottom.Should().Be(20.0);
        report.PageSetup.Watermark.Should().NotBeNull();
        report.PageSetup.Watermark!.Text.Should().Be("DRAFT - CONFIDENTIAL");
        report.PageSetup.Watermark.Opacity.Should().Be(0.15f);
        report.PageSetup.Watermark.RotationAngle.Should().Be(-30f);
    }

    [Fact]
    public void RowAndColumnLayout_CalculatesCoordinates()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A4);
                page.Margin(10, UnitType.Mm); // Available width = 210 - 20 = 190mm

                page.Content().Column(col =>
                {
                    col.Spacing(5, UnitType.Mm);

                    col.Item().Row(row =>
                    {
                        row.ConstantItem(50, UnitType.Mm).Text("Left Fixed 50mm");
                        row.RelativeItem(1.0).Text("Middle Flexible");
                        row.RelativeItem(1.0).Text("Right Flexible");
                    });

                    col.Item().Text("Second Row Below");
                });
            });
        });

        var report = doc.ToReportDefinition();
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Count.Should().Be(4);

        // First item constant: X = 10, Width = 50
        var elem1 = report.Bands.Detail.Elements[0];
        elem1.X.Should().Be(10.0);
        elem1.Width.Should().Be(50.0);

        // Remaining width = 190 - 50 = 140mm divided by 2 = 70mm each
        var elem2 = report.Bands.Detail.Elements[1];
        elem2.X.Should().Be(60.0);
        elem2.Width.Should().Be(70.0);

        var elem3 = report.Bands.Detail.Elements[2];
        elem3.X.Should().Be(130.0);
        elem3.Width.Should().Be(70.0);
    }

    [Fact]
    public async Task DynamicTable_WithAlternatingRows_RendersToPdfSuccessfully()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A4);
                page.Margin(10, UnitType.Mm);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(1);
                    });

                    table.AlternatingRowBackground("#F3F4F6");

                    table.Header(h =>
                    {
                        h.BackgroundColor("#1E3A8A");
                        h.Cell().Color("#FFFFFF").Bold().Text("No.");
                        h.Cell().Color("#FFFFFF").Bold().Text("Product Description");
                        h.Cell().Color("#FFFFFF").Bold().AlignRight().Text("Price");
                    });

                    for (int i = 1; i <= 5; i++)
                    {
                        int index = i;
                        table.Row(r =>
                        {
                            r.Cell().Text(index.ToString());
                            r.Cell().Text($"Cloud Server Tier {index}");
                            r.Cell().AlignRight().Text($"{(index * 1500):N2}");
                        });
                    }

                    table.Footer(f =>
                    {
                        f.Cell().ColumnSpan(2).Text("Total Amount").Bold();
                        f.Cell().AlignRight().Text("22,500.00").Bold();
                    });
                });
            });
        });

        var pdfBytes = await doc.GeneratePdfAsync();
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);

        var report = doc.ToReportDefinition();
        var tableElem = report.Bands.Detail!.Elements.First(e => e.Type == ElementType.Table);
        tableElem.Table.Should().NotBeNull();
        tableElem.Table!.Rows.Count.Should().Be(5);
        tableElem.Table.AlternatingRowBackground.Should().Be("#F3F4F6");
    }

    [Fact]
    public async Task ThaiFeatures_BahtText_ThaiDate_PromptPay_RendersPdf()
    {
        var testDate = new DateTime(2026, 10, 13);
        double totalAmount = 125000.50;

        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A4);
                page.Margin(15, UnitType.Mm);

                page.Header().Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("ใบเสร็จรับเงิน วันที่: ");
                            t.ThaiDate(testDate);
                            t.Bold();
                        });
                    });

                    // PromptPay QR Code
                    r.ConstantItem(80, UnitType.Mm).QrPromptPay("0812345678", totalAmount, 40);
                });

                page.Content().Column(col =>
                {
                    col.Item().Text(t =>
                    {
                        t.Span("ยอดเงินรวมทั้งสิ้น: ");
                        t.BahtText(totalAmount);
                        t.Bold();
                        t.FontSize(14);
                    });
                });

                page.Footer().Row(r =>
                {
                    r.RelativeItem().Text("Bangplanix Reporting System");
                    r.ConstantItem(60, UnitType.Mm).AlignRight().Text(t =>
                    {
                        t.Span("หน้า ");
                        t.CurrentPageNumber();
                        t.Span(" จาก ");
                        t.TotalPages();
                    });
                });
            });
        });

        var report = doc.ToReportDefinition();

        // Check BahtText evaluated
        var textElem = report.Bands.Detail!.Elements[0];
        textElem.Text.Should().Contain("หนึ่งแสนสองหมื่นห้าพันบาทห้าสิบสตางค์");

        // Check ThaiDate evaluated (2026 + 543 = 2569)
        var headerTextElem = report.Bands.PageHeader!.Elements.First(e => e.Type == ElementType.Text);
        headerTextElem.Text.Should().Contain("2569");

        // Check PromptPay QR element generated
        var qrElem = report.Bands.PageHeader.Elements.First(e => e.Type == ElementType.QrCode);
        qrElem.Text.Should().StartWith("000201010212"); // PromptPay dynamic EMVCo header

        var pdfBytes = await doc.GeneratePdfAsync();
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task ReusableComponent_Compose_RendersSuccessfully()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A4);
                page.Header().Component(new TestInvoiceHeaderComponent());
                page.Content().Text("Invoice body content");
            });
        });

        var report = doc.ToReportDefinition();
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements.Count.Should().BeGreaterThanOrEqualTo(2);

        var pdfBytes = await doc.GeneratePdfAsync();
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void ExportToJsonAndToReportDefinition_ProducesValidOutput()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A4);
                page.Content().Text("JSON Export Test");
            });
        });

        var json = doc.ToJson();
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("pageSetup");
        json.Should().Contain("A4");
        json.Should().Contain("JSON Export Test");

        var report = doc.ToReportDefinition();
        report.Should().NotBeNull();
        report.PageSetup.PaperKind.Should().Be(PaperKind.A4);
    }

    [Fact]
    public async Task GeneratePdfStreamAsync_And_SyncGeneratePdf_WorkAccurately()
    {
        var doc = BangplanixDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PaperKind.A5);
                page.Content().Text("Stream Test");
            });
        });

        using var memoryStream = new MemoryStream();
        await doc.GeneratePdfStreamAsync(memoryStream);
        memoryStream.Length.Should().BeGreaterThan(500);

        var syncBytes = doc.GeneratePdf();
        syncBytes.Should().NotBeNull();
        syncBytes.Length.Should().BeGreaterThan(500);
    }
}
