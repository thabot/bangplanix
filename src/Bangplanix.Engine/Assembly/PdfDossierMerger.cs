using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Fonts;
using Bangplanix.Engine.Pagination;
using Bangplanix.Engine.Visuals.Charts;
using SkiaSharp;

namespace Bangplanix.Engine.Assembly;

public static class PdfDossierMerger
{
    public static async Task<byte[]> MergeDossierToPdfAsync(
        DossierDefinition dossier,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dossier);

        using var memoryStream = new MemoryStream();
        await MergeDossierToStreamAsync(dossier, memoryStream, cancellationToken).ConfigureAwait(false);
        return memoryStream.ToArray();
    }

    public static Task MergeDossierToStreamAsync(
        DossierDefinition dossier,
        Stream outputStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dossier);
        ArgumentNullException.ThrowIfNull(outputStream);

        var pageW = (float)dossier.PageSetup.Width;
        var pageH = (float)dossier.PageSetup.Height;

        var pdfMetadata = new SKDocumentPdfMetadata
        {
            Title = dossier.Title,
            Author = dossier.Author,
            Subject = "Bangplanix Multi-Report Dossier",
            Creator = "Bangplanix Dossier Engine (.NET 10)",
            Producer = "SkiaSharp / HarfBuzz Vector Assembly",
            RasterDpi = 300.0f
        };

        var hasToc = dossier.TableOfContents != null;
        var links = new List<(int PageNumber, PdfLinkAnnotation Link)>();

        using var tempMs = new MemoryStream();
        using (var document = SKDocument.CreatePdf(tempMs, pdfMetadata))
        {
            var currentPage = 1;
            var tocEntries = new List<TocEntry>();

            // 1. Render Cover Page if configured
            if (dossier.CoverPage != null)
            {
                using var coverCanvas = document.BeginPage(pageW, pageH);
                RenderCoverPage(coverCanvas, dossier.CoverPage, pageW, pageH);
                document.EndPage();
                currentPage++;
            }

            // 2. Pre-calculate Section Page Numbers for TOC
            var tocPageNumber = currentPage;
            if (hasToc)
            {
                currentPage++; // Reserve 1 page for TOC
            }

            var sectionStartPages = new List<(DossierSectionDefinition Section, int StartPage)>();
            var runningSectionPage = currentPage;

            foreach (var section in dossier.Sections)
            {
                // If section has divider page, account for it
                if (section.Divider != null)
                {
                    runningSectionPage++;
                }

                sectionStartPages.Add((section, runningSectionPage));
                if (section.IncludeInToc)
                {
                    var title = string.IsNullOrWhiteSpace(section.TocTitle) ? section.Title : section.TocTitle;
                    tocEntries.Add(new TocEntry(title, runningSectionPage, 1));
                }

                var estimatedPages = Math.Max(1, section.DataRows.Count > 0 ? (int)Math.Ceiling(section.DataRows.Count / 25.0) : 1);
                runningSectionPage += estimatedPages;
            }

            // 3. Render TOC Page if configured & generate clickable link annotations
            if (hasToc && dossier.TableOfContents != null)
            {
                using var tocCanvas = document.BeginPage(pageW, pageH);
                TocGenerator.RenderTocPage(tocCanvas, dossier.TableOfContents, tocEntries, pageW, pageH);
                document.EndPage();

                // Add interactive link annotations for TOC items
                var tocRowH = 24f;
                var tocY = 112f;
                foreach (var entry in tocEntries)
                {
                    links.Add((tocPageNumber, new PdfLinkAnnotation
                    {
                        Rect = [54, tocY - 14, pageW - 54, tocY + 8],
                        TargetPageNumber = entry.PageNumber
                    }));
                    tocY += tocRowH;
                }
            }

            // 4. Render Each Section
            int totalDossierPages = runningSectionPage - 1;

            foreach (var (section, startPage) in sectionStartPages)
            {
                // Render Divider Page if present
                if (section.Divider != null)
                {
                    using var divCanvas = document.BeginPage(pageW, pageH);
                    RenderSectionDividerPage(divCanvas, section.Divider, pageW, pageH);
                    document.EndPage();
                }

                var report = section.Report ?? CreateDefaultSectionReport(section);

                var context = new BandContext
                {
                    Report = report,
                    Parameters = new Dictionary<string, object?>(section.Parameters, StringComparer.OrdinalIgnoreCase),
                    MainDataRows = section.DataRows,
                    CurrentPageNumber = startPage,
                    TotalPages = totalDossierPages
                };

                PaginationEngine.RenderReportPages(report, document, context);
            }

            document.Close();
        }

        // 5. Inject PDF Bookmark Outlines
        var outlineTree = PdfBookmarkEngine.BuildOutlineTreeFromSections(dossier.Sections, hasToc ? 3 : (dossier.CoverPage != null ? 2 : 1));
        if (dossier.GlobalBookmarks.Count > 0)
        {
            outlineTree.InsertRange(0, dossier.GlobalBookmarks);
        }

        var pdfWithBookmarks = PdfBookmarkEngine.InjectPdfBookmarks(tempMs.ToArray(), outlineTree);

        // 6. Inject Interactive Links
        var pdfWithLinks = PdfHyperlinkEngine.InjectLinkAnnotations(pdfWithBookmarks, links);

        // 7. Inject Embedded File Attachments
        var finalBytes = PdfAttachmentEngine.InjectEmbeddedAttachments(pdfWithLinks, dossier.Attachments);

        outputStream.Write(finalBytes, 0, finalBytes.Length);

        return Task.CompletedTask;
    }

    private static void RenderCoverPage(SKCanvas canvas, CoverPageDefinition cover, float pageW, float pageH)
    {
        var bgColor = ChartColorPalette.ParseColor(cover.BackgroundColor, new SKColor(30, 58, 138));
        var textColor = ChartColorPalette.ParseColor(cover.TextColor, SKColors.White);

        canvas.Clear(bgColor);

        var typeface = FontManager.Instance.GetTypeface("Sarabun");

        // Top Brand / Organization
        var curY = 120f;
        if (!string.IsNullOrWhiteSpace(cover.Organization))
        {
            using var orgPaint = new SKPaint
            {
                Color = textColor.WithAlpha(200),
                TextSize = 14f,
                FakeBoldText = true,
                IsAntialias = true,
                Typeface = typeface
            };
            canvas.DrawText(cover.Organization.ToUpperInvariant(), 60f, curY, orgPaint);
            curY += 20f;
        }

        // Decorative Accent Line
        using var linePaint = new SKPaint
        {
            Color = new SKColor(245, 158, 11), // Gold accent
            StrokeWidth = 3.5f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        canvas.DrawLine(60f, curY, 140f, curY, linePaint);
        curY += 50f;

        // Title
        using var titlePaint = new SKPaint
        {
            Color = textColor,
            TextSize = 28f,
            FakeBoldText = true,
            IsAntialias = true,
            Typeface = typeface
        };
        canvas.DrawText(cover.Title, 60f, curY, titlePaint);
        curY += 32f;

        // Subtitle
        if (!string.IsNullOrWhiteSpace(cover.Subtitle))
        {
            using var subPaint = new SKPaint
            {
                Color = textColor.WithAlpha(210),
                TextSize = 16f,
                IsAntialias = true,
                Typeface = typeface
            };
            canvas.DrawText(cover.Subtitle, 60f, curY, subPaint);
        }

        // Bottom Metadata Card (Prepared for / Prepared by / Date)
        var bottomY = pageH - 140f;
        using var metaPaint = new SKPaint
        {
            Color = textColor.WithAlpha(180),
            TextSize = 10f,
            IsAntialias = true,
            Typeface = typeface
        };
        using var metaValPaint = new SKPaint
        {
            Color = textColor,
            TextSize = 11f,
            FakeBoldText = true,
            IsAntialias = true,
            Typeface = typeface
        };

        if (!string.IsNullOrWhiteSpace(cover.PreparedFor))
        {
            canvas.DrawText("จัดทำสำหรับ (Prepared For):", 60f, bottomY, metaPaint);
            canvas.DrawText(cover.PreparedFor, 60f, bottomY + 16f, metaValPaint);
        }

        if (!string.IsNullOrWhiteSpace(cover.PreparedBy))
        {
            canvas.DrawText("จัดทำโดย (Prepared By):", pageW / 2f, bottomY, metaPaint);
            canvas.DrawText(cover.PreparedBy, pageW / 2f, bottomY + 16f, metaValPaint);
        }

        var dateText = cover.Date ?? DateTime.Now.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture);
        canvas.DrawText($"วันที่: {dateText}", 60f, pageH - 50f, metaPaint);
    }

    private static void RenderSectionDividerPage(SKCanvas canvas, SectionDividerDefinition divider, float pageW, float pageH)
    {
        var bgColor = ChartColorPalette.ParseColor(divider.BackgroundColor, new SKColor(15, 23, 42));
        var accentColor = ChartColorPalette.ParseColor(divider.AccentColor, new SKColor(59, 130, 246));

        canvas.Clear(bgColor);

        var typeface = FontManager.Instance.GetTypeface("Sarabun");

        // Huge Chapter Number (e.g. 01, 02)
        using var numPaint = new SKPaint
        {
            Color = accentColor.WithAlpha(120),
            TextSize = 72f,
            FakeBoldText = true,
            IsAntialias = true,
            Typeface = typeface
        };
        var chNum = divider.ChapterNumber ?? "01";
        canvas.DrawText(chNum, 60f, pageH / 2f - 40f, numPaint);

        // Accent Horizontal Line
        using var linePaint = new SKPaint { Color = accentColor, StrokeWidth = 4f, Style = SKPaintStyle.Stroke, IsAntialias = true };
        canvas.DrawLine(60f, pageH / 2f - 20f, 200f, pageH / 2f - 20f, linePaint);

        // Section Title
        using var titlePaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24f,
            FakeBoldText = true,
            IsAntialias = true,
            Typeface = typeface
        };
        canvas.DrawText(divider.Title, 60f, pageH / 2f + 25f, titlePaint);

        // Section Description
        if (!string.IsNullOrWhiteSpace(divider.Description))
        {
            using var descPaint = new SKPaint
            {
                Color = new SKColor(148, 163, 184),
                TextSize = 12f,
                IsAntialias = true,
                Typeface = typeface
            };
            canvas.DrawText(divider.Description, 60f, pageH / 2f + 55f, descPaint);
        }
    }

    private static ReportDefinition CreateDefaultSectionReport(DossierSectionDefinition section)
    {
        return new ReportDefinition
        {
            PageSetup = new PageSetup { Width = 595.28, Height = 841.89, Unit = UnitType.Pt },
            Bands = new BandsDefinition
            {
                PageHeader = new BandDefinition
                {
                    Height = 40,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = section.Title,
                            X = 20,
                            Y = 10,
                            Width = 500,
                            Height = 25,
                            Style = new StyleDefinition { FontSize = 14, FontWeight = "Bold" }
                        }
                    ]
                },
                Detail = new BandDefinition
                {
                    Height = 30,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = $"Section Content for {section.Title}",
                            X = 20,
                            Y = 5,
                            Width = 500,
                            Height = 20,
                            Style = new StyleDefinition { FontSize = 10 }
                        }
                    ]
                }
            }
        };
    }
}
