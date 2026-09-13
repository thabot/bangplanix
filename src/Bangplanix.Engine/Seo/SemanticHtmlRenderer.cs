using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Seo;

public static class SemanticHtmlRenderer
{
    public static string RenderToSemanticHtml(
        ReportDefinition report,
        Dictionary<string, object>? data = null,
        ReportSeoMetadata? seo = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        seo ??= new ReportSeoMetadata
        {
            Title = report.Metadata?.Title ?? "Bangplanix Report",
            Description = report.Metadata?.Description ?? "Enterprise Paginated Report",
            Author = report.Metadata?.Author ?? "Bangplanix",
            SchemaType = "Report"
        };

        var metaTags = OpenGraphGenerator.GenerateMetaTags(seo);
        var jsonLd = JsonLdGenerator.GenerateJsonLd(seo, data);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"th\" class=\"bpx-html\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine(metaTags);
        sb.AppendLine("  <script type=\"application/ld+json\">");
        sb.AppendLine(jsonLd);
        sb.AppendLine("  </script>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    :root { --bpx-primary: #2563eb; --bpx-text: #1e293b; --bpx-border: #e2e8f0; --bpx-bg: #ffffff; }");
        sb.AppendLine("    body { font-family: 'Sarabun', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f8fafc; color: var(--bpx-text); margin: 0; padding: 24px; display: flex; justify-content: center; }");
        sb.AppendLine("    .bpx-report-container { width: 100%; max-width: 800px; background: var(--bpx-bg); padding: 36px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); border-radius: 8px; border: 1px solid var(--bpx-border); }");
        sb.AppendLine("    .bpx-header { border-bottom: 2px solid var(--bpx-border); padding-bottom: 16px; margin-bottom: 24px; }");
        sb.AppendLine("    .bpx-title { font-size: 24px; font-weight: bold; color: var(--bpx-primary); margin: 0 0 8px 0; }");
        sb.AppendLine("    .bpx-table { width: 100%; border-collapse: collapse; margin: 16px 0; font-size: 14px; }");
        sb.AppendLine("    .bpx-table th, .bpx-table td { padding: 10px 14px; border-bottom: 1px solid var(--bpx-border); text-align: left; }");
        sb.AppendLine("    .bpx-table th { background: #f1f5f9; font-weight: 600; }");
        sb.AppendLine("    .bpx-table td.bpx-num, .bpx-table th.bpx-num { text-align: right; }");
        sb.AppendLine("    .bpx-footer { border-top: 1px solid var(--bpx-border); padding-top: 16px; margin-top: 32px; font-size: 12px; color: #64748b; display: flex; justify-content: space-between; align-items: center; }");
        sb.AppendLine("    @media print { body { background: white; padding: 0; } .bpx-report-container { box-shadow: none; border: none; padding: 0; } }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.Append("  <article class=\"bpx-report-container\" role=\"document\" aria-label=\"").Append(WebUtility.HtmlEncode(seo.Title)).AppendLine("\">");

        // Header section
        sb.AppendLine("    <header class=\"bpx-header\" role=\"banner\">");
        sb.Append("      <h1 class=\"bpx-title\">").Append(WebUtility.HtmlEncode(seo.Title)).AppendLine("</h1>");
        if (!string.IsNullOrWhiteSpace(seo.Description))
        {
            sb.Append("      <p class=\"bpx-desc\">").Append(WebUtility.HtmlEncode(seo.Description)).AppendLine("</p>");
        }
        sb.AppendLine("    </header>");

        // Main content
        sb.AppendLine("    <main class=\"bpx-main\" role=\"main\">");

        // Render Bands
        if (report.Bands != null)
        {
            if (report.Bands.ReportHeader != null && report.Bands.ReportHeader.Elements.Count > 0)
            {
                sb.AppendLine("      <section class=\"bpx-band bpx-band-title\" aria-label=\"Title Section\">");
                foreach (var elem in report.Bands.ReportHeader.Elements)
                {
                    RenderElement(sb, elem);
                }
                sb.AppendLine("      </section>");
            }

            if (report.Bands.Detail != null && report.Bands.Detail.Elements.Count > 0)
            {
                sb.AppendLine("      <section class=\"bpx-band bpx-band-detail\" aria-label=\"Report Detail\">");
                var datasetName = report.Datasets.FirstOrDefault()?.Name ?? "items";
                var rows = ExtractRows(data, datasetName);

                if (rows.Count > 0)
                {
                    sb.AppendLine("        <table class=\"bpx-table\" role=\"table\">");
                    sb.AppendLine("          <thead>");
                    sb.AppendLine("            <tr>");
                    foreach (var elem in report.Bands.Detail.Elements)
                    {
                        var colName = elem.Expression?.TrimStart('=') ?? elem.Text ?? "Field";
                        var isNum = elem.Style?.Align == HorizontalAlign.Right;
                        var colClass = isNum ? "bpx-num" : "";
                        sb.Append("              <th scope=\"col\" class=\"").Append(colClass).Append("\">").Append(WebUtility.HtmlEncode(colName)).AppendLine("</th>");
                    }
                    sb.AppendLine("            </tr>");
                    sb.AppendLine("          </thead>");
                    sb.AppendLine("          <tbody>");
                    foreach (var row in rows)
                    {
                        sb.AppendLine("            <tr>");
                        foreach (var elem in report.Bands.Detail.Elements)
                        {
                            var val = EvaluateField(elem, row);
                            var isNum = elem.Style?.Align == HorizontalAlign.Right;
                            var tdClass = isNum ? "bpx-num" : "";
                            sb.Append("              <td class=\"").Append(tdClass).Append("\">").Append(WebUtility.HtmlEncode(val)).AppendLine("</td>");
                        }
                        sb.AppendLine("            </tr>");
                    }
                    sb.AppendLine("          </tbody>");
                    sb.AppendLine("        </table>");
                }
                else
                {
                    foreach (var elem in report.Bands.Detail.Elements)
                    {
                        RenderElement(sb, elem);
                    }
                }
                sb.AppendLine("      </section>");
            }
        }

        sb.AppendLine("    </main>");

        // Footer section
        sb.AppendLine("    <footer class=\"bpx-footer\" role=\"contentinfo\">");
        sb.AppendLine("      <span>Generated by Bangplanix Enterprise Engine</span>");
        sb.Append("      <span>").Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).AppendLine(" UTC</span>");
        sb.AppendLine("    </footer>");

        sb.AppendLine("  </article>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static void RenderElement(StringBuilder sb, ElementDefinition elem)
    {
        if (elem.Type == ElementType.Text)
        {
            var tag = (elem.Style?.FontSize ?? 10) >= 16 ? "h2" : "p";
            var text = elem.Text ?? elem.Expression ?? "";
            sb.Append("        <").Append(tag).Append(" class=\"bpx-text\">").Append(WebUtility.HtmlEncode(text)).Append("</").Append(tag).AppendLine(">");
        }
        else if (elem.Type == ElementType.Shape)
        {
            sb.AppendLine("        <hr class=\"bpx-divider\" />");
        }
        else if (elem.Type == ElementType.Barcode)
        {
            var bType = elem.BarcodeType?.ToString() ?? "Barcode";
            var bText = elem.Text ?? "";
            sb.Append("        <div class=\"bpx-barcode\" role=\"img\" aria-label=\"Barcode ").Append(WebUtility.HtmlEncode(bText)).Append("\">[")
              .Append(bType).Append(": ").Append(WebUtility.HtmlEncode(bText)).AppendLine("]</div>");
        }
    }

    private static List<Dictionary<string, object>> ExtractRows(Dictionary<string, object>? data, string datasetName)
    {
        if (data == null) return new List<Dictionary<string, object>>();

        if (data.TryGetValue(datasetName, out var dsVal))
        {
            if (dsVal is JsonElement jsonElem && jsonElem.ValueKind == JsonValueKind.Array)
            {
                var list = new List<Dictionary<string, object>>();
                foreach (var item in jsonElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in item.EnumerateObject())
                        {
                            dict[prop.Name] = prop.Value.ToString();
                        }
                        list.Add(dict);
                    }
                }
                return list;
            }
            if (dsVal is IEnumerable<Dictionary<string, object>> dictList)
            {
                return dictList.ToList();
            }
        }
        return new List<Dictionary<string, object>>();
    }

    private static string EvaluateField(ElementDefinition elem, Dictionary<string, object> row)
    {
        if (!string.IsNullOrWhiteSpace(elem.Text)) return elem.Text;
        if (string.IsNullOrWhiteSpace(elem.Expression)) return "";

        var expr = elem.Expression.TrimStart('=');
        if (expr.StartsWith("Fields!", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(".Value", StringComparison.OrdinalIgnoreCase))
        {
            var fieldName = expr[7..^6];
            if (row.TryGetValue(fieldName, out var val) && val != null)
            {
                return val.ToString() ?? "";
            }
        }
        return expr;
    }
}
