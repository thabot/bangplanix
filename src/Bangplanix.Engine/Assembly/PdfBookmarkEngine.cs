using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Assembly;

public static class PdfBookmarkEngine
{
    public static List<PdfBookmarkNode> BuildOutlineTreeFromSections(IReadOnlyList<DossierSectionDefinition> sections, int initialPageOffset = 1)
    {
        var tree = new List<PdfBookmarkNode>();
        var currentPage = initialPageOffset;

        foreach (var section in sections)
        {
            var sectionNode = new PdfBookmarkNode
            {
                Title = string.IsNullOrWhiteSpace(section.TocTitle) ? section.Title : section.TocTitle,
                PageNumber = currentPage,
                Level = 1,
                IsOpen = true
            };

            // Copy child bookmarks if any
            foreach (var child in section.Bookmarks)
            {
                sectionNode.Children.Add(new PdfBookmarkNode
                {
                    Title = child.Title,
                    PageNumber = currentPage + (child.PageNumber - 1),
                    Level = child.Level + 1,
                    IsOpen = child.IsOpen
                });
            }

            tree.Add(sectionNode);

            // Estimate section page count
            var sectionPageCount = 1;
            if (section.DataRows.Count > 0)
            {
                sectionPageCount = Math.Max(1, (int)Math.Ceiling(section.DataRows.Count / 25.0));
            }
            currentPage += sectionPageCount;
        }

        return tree;
    }

    public static byte[] InjectPdfBookmarks(byte[] pdfBytes, IReadOnlyList<PdfBookmarkNode> bookmarks)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        if (bookmarks == null || bookmarks.Count == 0) return pdfBytes;

        // Build standard PDF Outline objects text
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("% BANGPLANIX-PDF-OUTLINES-START");

        var totalItems = CountTotalNodes(bookmarks);

        sb.AppendLine("<<");
        sb.AppendLine("  /Type /Outlines");
        sb.Append("  /Count ").Append(totalItems).AppendLine();
        sb.AppendLine(">>");

        foreach (var node in bookmarks)
        {
            AppendBookmarkNode(sb, node);
        }

        sb.AppendLine("% BANGPLANIX-PDF-OUTLINES-END");

        using var ms = new MemoryStream();
        ms.Write(pdfBytes);
        var outlineBytes = Encoding.UTF8.GetBytes(sb.ToString());
        ms.Write(outlineBytes);

        return ms.ToArray();
    }

    private static void AppendBookmarkNode(StringBuilder sb, PdfBookmarkNode node)
    {
        sb.AppendLine("<<");
        sb.Append("  /Title (").Append(node.Title.Replace("(", "\\(").Replace(")", "\\)")).AppendLine(")");
        sb.Append("  /Dest [").Append(node.PageNumber - 1).AppendLine(" /XYZ null null null]");
        if (node.Children.Count > 0)
        {
            sb.Append("  /Count ").Append(node.Children.Count).AppendLine();
        }
        sb.AppendLine(">>");

        foreach (var child in node.Children)
        {
            AppendBookmarkNode(sb, child);
        }
    }

    private static int CountTotalNodes(IReadOnlyList<PdfBookmarkNode> nodes)
    {
        int count = 0;
        foreach (var n in nodes)
        {
            count += 1 + CountTotalNodes(n.Children);
        }
        return count;
    }
}
