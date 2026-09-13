using Bangplanix.Core.Models;

namespace Bangplanix.Printing.EscPos;

public static class EscPosReportRenderer
{
    public static byte[] RenderToEscPos(ReportDefinition report, IReadOnlyList<IDictionary<string, object?>>? dataRows = null, int lineWidth = 42)
    {
        ArgumentNullException.ThrowIfNull(report);
        var gen = EscPosGenerator.CreateThai();

        // 1. Header (Title / Author)
        if (!string.IsNullOrEmpty(report.Metadata?.Title))
        {
            gen.Align(EscPosAlignment.Center)
               .Bold(true)
               .TextSize(2, 2)
               .TextLine(report.Metadata.Title)
               .Bold(false)
               .TextSize(1, 1);
        }

        if (report.Bands.PageHeader != null)
        {
            foreach (var elem in report.Bands.PageHeader.Elements)
            {
                RenderElement(gen, elem, null, lineWidth);
            }
        }

        gen.Align(EscPosAlignment.Left).SeparatorLine('-', lineWidth);

        // 2. Data Rows (Detail Band)
        if (dataRows != null && dataRows.Count > 0 && report.Bands.Detail != null)
        {
            foreach (var row in dataRows)
            {
                foreach (var elem in report.Bands.Detail.Elements)
                {
                    RenderElement(gen, elem, row, lineWidth);
                }
            }
        }
        else if (report.Bands.Detail != null)
        {
            foreach (var elem in report.Bands.Detail.Elements)
            {
                RenderElement(gen, elem, null, lineWidth);
            }
        }

        gen.SeparatorLine('-', lineWidth);

        // 3. Footer Band
        if (report.Bands.PageFooter != null)
        {
            foreach (var elem in report.Bands.PageFooter.Elements)
            {
                RenderElement(gen, elem, null, lineWidth);
            }
        }

        // Cut paper
        gen.Cut(EscPosCutType.Partial, 3);

        return gen.ToByteArray();
    }

    private static void RenderElement(EscPosGenerator gen, ElementDefinition elem, IDictionary<string, object?>? row, int lineWidth)
    {
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
                var isBold = elem.Style?.FontWeight?.Equals("Bold", StringComparison.OrdinalIgnoreCase) == true;
                var align = elem.Style?.Align switch
                {
                    HorizontalAlign.Center => EscPosAlignment.Center,
                    HorizontalAlign.Right => EscPosAlignment.Right,
                    _ => EscPosAlignment.Left
                };

                gen.Align(align)
                   .Bold(isBold)
                   .TextLine(text)
                   .Bold(false);
            }
        }
        else if (elem.Type == ElementType.Barcode)
        {
            var barcodeText = elem.Text ?? "12345678";
            gen.Align(EscPosAlignment.Center)
               .Barcode128(barcodeText)
               .Align(EscPosAlignment.Left);
        }
    }
}
