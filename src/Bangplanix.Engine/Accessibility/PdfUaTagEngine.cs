using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Accessibility;

public static class PdfUaTagEngine
{
    public static PdfStructElement BuildStructureTreeFromReport(ReportDefinition report, AccessibilityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        var opt = options ?? new AccessibilityOptions();

        var root = new PdfStructElement
        {
            TagType = PdfTagType.Document,
            Title = opt.DocumentTitle ?? report.Metadata.Title ?? "Bangplanix Tagged Report",
            Language = opt.PrimaryLanguage
        };

        // 1. Report Header -> H1
        if (report.Bands.ReportHeader != null)
        {
            var headerSection = new PdfStructElement { TagType = PdfTagType.Section };
            foreach (var el in report.Bands.ReportHeader.Elements)
            {
                headerSection.Children.Add(MapElementToStructElem(el, isHeading: true, headingLevel: 1, opt));
            }
            root.Children.Add(headerSection);
        }

        // 2. Page Header -> H2
        if (report.Bands.PageHeader != null)
        {
            var pageHeaderSec = new PdfStructElement { TagType = PdfTagType.Section };
            foreach (var el in report.Bands.PageHeader.Elements)
            {
                pageHeaderSec.Children.Add(MapElementToStructElem(el, isHeading: true, headingLevel: 2, opt));
            }
            root.Children.Add(pageHeaderSec);
        }

        // 3. Detail Band -> Table / Rows
        if (report.Bands.Detail != null)
        {
            var detailSec = new PdfStructElement { TagType = PdfTagType.Section, Title = "Detail Data" };
            var tableElem = new PdfStructElement { TagType = PdfTagType.Table };
            var tableRow = new PdfStructElement { TagType = PdfTagType.TableRow };

            foreach (var el in report.Bands.Detail.Elements)
            {
                var td = new PdfStructElement { TagType = PdfTagType.TableData };
                td.Children.Add(MapElementToStructElem(el, isHeading: false, headingLevel: 0, opt));
                tableRow.Children.Add(td);
            }

            tableElem.Children.Add(tableRow);
            detailSec.Children.Add(tableElem);
            root.Children.Add(detailSec);
        }

        // 4. Report Footer -> Section
        if (report.Bands.ReportFooter != null)
        {
            var footerSec = new PdfStructElement { TagType = PdfTagType.Section };
            foreach (var el in report.Bands.ReportFooter.Elements)
            {
                footerSec.Children.Add(MapElementToStructElem(el, isHeading: false, headingLevel: 0, opt));
            }
            root.Children.Add(footerSec);
        }

        return root;
    }

    private static PdfStructElement MapElementToStructElem(ElementDefinition element, bool isHeading, int headingLevel, AccessibilityOptions options)
    {
        switch (element.Type)
        {
            case ElementType.Text:
                return new PdfStructElement
                {
                    TagType = isHeading ? (headingLevel == 1 ? PdfTagType.H1 : (headingLevel == 2 ? PdfTagType.H2 : PdfTagType.H3)) : PdfTagType.Paragraph,
                    Title = element.Text,
                    ActualText = element.Text
                };

            case ElementType.Image:
                return new PdfStructElement
                {
                    TagType = PdfTagType.Figure,
                    Title = "Image",
                    AltText = string.IsNullOrWhiteSpace(element.Text) ? options.DefaultImageAltText : element.Text
                };

            case ElementType.Barcode:
            case ElementType.QrCode:
                return new PdfStructElement
                {
                    TagType = PdfTagType.Figure,
                    Title = "Barcode / QR",
                    AltText = $"บาร์โค้ดข้อมูล: {element.Text ?? "Barcode payload"}",
                    ActualText = element.Text
                };

            case ElementType.Chart:
                return new PdfStructElement
                {
                    TagType = PdfTagType.Figure,
                    Title = element.Chart?.Title ?? "Chart",
                    AltText = string.IsNullOrWhiteSpace(element.Chart?.Title) ? options.DefaultChartAltText : $"แผนภูมิแสดง: {element.Chart.Title}"
                };

            case ElementType.Shape:
                return new PdfStructElement
                {
                    TagType = PdfTagType.Artifact,
                    Title = "Decorative Shape"
                };

            default:
                return new PdfStructElement
                {
                    TagType = PdfTagType.Paragraph,
                    Title = element.Text
                };
        }
    }

    public static byte[] ApplyPdfUaUniversalAccessibility(byte[] pdfBytes, ReportDefinition report, AccessibilityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        var opt = options ?? new AccessibilityOptions();
        if (!opt.EnablePdfUa) return pdfBytes;

        var structTree = BuildStructureTreeFromReport(report, opt);

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("% BANGPLANIX-PDF-UA-TAGS-START");

        // Document Catalog additions: /Lang, /ViewerPreferences, /MarkInfo
        sb.AppendLine("<<");
        sb.AppendLine("  /Type /Catalog");
        sb.Append("  /Lang (").Append(opt.PrimaryLanguage).AppendLine(")");
        sb.AppendLine("  /MarkInfo << /Marked true >>");
        if (opt.DisplayDocTitle)
        {
            sb.AppendLine("  /ViewerPreferences << /DisplayDocTitle true >>");
        }

        // StructTreeRoot
        sb.AppendLine("  /StructTreeRoot <<");
        sb.AppendLine("    /Type /StructTreeRoot");
        sb.AppendLine("    /RoleMap <<");
        sb.AppendLine("      /Document /Document");
        sb.AppendLine("      /H1 /H1");
        sb.AppendLine("      /H2 /H2");
        sb.AppendLine("      /P /P");
        sb.AppendLine("      /Table /Table");
        sb.AppendLine("      /TR /TR");
        sb.AppendLine("      /TH /TH");
        sb.AppendLine("      /TD /TD");
        sb.AppendLine("      /Figure /Figure");
        sb.AppendLine("      /Artifact /Artifact");
        sb.AppendLine("    >>");
        sb.AppendLine("    /K [");

        AppendStructElement(sb, structTree, "      ");

        sb.AppendLine("    ]");
        sb.AppendLine("  >>");
        sb.AppendLine(">>");

        // XMP PDF/UA Identification Metadata
        sb.AppendLine("% PDF/UA-1 (ISO 14289-1) Identification Schema");
        sb.AppendLine("<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">");
        sb.AppendLine("  <rdf:Description rdf:about=\"\" xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\">");
        sb.AppendLine("    <pdfuaid:part>1</pdfuaid:part>");
        sb.AppendLine("  </rdf:Description>");
        sb.AppendLine("</rdf:RDF>");
        sb.AppendLine("% BANGPLANIX-PDF-UA-TAGS-END");

        using var ms = new MemoryStream();
        ms.Write(pdfBytes);
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        ms.Write(bytes);

        return ms.ToArray();
    }

    private static void AppendStructElement(StringBuilder sb, PdfStructElement elem, string indent)
    {
        sb.Append(indent).AppendLine("<<");
        sb.Append(indent).Append("  /Type /StructElem\n");
        sb.Append(indent).Append("  /S /").Append(elem.TagType.ToString()).AppendLine();

        if (!string.IsNullOrWhiteSpace(elem.Title))
        {
            sb.Append(indent).Append("  /T (").Append(elem.Title.Replace("(", "\\(").Replace(")", "\\)")).AppendLine(")");
        }

        if (!string.IsNullOrWhiteSpace(elem.AltText))
        {
            sb.Append(indent).Append("  /Alt (").Append(elem.AltText.Replace("(", "\\(").Replace(")", "\\)")).AppendLine(")");
        }

        if (!string.IsNullOrWhiteSpace(elem.ActualText))
        {
            sb.Append(indent).Append("  /ActualText (").Append(elem.ActualText.Replace("(", "\\(").Replace(")", "\\)")).AppendLine(")");
        }

        if (elem.Children.Count > 0)
        {
            sb.Append(indent).AppendLine("  /K [");
            foreach (var child in elem.Children)
            {
                AppendStructElement(sb, child, indent + "    ");
            }
            sb.Append(indent).AppendLine("  ]");
        }

        sb.Append(indent).AppendLine(">>");
    }
}
