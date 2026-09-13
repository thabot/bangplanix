using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Assembly;
using FluentAssertions;
using SkiaSharp;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Engine.Tests;

public class DossierAndTocTests
{
    private static SKBitmap CreateBitmap(int width = 595, int height = 842) =>
        new(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

    [Fact]
    public void TocGenerator_RenderTocPage_ShouldDrawEntriesWithLeaderDotsAndPageNumbers()
    {
        using var bitmap = CreateBitmap();
        using var canvas = new SKCanvas(bitmap);

        var toc = new TocDefinition
        {
            Title = "สารบัญเอกสารรายงานประจำปี (Annual Dossier TOC)",
            ShowPageNumbers = true,
            FontSize = 11f
        };

        var entries = new List<TocEntry>
        {
            new("1. บทสรุปสำหรับผู้บริหาร (Executive Summary)", 2, 1),
            new("2. รายงานผลประกอบการทางการเงิน (Financial Performance)", 5, 1),
            new("   2.1 งบกำไรขาดทุน (P&L Statement)", 6, 2),
            new("   2.2 งบกระแสเงินสด (Cashflow Statement)", 8, 2),
            new("3. แผนการดำเนินงานและเป้าหมาย Q3-Q4 (Roadmap)", 12, 1),
            new("4. ภาคผนวกและรายงานการตรวจสอบ (Appendix & Audit)", 16, 1)
        };

        TocGenerator.RenderTocPage(canvas, toc, entries, 595f, 842f);

        // Verify non-white pixels drawn
        var nonWhitePixels = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White) nonWhitePixels++;
            }
        }

        nonWhitePixels.Should().BeGreaterThan(50);
    }

    [Fact]
    public void PdfBookmarkEngine_BuildTreeAndInject_ShouldBuildValidHierarchicalOutlines()
    {
        var sections = new List<DossierSectionDefinition>
        {
            new()
            {
                Title = "Section 1: Summary",
                TocTitle = "Executive Summary",
                DataRows = new List<IDictionary<string, object?>> { new Dictionary<string, object?>() }
            },
            new()
            {
                Title = "Section 2: Sales Invoices",
                TocTitle = "Sales Invoices",
                DataRows = new List<IDictionary<string, object?>>
                {
                    new Dictionary<string, object?>(),
                    new Dictionary<string, object?>(),
                    new Dictionary<string, object?>()
                },
                Bookmarks =
                [
                    new PdfBookmarkNode { Title = "Invoice #INV-2026-001", PageNumber = 1, Level = 2 },
                    new PdfBookmarkNode { Title = "Invoice #INV-2026-002", PageNumber = 2, Level = 2 }
                ]
            }
        };

        var tree = PdfBookmarkEngine.BuildOutlineTreeFromSections(sections, initialPageOffset: 3);

        tree.Should().HaveCount(2);
        tree[0].Title.Should().Be("Executive Summary");
        tree[0].PageNumber.Should().Be(3);

        tree[1].Title.Should().Be("Sales Invoices");
        tree[1].PageNumber.Should().Be(4);
        tree[1].Children.Should().HaveCount(2);
        tree[1].Children[0].Title.Should().Be("Invoice #INV-2026-001");
        tree[1].Children[0].PageNumber.Should().Be(4); // 4 + (1 - 1)
        tree[1].Children[1].Title.Should().Be("Invoice #INV-2026-002");
        tree[1].Children[1].PageNumber.Should().Be(5); // 4 + (2 - 1)

        // Test Bookmark Outline injection into dummy PDF stream
        var dummyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");
        var resultPdf = PdfBookmarkEngine.InjectPdfBookmarks(dummyPdf, tree);

        var resultText = Encoding.UTF8.GetString(resultPdf);
        resultText.Should().Contain("% BANGPLANIX-PDF-OUTLINES-START");
        resultText.Should().Contain("/Type /Outlines");
        resultText.Should().Contain("Executive Summary");
        resultText.Should().Contain("Invoice #INV-2026-001");
    }

    [Fact]
    public async Task PdfDossierMerger_MergeDossierToPdfAsync_ShouldGenerateUnifiedMultiSectionPdf()
    {
        var dossier = new DossierDefinition
        {
            Title = "รายงานสรุปประจำปี 2026 (Bangplanix Annual Dossier)",
            Author = "Bangplanix Enterprise Suite",
            CoverPage = new CoverPageDefinition
            {
                Title = "รายงานผลการดำเนินงานประจำปี 2569",
                Subtitle = "Bangplanix Enterprise Performance Dossier",
                Organization = "Bangplanix Global Corp",
                PreparedFor = "คณะกรรมการบริหาร (Board of Directors)",
                PreparedBy = "ฝ่ายบัญชีและการเงินกลาง",
                Date = "12 กันยายน 2569",
                BackgroundColor = "#0f172a",
                TextColor = "#ffffff"
            },
            TableOfContents = new TocDefinition
            {
                Title = "สารบัญเอกสาร (Table of Contents)",
                ShowPageNumbers = true
            },
            Sections =
            [
                new DossierSectionDefinition
                {
                    Title = "ส่วนที่ 1: รายงานยอดขายแยกตามภูมิภาค",
                    TocTitle = "1. ยอดขายแยกตามภูมิภาค",
                    Report = new ReportDefinition
                    {
                        PageSetup = new PageSetup { Width = 595.28, Height = 841.89, Unit = UnitType.Pt },
                        Bands = new BandsDefinition
                        {
                            PageHeader = new BandDefinition
                            {
                                Height = 40,
                                Elements = [new ElementDefinition { Type = ElementType.Text, Text = "รายงานยอดขายภูมิภาค", Width = 400, Height = 25 }]
                            },
                            Detail = new BandDefinition
                            {
                                Height = 30,
                                Elements = [new ElementDefinition { Type = ElementType.Text, Text = "ข้อมูลยอดขายภาคกลางและภาคใต้", Width = 400, Height = 20 }]
                            }
                        }
                    }
                },
                new DossierSectionDefinition
                {
                    Title = "ส่วนที่ 2: รายงานสินค้าคงคลังและซัพพลายเชน",
                    TocTitle = "2. รายงานสินค้าคงคลัง",
                    Report = new ReportDefinition
                    {
                        PageSetup = new PageSetup { Width = 595.28, Height = 841.89, Unit = UnitType.Pt },
                        Bands = new BandsDefinition
                        {
                            PageHeader = new BandDefinition
                            {
                                Height = 40,
                                Elements = [new ElementDefinition { Type = ElementType.Text, Text = "รายงานสต็อกและคลังสินค้า", Width = 400, Height = 25 }]
                            },
                            Detail = new BandDefinition
                            {
                                Height = 30,
                                Elements = [new ElementDefinition { Type = ElementType.Text, Text = "อัตราการหมุนเวียนสินค้า 98.5%", Width = 400, Height = 20 }]
                            }
                        }
                    }
                }
            ]
        };

        var pdfBytes = await PdfDossierMerger.MergeDossierToPdfAsync(dossier);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        var pdfHeader = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        pdfHeader.Should().Be("%PDF-");

        var pdfContent = Encoding.UTF8.GetString(pdfBytes);
        pdfContent.Should().Contain("% BANGPLANIX-PDF-OUTLINES-START");
        pdfContent.Should().Contain("ยอดขายแยกตามภูมิภาค");
    }
}
