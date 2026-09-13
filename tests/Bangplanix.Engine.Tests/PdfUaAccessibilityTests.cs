using System;
using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Accessibility;
using FluentAssertions;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Engine.Tests;

public class PdfUaAccessibilityTests
{
    [Fact]
    public void BuildStructureTree_StandardInvoice_ShouldBuildProperSemanticHierarchy()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Commercial Tax Invoice", Author = "Bangplanix" },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 60,
                    Elements =
                    [
                        new ElementDefinition { Type = ElementType.Text, Text = "ใบกำกับภาษี / ใบเสร็จรับเงิน" }
                    ]
                },
                PageHeader = new BandDefinition
                {
                    Height = 40,
                    Elements =
                    [
                        new ElementDefinition { Type = ElementType.Text, Text = "ข้อมูลลูกค้าและที่อยู่" }
                    ]
                },
                Detail = new BandDefinition
                {
                    Height = 30,
                    Elements =
                    [
                        new ElementDefinition { Type = ElementType.Text, Text = "รายการสินค้า #1" },
                        new ElementDefinition { Type = ElementType.Barcode, Text = "8851234567890" },
                        new ElementDefinition { Type = ElementType.Chart, Chart = new ChartDefinition { Title = "ยอดขายเปรียบเทียบ" } }
                    ]
                }
            }
        };

        var options = new AccessibilityOptions
        {
            EnablePdfUa = true,
            PrimaryLanguage = "th-TH",
            DisplayDocTitle = true
        };

        var root = PdfUaTagEngine.BuildStructureTreeFromReport(report, options);

        root.TagType.Should().Be(PdfTagType.Document);
        root.Language.Should().Be("th-TH");
        root.Children.Should().HaveCount(3); // ReportHeader (H1), PageHeader (H2), Detail (Table)

        // H1 Heading
        var h1Section = root.Children[0];
        h1Section.Children[0].TagType.Should().Be(PdfTagType.H1);
        h1Section.Children[0].Title.Should().Be("ใบกำกับภาษี / ใบเสร็จรับเงิน");

        // H2 Heading
        var h2Section = root.Children[1];
        h2Section.Children[0].TagType.Should().Be(PdfTagType.H2);

        // Detail Table & Figures with Alt Text
        var tableSec = root.Children[2];
        tableSec.Children[0].TagType.Should().Be(PdfTagType.Table);
        var row = tableSec.Children[0].Children[0];
        row.TagType.Should().Be(PdfTagType.TableRow);
        row.Children.Should().HaveCount(3); // TD Text, TD Barcode, TD Chart

        // Verify Figures have mandatory Alt text for screen readers
        var barcodeFigure = row.Children[1].Children[0];
        barcodeFigure.TagType.Should().Be(PdfTagType.Figure);
        barcodeFigure.AltText.Should().NotBeNullOrWhiteSpace();
        barcodeFigure.AltText.Should().Contain("บาร์โค้ดข้อมูล");

        var chartFigure = row.Children[2].Children[0];
        chartFigure.TagType.Should().Be(PdfTagType.Figure);
        chartFigure.AltText.Should().NotBeNullOrWhiteSpace();
        chartFigure.AltText.Should().Contain("แผนภูมิแสดง: ยอดขายเปรียบเทียบ");
    }

    [Fact]
    public void ApplyPdfUa_ShouldInjectPdfUa1IdentificationAndCatalogTags()
    {
        var dummyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << >> endobj\n%%EOF");
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "รายงานงบดุลประจำปี" },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 40,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "งบแสดงฐานะการเงิน" }]
                }
            }
        };

        var options = new AccessibilityOptions
        {
            EnablePdfUa = true,
            PrimaryLanguage = "th-TH",
            DisplayDocTitle = true
        };

        var resultBytes = PdfUaTagEngine.ApplyPdfUaUniversalAccessibility(dummyPdf, report, options);
        var resultText = Encoding.UTF8.GetString(resultBytes);

        resultText.Should().Contain("% BANGPLANIX-PDF-UA-TAGS-START");
        resultText.Should().Contain("/Lang (th-TH)");
        resultText.Should().Contain("/MarkInfo << /Marked true >>");
        resultText.Should().Contain("/ViewerPreferences << /DisplayDocTitle true >>");
        resultText.Should().Contain("/StructTreeRoot <<");
        resultText.Should().Contain("<pdfuaid:part>1</pdfuaid:part>");
    }
}
