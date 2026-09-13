using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Jaspersoft;

public sealed class JaspersoftJrxmlAdapter : ILegacyReportAdapter
{
    public string FormatName => "Jaspersoft JRXML";
    public IReadOnlyList<string> SupportedExtensions => [".jrxml"];

    public ReportDefinition Convert(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sanitized = LegacyScriptSanitizer.Sanitize(content);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root;
        if (root == null) throw new InvalidOperationException("Invalid JRXML XML document: missing root element.");

        var ns = root.Name.Namespace;

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("name")?.Value ?? "Jaspersoft Imported Report",
                Author = "Jaspersoft Migration"
            }
        };

        // 1. Page Dimensions
        var pWidth = root.Attribute("pageWidth")?.Value;
        var pHeight = root.Attribute("pageHeight")?.Value;
        if (!string.IsNullOrEmpty(pWidth)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(pWidth, 595);
        if (!string.IsNullOrEmpty(pHeight)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(pHeight, 842);

        var lMargin = root.Attribute("leftMargin")?.Value;
        var rMargin = root.Attribute("rightMargin")?.Value;
        var tMargin = root.Attribute("topMargin")?.Value;
        var bMargin = root.Attribute("bottomMargin")?.Value;
        report.PageSetup.Margins = new MarginDefinition
        {
            Left = LegacyUnitNormalizer.ConvertToPoints(lMargin, 20),
            Right = LegacyUnitNormalizer.ConvertToPoints(rMargin, 20),
            Top = LegacyUnitNormalizer.ConvertToPoints(tMargin, 20),
            Bottom = LegacyUnitNormalizer.ConvertToPoints(bMargin, 20)
        };

        // 2. Query String -> Dataset
        var queryString = root.Element(ns + "queryString")?.Value;
        if (!string.IsNullOrEmpty(queryString))
        {
            report.Datasets.Add(new DatasetDefinition
            {
                Name = "MainDataset",
                Type = DatasetType.Sql,
                QueryOrUrl = queryString.Trim()
            });
        }

        // 3. Parameters
        foreach (var pElem in root.Elements(ns + "parameter"))
        {
            var pName = pElem.Attribute("name")?.Value ?? string.Empty;
            var pClass = pElem.Attribute("class")?.Value ?? "java.lang.String";
            if (pName.StartsWith("REPORT_", StringComparison.OrdinalIgnoreCase)) continue; // skip built-in

            var pDef = new ParameterDefinition
            {
                Name = pName,
                Label = pName,
                Type = MapJavaClassToParameterType(pClass)
            };

            var defaultExpr = pElem.Element(ns + "defaultValueExpression")?.Value;
            if (!string.IsNullOrEmpty(defaultExpr))
            {
                pDef.DefaultValue = defaultExpr.Trim('"', '\'');
            }

            report.Parameters.Add(pDef);
        }

        // 4. Page Header / Title
        var titleBand = root.Element(ns + "title")?.Element(ns + "band");
        var pageHeaderBand = root.Element(ns + "pageHeader")?.Element(ns + "band") ?? titleBand;
        if (pageHeaderBand != null)
        {
            var h = LegacyUnitNormalizer.ConvertToPoints(pageHeaderBand.Attribute("height")?.Value, 40);
            report.Bands.PageHeader = new BandDefinition
            {
                Height = h,
                Elements = ParseJasperBandElements(pageHeaderBand, ns)
            };
        }

        // 5. Detail Band
        var detailContainer = root.Element(ns + "detail");
        var detailBand = detailContainer?.Element(ns + "band");
        if (detailBand != null)
        {
            var h = LegacyUnitNormalizer.ConvertToPoints(detailBand.Attribute("height")?.Value, 60);
            report.Bands.Detail = new BandDefinition
            {
                Height = h,
                Elements = ParseJasperBandElements(detailBand, ns)
            };
        }

        // 6. Page Footer
        var pageFooterBand = root.Element(ns + "pageFooter")?.Element(ns + "band");
        if (pageFooterBand != null)
        {
            var h = LegacyUnitNormalizer.ConvertToPoints(pageFooterBand.Attribute("height")?.Value, 30);
            report.Bands.PageFooter = new BandDefinition
            {
                Height = h,
                Elements = ParseJasperBandElements(pageFooterBand, ns)
            };
        }

        return report;
    }

    private static List<ElementDefinition> ParseJasperBandElements(XElement band, XNamespace ns)
    {
        var list = new List<ElementDefinition>();

        // StaticText
        foreach (var st in band.Elements(ns + "staticText"))
        {
            var re = st.Element(ns + "reportElement");
            var text = st.Element(ns + "text")?.Value ?? string.Empty;

            list.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                Text = text,
                X = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("x")?.Value, 0),
                Y = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("y")?.Value, 0),
                Width = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("width")?.Value, 100),
                Height = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("height")?.Value, 20),
                Style = ParseJasperStyle(st.Element(ns + "textElement"), ns)
            });
        }

        // TextField
        foreach (var tf in band.Elements(ns + "textField"))
        {
            var re = tf.Element(ns + "reportElement");
            var expr = tf.Element(ns + "textFieldExpression")?.Value ?? string.Empty;

            list.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                Expression = LegacyExpressionTranspiler.Transpile(expr, "JASPER"),
                X = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("x")?.Value, 0),
                Y = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("y")?.Value, 0),
                Width = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("width")?.Value, 100),
                Height = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("height")?.Value, 20),
                Style = ParseJasperStyle(tf.Element(ns + "textElement"), ns)
            });
        }

        // Image
        foreach (var img in band.Elements(ns + "image"))
        {
            var re = img.Element(ns + "reportElement");
            var expr = img.Element(ns + "imageExpression")?.Value ?? string.Empty;

            list.Add(new ElementDefinition
            {
                Type = ElementType.Image,
                ImageSource = expr.Trim('"', '\''),
                X = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("x")?.Value, 0),
                Y = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("y")?.Value, 0),
                Width = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("width")?.Value, 100),
                Height = LegacyUnitNormalizer.ConvertToPoints(re?.Attribute("height")?.Value, 100)
            });
        }

        return list;
    }

    private static StyleDefinition ParseJasperStyle(XElement? textElem, XNamespace ns)
    {
        var style = new StyleDefinition();
        if (textElem == null) return style;

        var fontElem = textElem.Element(ns + "font");
        if (fontElem != null)
        {
            var fam = fontElem.Attribute("fontName")?.Value;
            if (!string.IsNullOrEmpty(fam)) style.FontFamily = fam;

            var size = fontElem.Attribute("size")?.Value;
            if (!string.IsNullOrEmpty(size)) style.FontSize = LegacyUnitNormalizer.ConvertToPoints(size, 10.0);

            if (fontElem.Attribute("isBold")?.Value == "true") style.FontWeight = "Bold";
            if (fontElem.Attribute("isItalic")?.Value == "true") style.FontStyle = "Italic";
        }

        var align = textElem.Attribute("textAlignment")?.Value;
        if (!string.IsNullOrEmpty(align))
        {
            style.Align = align.ToUpperInvariant() switch
            {
                "CENTER" => HorizontalAlign.Center,
                "RIGHT" => HorizontalAlign.Right,
                "JUSTIFY" => HorizontalAlign.Justify,
                _ => HorizontalAlign.Left
            };
        }

        return style;
    }

    private static ParameterType MapJavaClassToParameterType(string javaClass)
    {
        return javaClass switch
        {
            "java.lang.Integer" or "java.lang.Long" or "java.lang.Double" or "java.lang.Float" or "java.math.BigDecimal" => ParameterType.Number,
            "java.util.Date" or "java.sql.Date" or "java.sql.Timestamp" => ParameterType.DateTime,
            "java.lang.Boolean" => ParameterType.Boolean,
            _ => ParameterType.String
        };
    }
}
