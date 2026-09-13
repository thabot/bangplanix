using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Adobe;

public sealed class AdobeXdpAdapter : ILegacyReportAdapter
{
    public string FormatName => "Adobe LiveCycle / XFA / SAP ADS";
    public IReadOnlyList<string> SupportedExtensions => [".xdp", ".xfa.xml", ".xfa"];

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var sanitized = LegacyScriptSanitizer.Sanitize(content);
        var doc = XDocument.Parse(sanitized);
        var xdpRoot = doc.Root;
        if (xdpRoot == null)
        {
            throw new InvalidOperationException("Invalid or empty Adobe XFA XML document.");
        }

        // Find the <template> element (either directly or within <xdp:xdp>)
        var templateEl = xdpRoot.Name.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase)
            ? xdpRoot
            : xdpRoot.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("template", StringComparison.OrdinalIgnoreCase));

        if (templateEl == null)
        {
            throw new InvalidOperationException("Adobe XFA template (<template>) definition not found in XDP package.");
        }

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = templateEl.Attribute("name")?.Value ?? "AdobeXfaReport",
                Author = "Adobe XFA / SAP ADS Converter"
            },
            PageSetup = ParsePageSetup(templateEl)
        };

        // Extract Root Subforms & Content
        var rootSubform = templateEl.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("subform", StringComparison.OrdinalIgnoreCase));
        if (rootSubform != null)
        {
            ParseSubformTree(rootSubform, report);
        }

        // Ensure at least detail band exists
        report.Bands.Detail ??= new BandDefinition { Height = 100 };

        return report;
    }

    private static PageSetup ParsePageSetup(XElement templateEl)
    {
        var pageSetup = new PageSetup
        {
            PaperKind = PaperKind.A4,
            Orientation = PageOrientation.Portrait,
            Width = 595.28,
            Height = 841.89,
            Margins = new MarginDefinition { Top = 28.35, Bottom = 28.35, Left = 28.35, Right = 28.35 }
        };

        var pageArea = templateEl.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("pageArea", StringComparison.OrdinalIgnoreCase));
        if (pageArea != null)
        {
            var wAttr = pageArea.Attribute("w")?.Value;
            var hAttr = pageArea.Attribute("h")?.Value;

            if (!string.IsNullOrWhiteSpace(wAttr))
            {
                pageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(wAttr);
            }
            if (!string.IsNullOrWhiteSpace(hAttr))
            {
                pageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(hAttr);
            }

            if (pageSetup.Width > pageSetup.Height)
            {
                pageSetup.Orientation = PageOrientation.Landscape;
            }

            var medium = pageArea.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("medium", StringComparison.OrdinalIgnoreCase));
            if (medium != null)
            {
                var stock = medium.Attribute("stock")?.Value;
                if (!string.IsNullOrWhiteSpace(stock))
                {
                    if (stock.Contains("a4", StringComparison.OrdinalIgnoreCase)) pageSetup.PaperKind = PaperKind.A4;
                    else if (stock.Contains("a3", StringComparison.OrdinalIgnoreCase)) pageSetup.PaperKind = PaperKind.A3;
                    else if (stock.Contains("a5", StringComparison.OrdinalIgnoreCase)) pageSetup.PaperKind = PaperKind.A5;
                    else if (stock.Contains("letter", StringComparison.OrdinalIgnoreCase)) pageSetup.PaperKind = PaperKind.Letter;
                    else if (stock.Contains("legal", StringComparison.OrdinalIgnoreCase)) pageSetup.PaperKind = PaperKind.Legal;
                }
            }

            var margins = pageArea.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("margin", StringComparison.OrdinalIgnoreCase) || e.Name.LocalName.Equals("margins", StringComparison.OrdinalIgnoreCase));
            if (margins != null)
            {
                if (margins.Attribute("topInset") != null) pageSetup.Margins.Top = LegacyUnitNormalizer.ConvertToPoints(margins.Attribute("topInset")!.Value);
                if (margins.Attribute("bottomInset") != null) pageSetup.Margins.Bottom = LegacyUnitNormalizer.ConvertToPoints(margins.Attribute("bottomInset")!.Value);
                if (margins.Attribute("leftInset") != null) pageSetup.Margins.Left = LegacyUnitNormalizer.ConvertToPoints(margins.Attribute("leftInset")!.Value);
                if (margins.Attribute("rightInset") != null) pageSetup.Margins.Right = LegacyUnitNormalizer.ConvertToPoints(margins.Attribute("rightInset")!.Value);
            }
        }

        return pageSetup;
    }

    private static void ParseSubformTree(XElement rootSubform, ReportDefinition report)
    {
        var childSubforms = rootSubform.Elements().Where(e => e.Name.LocalName.Equals("subform", StringComparison.OrdinalIgnoreCase)).ToList();

        if (childSubforms.Count == 0)
        {
            var band = new BandDefinition
            {
                Height = GetSubformHeight(rootSubform, 150)
            };
            ExtractElementsFromSubform(rootSubform, band);
            report.Bands.Detail = band;
            return;
        }

        foreach (var subform in childSubforms)
        {
            var subformName = subform.Attribute("name")?.Value ?? "Subform";
            var occur = subform.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("occur", StringComparison.OrdinalIgnoreCase));
            var maxOccur = occur?.Attribute("max")?.Value;

            var isRepeater = maxOccur == "-1" || (int.TryParse(maxOccur, out var maxVal) && maxVal > 1) || subformName.Contains("detail", StringComparison.OrdinalIgnoreCase) || subformName.Contains("item", StringComparison.OrdinalIgnoreCase) || subformName.Contains("row", StringComparison.OrdinalIgnoreCase);

            var band = new BandDefinition
            {
                Height = GetSubformHeight(subform, 50)
            };

            ExtractElementsFromSubform(subform, band);

            if (subformName.Contains("header", StringComparison.OrdinalIgnoreCase) || subformName.Contains("pageheader", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.PageHeader = band;
            }
            else if (subformName.Contains("footer", StringComparison.OrdinalIgnoreCase) || subformName.Contains("pagefooter", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.PageFooter = band;
            }
            else
            {
                report.Bands.Detail = band;
            }
        }
    }

    private static double GetSubformHeight(XElement subform, double fallback)
    {
        var hAttr = subform.Attribute("h")?.Value;
        if (!string.IsNullOrWhiteSpace(hAttr))
        {
            return LegacyUnitNormalizer.ConvertToPoints(hAttr);
        }

        double maxY = 0;
        foreach (var child in subform.Elements().Where(e => e.Name.LocalName is "field" or "draw" or "subform"))
        {
            var y = child.Attribute("y") != null ? LegacyUnitNormalizer.ConvertToPoints(child.Attribute("y")!.Value) : 0;
            var h = child.Attribute("h") != null ? LegacyUnitNormalizer.ConvertToPoints(child.Attribute("h")!.Value) : 20;
            if (y + h > maxY) maxY = y + h;
        }

        return maxY > 0 ? maxY + 10 : fallback;
    }

    private static void ExtractElementsFromSubform(XElement subform, BandDefinition band)
    {
        // Extract <field>
        foreach (var fieldEl in subform.Elements().Where(e => e.Name.LocalName.Equals("field", StringComparison.OrdinalIgnoreCase)))
        {
            var elem = ConvertField(fieldEl);
            if (elem != null)
            {
                band.Elements.Add(elem);
            }
        }

        // Extract <draw> (static text, shapes, lines)
        foreach (var drawEl in subform.Elements().Where(e => e.Name.LocalName.Equals("draw", StringComparison.OrdinalIgnoreCase)))
        {
            var elem = ConvertDraw(drawEl);
            if (elem != null)
            {
                band.Elements.Add(elem);
            }
        }
    }

    private static ElementDefinition? ConvertField(XElement fieldEl)
    {
        var fieldName = fieldEl.Attribute("name")?.Value ?? "Field";
        var x = fieldEl.Attribute("x") != null ? LegacyUnitNormalizer.ConvertToPoints(fieldEl.Attribute("x")!.Value) : 0;
        var y = fieldEl.Attribute("y") != null ? LegacyUnitNormalizer.ConvertToPoints(fieldEl.Attribute("y")!.Value) : 0;
        var w = fieldEl.Attribute("w") != null ? LegacyUnitNormalizer.ConvertToPoints(fieldEl.Attribute("w")!.Value) : 120;
        var h = fieldEl.Attribute("h") != null ? LegacyUnitNormalizer.ConvertToPoints(fieldEl.Attribute("h")!.Value) : 20;

        var elem = new ElementDefinition
        {
            Id = $"xfa_{fieldName}_{Guid.NewGuid():N}",
            X = x,
            Y = y,
            Width = w,
            Height = h,
            Type = ElementType.Text
        };

        // Determine UI Type (Text, Barcode, etc.)
        var uiEl = fieldEl.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("ui", StringComparison.OrdinalIgnoreCase));
        if (uiEl != null)
        {
            var barcodeEl = uiEl.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("barcode", StringComparison.OrdinalIgnoreCase));
            if (barcodeEl != null)
            {
                var bcType = barcodeEl.Attribute("type")?.Value ?? "code128";
                if (bcType.Contains("qr", StringComparison.OrdinalIgnoreCase) || bcType.Contains("2d", StringComparison.OrdinalIgnoreCase))
                {
                    elem.Type = ElementType.QrCode;
                }
                else
                {
                    elem.Type = ElementType.Barcode;
                    elem.BarcodeType = BarcodeType.Code128;
                }
            }
        }

        // Check for Data Binding (<bind match="dataRef" ref="..."/>)
        var bindEl = fieldEl.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("bind", StringComparison.OrdinalIgnoreCase));
        var dataRef = bindEl?.Attribute("ref")?.Value;

        // Check for Calculate Script (<calculate><script ...>...</script></calculate>)
        var calcEl = fieldEl.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("calculate", StringComparison.OrdinalIgnoreCase));
        var scriptEl = calcEl?.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("script", StringComparison.OrdinalIgnoreCase));
        var scriptBody = scriptEl?.Value?.Trim();

        // Check for default/static value (<value><text>...</text></value>)
        var valueEl = fieldEl.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("value", StringComparison.OrdinalIgnoreCase));
        var staticVal = valueEl?.Value?.Trim();

        if (!string.IsNullOrWhiteSpace(scriptBody))
        {
            elem.Expression = LegacyExpressionTranspiler.Transpile(scriptBody, "XFA");
        }
        else if (!string.IsNullOrWhiteSpace(dataRef))
        {
            var cleanRef = dataRef.TrimStart('$', '.');
            if (cleanRef.Contains('.')) cleanRef = cleanRef[(cleanRef.LastIndexOf('.') + 1)..];
            elem.Expression = $"={{Fields.{cleanRef}}}";
        }
        else if (!string.IsNullOrWhiteSpace(staticVal))
        {
            elem.Text = staticVal;
        }
        else
        {
            elem.Expression = $"={{Fields.{fieldName}}}";
        }

        // Font and styling
        var fontEl = fieldEl.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("font", StringComparison.OrdinalIgnoreCase));
        if (fontEl != null)
        {
            elem.Style = new StyleDefinition();
            if (fontEl.Attribute("typeface") != null) elem.Style.FontFamily = fontEl.Attribute("typeface")!.Value;
            if (fontEl.Attribute("size") != null) elem.Style.FontSize = LegacyUnitNormalizer.ConvertToPoints(fontEl.Attribute("size")!.Value);
            if (fontEl.Attribute("weight")?.Value?.Equals("bold", StringComparison.OrdinalIgnoreCase) == true) elem.Style.FontWeight = "Bold";
            if (fontEl.Attribute("posture")?.Value?.Equals("italic", StringComparison.OrdinalIgnoreCase) == true) elem.Style.FontStyle = "Italic";
        }

        return elem;
    }

    private static ElementDefinition? ConvertDraw(XElement drawEl)
    {
        var drawName = drawEl.Attribute("name")?.Value ?? "Draw";
        var x = drawEl.Attribute("x") != null ? LegacyUnitNormalizer.ConvertToPoints(drawEl.Attribute("x")!.Value) : 0;
        var y = drawEl.Attribute("y") != null ? LegacyUnitNormalizer.ConvertToPoints(drawEl.Attribute("y")!.Value) : 0;
        var w = drawEl.Attribute("w") != null ? LegacyUnitNormalizer.ConvertToPoints(drawEl.Attribute("w")!.Value) : 100;
        var h = drawEl.Attribute("h") != null ? LegacyUnitNormalizer.ConvertToPoints(drawEl.Attribute("h")!.Value) : 20;

        var elem = new ElementDefinition
        {
            Id = $"xfa_draw_{drawName}_{Guid.NewGuid():N}",
            X = x,
            Y = y,
            Width = w,
            Height = h,
            Type = ElementType.Text
        };

        var lineEl = drawEl.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("line", StringComparison.OrdinalIgnoreCase));
        var rectEl = drawEl.Elements().FirstOrDefault(e => e.Name.LocalName.Equals("rectangle", StringComparison.OrdinalIgnoreCase));
        var textEl = drawEl.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("text", StringComparison.OrdinalIgnoreCase));

        if (lineEl != null)
        {
            elem.Type = ElementType.Shape;
        }
        else if (rectEl != null)
        {
            elem.Type = ElementType.Shape;
        }
        else if (textEl != null)
        {
            elem.Type = ElementType.Text;
            elem.Text = textEl.Value?.Trim() ?? string.Empty;
        }

        return elem;
    }
}
