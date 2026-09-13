using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using SkiaSharp;

namespace Bangplanix.Engine.Pagination;

public sealed class PaginationEngine
{
    public static void RenderReportPages(ReportDefinition report, SKDocument document, BandContext context)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        var pageSetup = report.PageSetup;
        var unit = pageSetup.Unit;
        var (pageWidthPt, pageHeightPt) = UnitConverter.GetPageDimensionsInPoints(pageSetup);

        var margins = pageSetup.Margins;
        var topMarginPt = UnitConverter.ToPoints(margins.Top, unit);
        var bottomMarginPt = UnitConverter.ToPoints(margins.Bottom, unit);
        var leftMarginPt = UnitConverter.ToPoints(margins.Left, unit);

        var pageHeaderHeightPt = report.Bands.PageHeader != null ? UnitConverter.ToPoints(report.Bands.PageHeader.Height, unit) : 0f;
        var pageFooterHeightPt = report.Bands.PageFooter != null ? UnitConverter.ToPoints(report.Bands.PageFooter.Height, unit) : 0f;

        var contentStartY = topMarginPt + pageHeaderHeightPt;
        var contentEndY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;

        var rows = context.MainDataRows;
        var detailBand = report.Bands.Detail;
        var detailHeightPt = detailBand != null ? UnitConverter.ToPoints(detailBand.Height, unit) : 0f;

        // Pass 1: Compute total pages (or start direct streaming)
        // For accurate @Globals.TotalPages, pre-calculate page count
        var totalPages = CalculateTotalPages(report, rows, contentStartY, contentEndY, detailHeightPt, unit);
        context.TotalPages = Math.Max(1, totalPages);

        int currentPage = 1;
        context.CurrentPageNumber = currentPage;

        SKCanvas canvas = document.BeginPage(pageWidthPt, pageHeightPt);
        var reportCanvas = new SkiaReportCanvas(canvas, unit);

        // Draw Page 1 Header
        if (report.Bands.PageHeader != null)
        {
            reportCanvas.RenderBand(report.Bands.PageHeader, topMarginPt, context);
        }

        float currentY = contentStartY;

        // Draw Report Header (Only on Page 1)
        if (report.Bands.ReportHeader != null)
        {
            var reportHeaderHeightPt = UnitConverter.ToPoints(report.Bands.ReportHeader.Height, unit);
            reportCanvas.RenderBand(report.Bands.ReportHeader, currentY, context);
            currentY += reportHeaderHeightPt;
        }

        if (rows.Count > 0 && detailBand != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                context.CurrentRow = rows[i];
                context.CurrentRowIndex = i;

                if (currentY + detailHeightPt > contentEndY)
                {
                    // Draw Footer for current page
                    if (report.Bands.PageFooter != null)
                    {
                        var footerY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;
                        reportCanvas.RenderBand(report.Bands.PageFooter, footerY, context);
                    }

                    document.EndPage();

                    // Start Next Page
                    currentPage++;
                    context.CurrentPageNumber = currentPage;
                    canvas = document.BeginPage(pageWidthPt, pageHeightPt);
                    reportCanvas = new SkiaReportCanvas(canvas, unit);

                    // Draw Page Header on new page
                    if (report.Bands.PageHeader != null)
                    {
                        reportCanvas.RenderBand(report.Bands.PageHeader, topMarginPt, context);
                    }

                    currentY = contentStartY;
                }

                reportCanvas.RenderBand(detailBand, currentY, context);
                currentY += detailHeightPt;
            }
        }
        else if (detailBand != null)
        {
            // Single detail pass without data rows
            reportCanvas.RenderBand(detailBand, currentY, context);
            currentY += detailHeightPt;
        }

        // Report Footer
        if (report.Bands.ReportFooter != null)
        {
            var reportFooterHeightPt = UnitConverter.ToPoints(report.Bands.ReportFooter.Height, unit);
            if (currentY + reportFooterHeightPt > contentEndY)
            {
                // Move Report Footer to new page
                if (report.Bands.PageFooter != null)
                {
                    var footerY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;
                    reportCanvas.RenderBand(report.Bands.PageFooter, footerY, context);
                }

                document.EndPage();

                currentPage++;
                context.CurrentPageNumber = currentPage;
                canvas = document.BeginPage(pageWidthPt, pageHeightPt);
                reportCanvas = new SkiaReportCanvas(canvas, unit);

                if (report.Bands.PageHeader != null)
                {
                    reportCanvas.RenderBand(report.Bands.PageHeader, topMarginPt, context);
                }

                currentY = contentStartY;
            }

            reportCanvas.RenderBand(report.Bands.ReportFooter, currentY, context);
        }

        // Draw Final Page Footer
        if (report.Bands.PageFooter != null)
        {
            var footerY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;
            reportCanvas.RenderBand(report.Bands.PageFooter, footerY, context);
        }

        document.EndPage();
    }

    private static int CalculateTotalPages(ReportDefinition report, IReadOnlyList<IDictionary<string, object?>> rows, float contentStartY, float contentEndY, float detailHeightPt, UnitType unit)
    {
        if (rows.Count == 0 || detailHeightPt <= 0)
        {
            return 1;
        }

        int pages = 1;
        float currentY = contentStartY;

        if (report.Bands.ReportHeader != null)
        {
            currentY += UnitConverter.ToPoints(report.Bands.ReportHeader.Height, unit);
        }

        for (int i = 0; i < rows.Count; i++)
        {
            if (currentY + detailHeightPt > contentEndY)
            {
                pages++;
                currentY = contentStartY;
            }
            currentY += detailHeightPt;
        }

        if (report.Bands.ReportFooter != null)
        {
            var rFooterHeight = UnitConverter.ToPoints(report.Bands.ReportFooter.Height, unit);
            if (currentY + rFooterHeight > contentEndY)
            {
                pages++;
            }
        }

        return pages;
    }
}
