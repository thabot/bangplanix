using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Assembly;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PdfAssemblyAndTocIntegrityTests
{
    [Fact]
    public void TocGenerator_RenderTocPage_ShouldDrawEntriesAndLeaderDots()
    {
        using var bitmap = new SKBitmap(595, 842, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var toc = new TocDefinition
        {
            Title = "Table of Contents",
            ShowPageNumbers = true,
            FontSize = 12f
        };

        var entries = new List<TocEntry>
        {
            new("1. Executive Summary", 1, 1),
            new("2. Financial Performance", 3, 1),
            new("   2.1 Revenue by Region", 4, 2),
            new("   2.2 Operating Expenses", 8, 2),
            new("3. Tax & Compliance Audit", 12, 1),
            new("4. Appendix & Declarations", 15, 1)
        };

        TocGenerator.RenderTocPage(canvas, toc, entries, 595f, 842f);

        // Verify that canvas was drawn to
        var nonTransparentCount = 0;
        for (int y = 0; y < bitmap.Height; y += 10)
        {
            for (int x = 0; x < bitmap.Width; x += 10)
            {
                if (bitmap.GetPixel(x, y) != SKColors.White && bitmap.GetPixel(x, y).Alpha > 0)
                {
                    nonTransparentCount++;
                }
            }
        }

        Assert.True(nonTransparentCount > 20);
    }

    [Fact]
    public void PdfBookmarkEngine_ShouldBuildAndInjectHierarchicalBookmarks()
    {
        var bookmarks = new List<PdfBookmarkNode>
        {
            new()
            {
                Title = "Chapter 1: Overview",
                PageNumber = 1,
                Children = new List<PdfBookmarkNode>
                {
                    new() { Title = "1.1 Introduction", PageNumber = 1 },
                    new() { Title = "1.2 Scope", PageNumber = 2 }
                }
            },
            new()
            {
                Title = "Chapter 2: Deep Dive",
                PageNumber = 5
            }
        };

        var initialPdf = Encoding.UTF8.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");
        var resultPdf = PdfBookmarkEngine.InjectPdfBookmarks(initialPdf, bookmarks);

        Assert.NotNull(resultPdf);
        Assert.True(resultPdf.Length > initialPdf.Length);

        var pdfText = Encoding.UTF8.GetString(resultPdf);
        Assert.Contains("BANGPLANIX-PDF-OUTLINES-START", pdfText, StringComparison.Ordinal);
        Assert.Contains("/Title (Chapter 1: Overview)", pdfText, StringComparison.Ordinal);
        Assert.Contains("/Title (1.1 Introduction)", pdfText, StringComparison.Ordinal);
        Assert.Contains("/Title (Chapter 2: Deep Dive)", pdfText, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfHyperlinkEngine_ShouldInjectPageLinkAnnotations()
    {
        var links = new List<(int PageNumber, PdfLinkAnnotation Link)>
        {
            (1, new PdfLinkAnnotation
            {
                Rect = [50, 100, 200, 120],
                Uri = "https://bangplanix.io/docs"
            }),
            (2, new PdfLinkAnnotation
            {
                Rect = [50, 200, 200, 220],
                TargetPageNumber = 5
            })
        };

        var initialPdf = Encoding.UTF8.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Page >>\nendobj\n%%EOF");
        var resultPdf = PdfHyperlinkEngine.InjectLinkAnnotations(initialPdf, links);

        Assert.NotNull(resultPdf);
        Assert.True(resultPdf.Length > initialPdf.Length);

        var pdfText = Encoding.UTF8.GetString(resultPdf);
        Assert.Contains("BANGPLANIX-PDF-LINKS-START", pdfText, StringComparison.Ordinal);
        Assert.Contains("/Subtype /Link", pdfText, StringComparison.Ordinal);
        Assert.Contains("/URI (https://bangplanix.io/docs)", pdfText, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfAttachmentEngine_ShouldEmbedArbitraryFilesWithMimeTypes()
    {
        var xmlBytes = Encoding.UTF8.GetBytes("<Invoice><Id>INV-2026-001</Id></Invoice>");
        var csvBytes = Encoding.UTF8.GetBytes("Id,Amount\n1,500.00");

        var attachments = new List<PdfAttachmentDefinition>
        {
            new() { FileName = "invoice.xml", Data = xmlBytes, MediaType = "application/xml", Description = "Embedded e-Invoice XML" },
            new() { FileName = "data.csv", Data = csvBytes, MediaType = "text/csv", Description = "Supporting Raw Data" }
        };

        var initialPdf = Encoding.UTF8.GetBytes("%PDF-1.7\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");
        var resultPdf = PdfAttachmentEngine.InjectEmbeddedAttachments(initialPdf, attachments);

        Assert.NotNull(resultPdf);
        Assert.True(resultPdf.Length > initialPdf.Length);

        var pdfText = Encoding.UTF8.GetString(resultPdf);
        Assert.Contains("BANGPLANIX-PDF-EMBEDDED-FILES-START", pdfText, StringComparison.Ordinal);
        Assert.Contains("/F (invoice.xml)", pdfText, StringComparison.Ordinal);
        Assert.Contains("/F (data.csv)", pdfText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PdfDossierMerger_ShouldAssembleMultiDocumentDossier()
    {
        var dossier = new DossierDefinition
        {
            Title = "Bangplanix Comprehensive Annual Dossier 2026",
            Author = "Enterprise Reporting Team",
            PageSetup = new PageSetup { Width = 595, Height = 842 },
            TableOfContents = new TocDefinition { Title = "Dossier Index", ShowPageNumbers = true },
            Sections = new List<DossierSectionDefinition>
            {
                new()
                {
                    Title = "Section 1: Executive Summary",
                    Report = new ReportDefinition
                    {
                        Metadata = new ReportMetadata { Title = "Executive Summary" },
                        PageSetup = new PageSetup { Width = 595, Height = 842 },
                        Bands = new BandsDefinition
                        {
                            ReportHeader = new BandDefinition
                            {
                                Height = 50,
                                Elements = new List<ElementDefinition>
                                {
                                    new() { Type = ElementType.Text, Text = "Executive Summary Content" }
                                }
                            }
                        }
                    }
                }
            }
        };

        var mergedPdf = await PdfDossierMerger.MergeDossierToPdfAsync(dossier);

        Assert.NotNull(mergedPdf);
        Assert.True(mergedPdf.Length > 0);
        var pdfHeader = Encoding.UTF8.GetString(mergedPdf.Take(8).ToArray());
        Assert.StartsWith("%PDF-", pdfHeader, StringComparison.Ordinal);
    }
}
