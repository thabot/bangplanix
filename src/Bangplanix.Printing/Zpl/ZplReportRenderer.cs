using Bangplanix.Core.Models;

namespace Bangplanix.Printing.Zpl;

public static class ZplReportRenderer
{
    public static string RenderToZpl(ReportDefinition report, IReadOnlyList<IDictionary<string, object?>>? dataRows = null, ZplDpi dpi = ZplDpi.Dpi203)
    {
        ArgumentNullException.ThrowIfNull(report);
        var gen = new ZplGenerator(dpi);

        // Header Elements
        if (report.Bands.PageHeader != null)
        {
            foreach (var elem in report.Bands.PageHeader.Elements)
            {
                RenderElement(gen, elem, null, 0);
            }
        }

        int currentY = report.Bands.PageHeader != null ? gen.PointsToDots(report.Bands.PageHeader.Height) : 0;

        // Detail Elements / Rows
        if (dataRows != null && dataRows.Count > 0 && report.Bands.Detail != null)
        {
            var detailHeightDots = gen.PointsToDots(report.Bands.Detail.Height);
            foreach (var row in dataRows)
            {
                foreach (var elem in report.Bands.Detail.Elements)
                {
                    RenderElement(gen, elem, row, currentY);
                }
                currentY += detailHeightDots;
            }
        }
        else if (report.Bands.Detail != null)
        {
            foreach (var elem in report.Bands.Detail.Elements)
            {
                RenderElement(gen, elem, null, currentY);
            }
            currentY += gen.PointsToDots(report.Bands.Detail.Height);
        }

        // Footer Elements
        if (report.Bands.PageFooter != null)
        {
            foreach (var elem in report.Bands.PageFooter.Elements)
            {
                RenderElement(gen, elem, null, currentY);
            }
        }

        gen.EndLabel();
        return gen.Build();
    }

    private static void RenderElement(ZplGenerator gen, ElementDefinition elem, IDictionary<string, object?>? row, int yOffsetDots)
    {
        int x = gen.PointsToDots(elem.X);
        int y = gen.PointsToDots(elem.Y) + yOffsetDots;
        int w = gen.PointsToDots(elem.Width);
        int h = gen.PointsToDots(elem.Height);

        if (elem.Type == ElementType.Text)
        {
            var text = elem.Text;
            if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(elem.Expression) && row != null)
            {
                var fieldKey = elem.Expression.Replace("=Fields.", "").Replace("Fields.", "").Trim();
                if (row.TryGetValue(fieldKey, out var val) && val != null)
                {
                    text = val.ToString();
                }
            }

            if (!string.IsNullOrEmpty(text))
            {
                int fontHeight = elem.Style?.FontSize > 0 ? gen.PointsToDots(elem.Style.FontSize) : 28;
                gen.Text(x, y, text, fontHeight);
            }
        }
        else if (elem.Type == ElementType.QrCode)
        {
            var qrText = elem.Text ?? "12345678";
            gen.QrCode(x, y, qrText, magnification: 5);
        }
        else if (elem.Type == ElementType.Barcode)
        {
            var barcodeText = elem.Text ?? "12345678";
            gen.Barcode128(x, y, barcodeText, height: h > 0 ? h : 60);
        }
        else if (elem.Type == ElementType.Shape)
        {
            if (elem.ShapeType == ShapeType.Line)
            {
                gen.Line(x, y, w, thickness: Math.Max(2, h));
            }
            else
            {
                gen.Box(x, y, w, h, borderThickness: 2);
            }
        }
    }
}
