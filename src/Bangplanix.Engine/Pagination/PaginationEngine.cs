using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Visuals;
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
        var rightMarginPt = UnitConverter.ToPoints(margins.Right, unit);

        var pageHeaderHeightPt = report.Bands.PageHeader != null ? UnitConverter.ToPoints(report.Bands.PageHeader.Height, unit) : 0f;
        var pageFooterHeightPt = report.Bands.PageFooter != null ? UnitConverter.ToPoints(report.Bands.PageFooter.Height, unit) : 0f;

        var contentStartY = topMarginPt + pageHeaderHeightPt;
        var contentEndY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;

        var rows = context.MainDataRows;
        var detailBand = report.Bands.Detail;
        var detailHeightPt = detailBand != null ? UnitConverter.ToPoints(detailBand.Height, unit) : 0f;

        var watermark = report.Watermark ?? report.PageSetup.Watermark;

        // Continuous Height Mode (POS Slips / Rolls)
        if (pageSetup.ContinuousHeight)
        {
            RenderContinuousReport(report, document, context, pageWidthPt, topMarginPt, bottomMarginPt, unit, watermark);
            return;
        }

        // Pass 1: Compute total pages
        var totalPages = CalculateTotalPages(report, rows, contentStartY, contentEndY, detailHeightPt, unit, context);
        context.TotalPages = Math.Max(1, totalPages);

        int currentPage = 1;
        context.CurrentPageNumber = currentPage;
        context.AvailableHeight = Math.Max(0, contentEndY - contentStartY);

        SKCanvas canvas = document.BeginPage(pageWidthPt, pageHeightPt);
        var reportCanvas = new SkiaReportCanvas(canvas, unit);

        // Background Watermark
        if (watermark != null && watermark.Layer == WatermarkLayer.Background)
        {
            WatermarkRenderer.RenderWatermark(canvas, watermark, pageWidthPt, pageHeightPt, context);
        }

        // Page 1 Header
        if (report.Bands.PageHeader != null)
        {
            reportCanvas.RenderBand(report.Bands.PageHeader, topMarginPt, context);
        }

        float currentY = contentStartY;

        // Report Header
        if (report.Bands.ReportHeader != null)
        {
            var rhHeightPt = report.Bands.ReportHeader.CanGrow
                ? CalculateDynamicBandHeight(report.Bands.ReportHeader, unit, context)
                : UnitConverter.ToPoints(report.Bands.ReportHeader.Height, unit);

            reportCanvas.RenderBand(report.Bands.ReportHeader, currentY, context);
            currentY += rhHeightPt;
        }

        if (rows.Count > 0 && detailBand != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                context.CurrentRow = rows[i];
                context.CurrentRowIndex = i;

                var effectiveDetailHeightPt = detailBand.CanGrow
                    ? CalculateDynamicBandHeight(detailBand, unit, context)
                    : detailHeightPt;

                // Ensure Space Check
                if (detailBand.EnsureSpace > 0)
                {
                    var ensureSpacePt = UnitConverter.ToPoints(detailBand.EnsureSpace, unit);
                    if ((contentEndY - currentY) < ensureSpacePt && currentY > contentStartY)
                    {
                        currentY = TriggerPageBreak(report, document, ref currentPage, ref canvas, ref reportCanvas,
                            pageWidthPt, pageHeightPt, topMarginPt, bottomMarginPt, pageFooterHeightPt, contentStartY,
                            margins, unit, watermark, context);
                    }
                }

                if (currentY + effectiveDetailHeightPt > contentEndY)
                {
                    currentY = TriggerPageBreak(report, document, ref currentPage, ref canvas, ref reportCanvas,
                        pageWidthPt, pageHeightPt, topMarginPt, bottomMarginPt, pageFooterHeightPt, contentStartY,
                        margins, unit, watermark, context);
                }

                context.AvailableHeight = Math.Max(0, contentEndY - currentY);
                reportCanvas.RenderBand(detailBand, currentY, context);
                currentY += effectiveDetailHeightPt;
            }
        }
        else if (detailBand != null)
        {
            var effectiveDetailHeightPt = detailBand.CanGrow
                ? CalculateDynamicBandHeight(detailBand, unit, context)
                : detailHeightPt;

            context.AvailableHeight = Math.Max(0, contentEndY - currentY);
            reportCanvas.RenderBand(detailBand, currentY, context);
            currentY += effectiveDetailHeightPt;
        }

        // Report Footer
        if (report.Bands.ReportFooter != null)
        {
            var rFooterHeightPt = report.Bands.ReportFooter.CanGrow
                ? CalculateDynamicBandHeight(report.Bands.ReportFooter, unit, context)
                : UnitConverter.ToPoints(report.Bands.ReportFooter.Height, unit);

            if (report.Bands.ReportFooter.EnsureSpace > 0)
            {
                var ensureSpacePt = UnitConverter.ToPoints(report.Bands.ReportFooter.EnsureSpace, unit);
                if ((contentEndY - currentY) < ensureSpacePt && currentY > contentStartY)
                {
                    currentY = TriggerPageBreak(report, document, ref currentPage, ref canvas, ref reportCanvas,
                        pageWidthPt, pageHeightPt, topMarginPt, bottomMarginPt, pageFooterHeightPt, contentStartY,
                        margins, unit, watermark, context);
                }
            }

            if (currentY + rFooterHeightPt > contentEndY)
            {
                currentY = TriggerPageBreak(report, document, ref currentPage, ref canvas, ref reportCanvas,
                    pageWidthPt, pageHeightPt, topMarginPt, bottomMarginPt, pageFooterHeightPt, contentStartY,
                    margins, unit, watermark, context);
            }

            context.AvailableHeight = Math.Max(0, contentEndY - currentY);
            reportCanvas.RenderBand(report.Bands.ReportFooter, currentY, context);
        }

        // Final Page Footer
        if (report.Bands.PageFooter != null)
        {
            var footerY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;
            reportCanvas.RenderBand(report.Bands.PageFooter, footerY, context);
        }

        // Foreground Watermark
        if (watermark != null && watermark.Layer == WatermarkLayer.Foreground)
        {
            WatermarkRenderer.RenderWatermark(canvas, watermark, pageWidthPt, pageHeightPt, context);
        }

        // Auto Page Number
        if (pageSetup.ShowPageNumbers)
        {
            RenderAutoPageNumber(reportCanvas, pageSetup, pageWidthPt, pageHeightPt, margins, unit, currentPage, context.TotalPages);
        }

        document.EndPage();
    }

    private static float TriggerPageBreak(
        ReportDefinition report,
        SKDocument document,
        ref int currentPage,
        ref SKCanvas canvas,
        ref SkiaReportCanvas reportCanvas,
        float pageWidthPt,
        float pageHeightPt,
        float topMarginPt,
        float bottomMarginPt,
        float pageFooterHeightPt,
        float contentStartY,
        MarginDefinition margins,
        UnitType unit,
        WatermarkDefinition? watermark,
        BandContext context)
    {
        // Draw Footer for current page
        if (report.Bands.PageFooter != null)
        {
            var footerY = pageHeightPt - bottomMarginPt - pageFooterHeightPt;
            reportCanvas.RenderBand(report.Bands.PageFooter, footerY, context);
        }

        // Foreground Watermark
        if (watermark != null && watermark.Layer == WatermarkLayer.Foreground)
        {
            WatermarkRenderer.RenderWatermark(canvas, watermark, pageWidthPt, pageHeightPt, context);
        }

        // Auto Page Number
        if (report.PageSetup.ShowPageNumbers)
        {
            RenderAutoPageNumber(reportCanvas, report.PageSetup, pageWidthPt, pageHeightPt, margins, unit, currentPage, context.TotalPages);
        }

        document.EndPage();

        // Start Next Page
        currentPage++;
        context.CurrentPageNumber = currentPage;
        canvas = document.BeginPage(pageWidthPt, pageHeightPt);
        reportCanvas = new SkiaReportCanvas(canvas, unit);

        // Background Watermark on new page
        if (watermark != null && watermark.Layer == WatermarkLayer.Background)
        {
            WatermarkRenderer.RenderWatermark(canvas, watermark, pageWidthPt, pageHeightPt, context);
        }

        // Draw Page Header on new page
        if (report.Bands.PageHeader != null)
        {
            reportCanvas.RenderBand(report.Bands.PageHeader, topMarginPt, context);
        }

        return contentStartY;
    }

    public static float CalculateDynamicBandHeight(BandDefinition band, UnitType unit, BandContext context)
    {
        ArgumentNullException.ThrowIfNull(band);
        var baseHeightPt = UnitConverter.ToPoints(band.Height, unit);
        if (!band.CanGrow && !band.Elements.Any(e => e.CanGrow || e.Type == ElementType.Table))
        {
            return baseHeightPt;
        }

        float maxElementBottomPt = baseHeightPt;

        foreach (var element in band.Elements)
        {
            if (!SkiaReportCanvas.IsPageDisplayMatch(element.ShowOnPages, context))
            {
                continue;
            }

            var elYPt = UnitConverter.ToPoints(element.Y, unit);
            var elWidthPt = UnitConverter.ToPoints(element.Width, unit);
            var elHeightPt = UnitConverter.ToPoints(element.Height, unit);

            if (element.Type == ElementType.Text && (element.CanGrow || band.CanGrow))
            {
                var textVal = context.ResolveExpressionOrValue(element.Text, element.Expression)?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(textVal))
                {
                    var style = element.Style ?? (element.StyleRef != null && context.Report.Styles.TryGetValue(element.StyleRef, out var refStyle) ? refStyle : null);
                    var measuredHeightPt = SkiaReportCanvas.MeasureTextHeight(textVal, elWidthPt, style);
                    var requiredHeight = Math.Max(elHeightPt, measuredHeightPt);
                    maxElementBottomPt = Math.Max(maxElementBottomPt, elYPt + requiredHeight);
                }
            }
            else if (element.Type == ElementType.Table && element.Table != null)
            {
                var colWidths = TableRenderer.CalculateColumnWidths(element.Table.Columns, elWidthPt, unit);
                float totalTableHeight = 0f;
                if (element.Table.Header != null)
                {
                    totalTableHeight += TableRenderer.MeasureRowHeight(element.Table.Header, colWidths, unit, context, context.Report);
                }
                foreach (var row in element.Table.Rows)
                {
                    totalTableHeight += TableRenderer.MeasureRowHeight(row, colWidths, unit, context, context.Report);
                }
                if (element.Table.Footer != null)
                {
                    totalTableHeight += TableRenderer.MeasureRowHeight(element.Table.Footer, colWidths, unit, context, context.Report);
                }
                maxElementBottomPt = Math.Max(maxElementBottomPt, elYPt + totalTableHeight);
            }
            else
            {
                maxElementBottomPt = Math.Max(maxElementBottomPt, elYPt + elHeightPt);
            }
        }

        return maxElementBottomPt;
    }

    private static int CalculateTotalPages(
        ReportDefinition report,
        IReadOnlyList<IDictionary<string, object?>> rows,
        float contentStartY,
        float contentEndY,
        float detailHeightPt,
        UnitType unit,
        BandContext context)
    {
        if (rows.Count == 0 && report.Bands.Detail == null)
        {
            return 1;
        }

        int pages = 1;
        float currentY = contentStartY;

        if (report.Bands.ReportHeader != null)
        {
            currentY += report.Bands.ReportHeader.CanGrow
                ? CalculateDynamicBandHeight(report.Bands.ReportHeader, unit, context)
                : UnitConverter.ToPoints(report.Bands.ReportHeader.Height, unit);
        }

        var detailBand = report.Bands.Detail;

        if (rows.Count > 0 && detailBand != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                context.CurrentRow = rows[i];
                context.CurrentRowIndex = i;

                var effectiveHeight = detailBand.CanGrow
                    ? CalculateDynamicBandHeight(detailBand, unit, context)
                    : detailHeightPt;

                if (detailBand.EnsureSpace > 0)
                {
                    var ensureSpacePt = UnitConverter.ToPoints(detailBand.EnsureSpace, unit);
                    if ((contentEndY - currentY) < ensureSpacePt && currentY > contentStartY)
                    {
                        pages++;
                        currentY = contentStartY;
                    }
                }

                if (currentY + effectiveHeight > contentEndY)
                {
                    pages++;
                    currentY = contentStartY;
                }
                currentY += effectiveHeight;
            }
        }
        else if (detailBand != null)
        {
            var effectiveHeight = detailBand.CanGrow
                ? CalculateDynamicBandHeight(detailBand, unit, context)
                : detailHeightPt;
            currentY += effectiveHeight;
        }

        if (report.Bands.ReportFooter != null)
        {
            var rFooterHeight = report.Bands.ReportFooter.CanGrow
                ? CalculateDynamicBandHeight(report.Bands.ReportFooter, unit, context)
                : UnitConverter.ToPoints(report.Bands.ReportFooter.Height, unit);

            if (report.Bands.ReportFooter.EnsureSpace > 0)
            {
                var ensureSpacePt = UnitConverter.ToPoints(report.Bands.ReportFooter.EnsureSpace, unit);
                if ((contentEndY - currentY) < ensureSpacePt && currentY > contentStartY)
                {
                    pages++;
                    currentY = contentStartY;
                }
            }

            if (currentY + rFooterHeight > contentEndY)
            {
                pages++;
            }
        }

        return pages;
    }

    private static void RenderContinuousReport(
        ReportDefinition report,
        SKDocument document,
        BandContext context,
        float pageWidthPt,
        float topMarginPt,
        float bottomMarginPt,
        UnitType unit,
        WatermarkDefinition? watermark)
    {
        var rows = context.MainDataRows;
        var detailBand = report.Bands.Detail;
        float totalHeightPt = topMarginPt + bottomMarginPt;

        if (report.Bands.ReportHeader != null)
        {
            totalHeightPt += CalculateDynamicBandHeight(report.Bands.ReportHeader, unit, context);
        }

        if (rows.Count > 0 && detailBand != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                context.CurrentRow = rows[i];
                context.CurrentRowIndex = i;
                totalHeightPt += CalculateDynamicBandHeight(detailBand, unit, context);
            }
        }
        else if (detailBand != null)
        {
            totalHeightPt += CalculateDynamicBandHeight(detailBand, unit, context);
        }

        if (report.Bands.ReportFooter != null)
        {
            totalHeightPt += CalculateDynamicBandHeight(report.Bands.ReportFooter, unit, context);
        }

        totalHeightPt = Math.Max(100f, totalHeightPt);
        context.TotalPages = 1;
        context.CurrentPageNumber = 1;

        SKCanvas canvas = document.BeginPage(pageWidthPt, totalHeightPt);
        var reportCanvas = new SkiaReportCanvas(canvas, unit);

        if (watermark != null && watermark.Layer == WatermarkLayer.Background)
        {
            WatermarkRenderer.RenderWatermark(canvas, watermark, pageWidthPt, totalHeightPt, context);
        }

        float currentY = topMarginPt;

        if (report.Bands.ReportHeader != null)
        {
            var h = CalculateDynamicBandHeight(report.Bands.ReportHeader, unit, context);
            reportCanvas.RenderBand(report.Bands.ReportHeader, currentY, context);
            currentY += h;
        }

        if (rows.Count > 0 && detailBand != null)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                context.CurrentRow = rows[i];
                context.CurrentRowIndex = i;
                var h = CalculateDynamicBandHeight(detailBand, unit, context);
                reportCanvas.RenderBand(detailBand, currentY, context);
                currentY += h;
            }
        }
        else if (detailBand != null)
        {
            var h = CalculateDynamicBandHeight(detailBand, unit, context);
            reportCanvas.RenderBand(detailBand, currentY, context);
            currentY += h;
        }

        if (report.Bands.ReportFooter != null)
        {
            reportCanvas.RenderBand(report.Bands.ReportFooter, currentY, context);
        }

        if (watermark != null && watermark.Layer == WatermarkLayer.Foreground)
        {
            WatermarkRenderer.RenderWatermark(canvas, watermark, pageWidthPt, totalHeightPt, context);
        }

        document.EndPage();
    }

    public static void RenderAutoPageNumber(
        SkiaReportCanvas reportCanvas,
        PageSetup pageSetup,
        float pageWidthPt,
        float pageHeightPt,
        MarginDefinition margins,
        UnitType unit,
        int currentPage,
        int totalPages)
    {
        var format = pageSetup.PageNumberFormat;
        if (string.IsNullOrWhiteSpace(format))
        {
            format = "{page} / {total}";
        }

        var pageStr = format.Replace("{page}", currentPage.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
                            .Replace("{total}", totalPages.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);

        var topMarginPt = UnitConverter.ToPoints(margins.Top, unit);
        var bottomMarginPt = UnitConverter.ToPoints(margins.Bottom, unit);
        var leftMarginPt = UnitConverter.ToPoints(margins.Left, unit);
        var rightMarginPt = UnitConverter.ToPoints(margins.Right, unit);

        var style = pageSetup.PageNumberStyle ?? new StyleDefinition
        {
            FontSize = 9.0,
            Color = "#666666",
            FontFamily = "Sarabun"
        };

        float xPt, yPt, wPt = 200f, hPt = 15f;

        switch (pageSetup.PageNumberPosition)
        {
            case PageNumberPosition.BottomLeft:
                xPt = leftMarginPt;
                yPt = pageHeightPt - bottomMarginPt + 2f;
                style.Align = HorizontalAlign.Left;
                break;
            case PageNumberPosition.BottomCenter:
                xPt = (pageWidthPt - wPt) / 2.0f;
                yPt = pageHeightPt - bottomMarginPt + 2f;
                style.Align = HorizontalAlign.Center;
                break;
            case PageNumberPosition.BottomRight:
            default:
                xPt = pageWidthPt - rightMarginPt - wPt;
                yPt = pageHeightPt - bottomMarginPt + 2f;
                style.Align = HorizontalAlign.Right;
                break;
            case PageNumberPosition.TopLeft:
                xPt = leftMarginPt;
                yPt = Math.Max(0f, topMarginPt - hPt - 2f);
                style.Align = HorizontalAlign.Left;
                break;
            case PageNumberPosition.TopCenter:
                xPt = (pageWidthPt - wPt) / 2.0f;
                yPt = Math.Max(0f, topMarginPt - hPt - 2f);
                style.Align = HorizontalAlign.Center;
                break;
            case PageNumberPosition.TopRight:
                xPt = pageWidthPt - rightMarginPt - wPt;
                yPt = Math.Max(0f, topMarginPt - hPt - 2f);
                style.Align = HorizontalAlign.Right;
                break;
        }

        reportCanvas.DrawDirectText(pageStr, xPt, yPt, wPt, hPt, style);
    }
}
