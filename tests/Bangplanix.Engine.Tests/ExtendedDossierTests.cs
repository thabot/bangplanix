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

public class ExtendedDossierTests
{
    [Fact]
    public void PdfHyperlinkEngine_InternalAndExternalLinks_ShouldInjectValidAnnotationDictionaries()
    {
        var dummyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << >> endobj\n%%EOF");
        var links = new List<(int PageNumber, PdfLinkAnnotation Link)>
        {
            (1, new PdfLinkAnnotation { Rect = [50, 100, 300, 120], TargetPageNumber = 5 }),
            (1, new PdfLinkAnnotation { Rect = [50, 140, 200, 160], Uri = "https://bangplanix.io/reports" })
        };

        var outputBytes = PdfHyperlinkEngine.InjectLinkAnnotations(dummyPdf, links);
        var outputText = Encoding.UTF8.GetString(outputBytes);

        outputText.Should().Contain("% BANGPLANIX-PDF-LINKS-START");
        outputText.Should().Contain("/Subtype /Link");
        outputText.Should().Contain("/Dest [4 /XYZ null null null]"); // 0-indexed destination for page 5
        outputText.Should().Contain("/URI (https://bangplanix.io/reports)");
    }

    [Fact]
    public void PdfAttachmentEngine_InjectAttachments_ShouldInjectEmbeddedFilesDictionary()
    {
        var dummyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj << >> endobj\n%%EOF");
        var attachments = new List<PdfAttachmentDefinition>
        {
            new()
            {
                FileName = "raw_financial_data.xlsx",
                MediaType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Data = [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00] // Zip header
            },
            new()
            {
                FileName = "etax_invoice.xml",
                MediaType = "application/xml",
                Data = Encoding.UTF8.GetBytes("<TaxInvoice><Id>INV-2026-001</Id></TaxInvoice>")
            }
        };

        var outputBytes = PdfAttachmentEngine.InjectEmbeddedAttachments(dummyPdf, attachments);
        var outputText = Encoding.UTF8.GetString(outputBytes);

        outputText.Should().Contain("% BANGPLANIX-PDF-EMBEDDED-FILES-START");
        outputText.Should().Contain("/EmbeddedFiles");
        outputText.Should().Contain("(raw_financial_data.xlsx)");
        outputText.Should().Contain("(etax_invoice.xml)");
        outputText.Should().Contain("/Type /EmbeddedFile");
    }

    [Fact]
    public async Task PdfDossierMerger_WithSectionDividersAndAttachments_ShouldProduceCompleteDossier()
    {
        var dossier = new DossierDefinition
        {
            Title = "สมุดรายงานประจำไตรมาส (Quarterly Performance Binder)",
            Author = "Bangplanix Dossier Engine",
            CoverPage = new CoverPageDefinition
            {
                Title = "รายงานสรุปประจำไตรมาส 3/2569",
                Subtitle = "Bangplanix Executive Dossier & Data Binder",
                Organization = "Bangplanix Holdings",
                PreparedFor = "Chief Financial Officer (CFO)",
                BackgroundColor = "#1e293b"
            },
            TableOfContents = new TocDefinition
            {
                Title = "สารบัญชุดรายงาน (Table of Contents)"
            },
            Attachments =
            [
                new PdfAttachmentDefinition
                {
                    FileName = "audit_log.json",
                    Data = Encoding.UTF8.GetBytes("{\"audit\": true, \"year\": 2026}")
                }
            ],
            Sections =
            [
                new DossierSectionDefinition
                {
                    Title = "บทที่ 1: รายงานการเงิน",
                    TocTitle = "1. ผลการเงินรายไตรมาส",
                    Divider = new SectionDividerDefinition
                    {
                        ChapterNumber = "01",
                        Title = "รายงานการเงิน (Financial Results)",
                        Description = "งบแสดงฐานะการเงินและผลการดำเนินงาน",
                        BackgroundColor = "#0f172a",
                        AccentColor = "#3b82f6"
                    },
                    Report = new ReportDefinition
                    {
                        PageSetup = new PageSetup { Width = 595.28, Height = 841.89, Unit = UnitType.Pt },
                        Bands = new BandsDefinition
                        {
                            Detail = new BandDefinition
                            {
                                Height = 50,
                                Elements = [new ElementDefinition { Type = ElementType.Text, Text = "กำไรสุทธิเพิ่มขึ้น 24.5%", Width = 400, Height = 30 }]
                            }
                        }
                    }
                }
            ]
        };

        var pdfBytes = await PdfDossierMerger.MergeDossierToPdfAsync(dossier);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);

        var pdfText = Encoding.UTF8.GetString(pdfBytes);
        pdfText.Should().Contain("% BANGPLANIX-PDF-OUTLINES-START");
        pdfText.Should().Contain("% BANGPLANIX-PDF-LINKS-START");
        pdfText.Should().Contain("% BANGPLANIX-PDF-EMBEDDED-FILES-START");
        pdfText.Should().Contain("audit_log.json");
    }
}
