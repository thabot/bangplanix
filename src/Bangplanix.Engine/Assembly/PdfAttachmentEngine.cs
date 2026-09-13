using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Assembly;

public static class PdfAttachmentEngine
{
    public static byte[] InjectEmbeddedAttachments(byte[] pdfBytes, IReadOnlyList<PdfAttachmentDefinition> attachments)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (attachments == null || attachments.Count == 0) return pdfBytes;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("% BANGPLANIX-PDF-EMBEDDED-FILES-START");
        sb.AppendLine("<<");
        sb.AppendLine("  /Type /Catalog");
        sb.AppendLine("  /Names <<");
        sb.AppendLine("    /EmbeddedFiles <<");
        sb.AppendLine("      /Names [");

        foreach (var att in attachments)
        {
            var cleanName = att.FileName.Replace("(", "\\(").Replace(")", "\\)");
            sb.Append("        (").Append(cleanName).Append(") <<\n");
            sb.AppendLine("          /Type /Filespec");
            sb.Append("          /F (").Append(cleanName).AppendLine(")");
            sb.Append("          /UF (").Append(cleanName).AppendLine(")");
            sb.AppendLine("          /EF << /F <<");
            sb.AppendLine("            /Type /EmbeddedFile");
            sb.Append("            /Subtype /").Append(att.MediaType.Replace("/", "#2F")).AppendLine();
            sb.Append("            /Length ").Append(att.Data.Length).AppendLine();
            sb.AppendLine("          >> >>");
            sb.AppendLine("        >>");
        }

        sb.AppendLine("      ]");
        sb.AppendLine("    >>");
        sb.AppendLine("  >>");
        sb.AppendLine(">>");
        sb.AppendLine("% BANGPLANIX-PDF-EMBEDDED-FILES-END");

        using var ms = new MemoryStream();
        ms.Write(pdfBytes);
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        ms.Write(bytes);

        return ms.ToArray();
    }
}
