using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Assembly;

public static class PdfHyperlinkEngine
{
    public static byte[] InjectLinkAnnotations(byte[] pdfBytes, IReadOnlyList<(int PageNumber, PdfLinkAnnotation Link)> links)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (links == null || links.Count == 0) return pdfBytes;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("% BANGPLANIX-PDF-LINKS-START");

        foreach (var (pageNum, link) in links)
        {
            var r = link.Rect;
            var r0 = r.Length > 0 ? r[0] : 0;
            var r1 = r.Length > 1 ? r[1] : 0;
            var r2 = r.Length > 2 ? r[2] : 100;
            var r3 = r.Length > 3 ? r[3] : 20;

            sb.AppendLine("<<");
            sb.AppendLine("  /Type /Annot");
            sb.AppendLine("  /Subtype /Link");
            sb.Append(CultureInfo.InvariantCulture, $"  /Rect [{r0:F1} {r1:F1} {r2:F1} {r3:F1}]").AppendLine();
            sb.AppendLine("  /Border [0 0 0]");

            if (!string.IsNullOrWhiteSpace(link.Uri))
            {
                sb.Append("  /A << /S /URI /URI (").Append(link.Uri.Replace("(", "\\(").Replace(")", "\\)")).AppendLine(") >>");
            }
            else if (link.TargetPageNumber.HasValue)
            {
                sb.Append("  /Dest [").Append(link.TargetPageNumber.Value - 1).AppendLine(" /XYZ null null null]");
            }

            sb.AppendLine(">>");
        }

        sb.AppendLine("% BANGPLANIX-PDF-LINKS-END");

        using var ms = new MemoryStream();
        ms.Write(pdfBytes);
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        ms.Write(bytes);

        return ms.ToArray();
    }
}
