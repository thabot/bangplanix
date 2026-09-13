using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Ssrs;

public sealed class SsrsRdlAdapter : ILegacyReportAdapter
{
    public string FormatName => "SSRS / Power BI Paginated";
    public IReadOnlyList<string> SupportedExtensions => [".rdl", ".rdlc"];

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
        if (root == null) throw new InvalidOperationException("Invalid RDL XML document: missing root element.");

        var ns = root.Name.Namespace;

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Element(ns + "Description")?.Value ?? "SSRS Imported Report",
                Author = root.Element(ns + "Author")?.Value ?? "SSRS Migration"
            }
        };

        // 1. Page Setup & Dimensions
        var pageElem = root.Element(ns + "Page") ?? root;
        var pageHeight = pageElem.Element(ns + "PageHeight")?.Value;
        var pageWidth = pageElem.Element(ns + "PageWidth")?.Value;
        if (!string.IsNullOrEmpty(pageWidth))
        {
            report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(pageWidth, 595.28);
        }
        if (!string.IsNullOrEmpty(pageHeight))
        {
            report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(pageHeight, 841.89);
        }

        var lMargin = pageElem.Element(ns + "LeftMargin")?.Value;
        var rMargin = pageElem.Element(ns + "RightMargin")?.Value;
        var tMargin = pageElem.Element(ns + "TopMargin")?.Value;
        var bMargin = pageElem.Element(ns + "BottomMargin")?.Value;
        report.PageSetup.Margins = new MarginDefinition
        {
            Left = LegacyUnitNormalizer.ConvertToPoints(lMargin, 28.35),
            Right = LegacyUnitNormalizer.ConvertToPoints(rMargin, 28.35),
            Top = LegacyUnitNormalizer.ConvertToPoints(tMargin, 28.35),
            Bottom = LegacyUnitNormalizer.ConvertToPoints(bMargin, 28.35)
        };

        // 2. Report Parameters
        var paramContainer = root.Element(ns + "ReportParameters");
        if (paramContainer != null)
        {
            foreach (var pElem in paramContainer.Elements(ns + "ReportParameter"))
            {
                var pName = pElem.Attribute("Name")?.Value ?? pElem.Element(ns + "Name")?.Value ?? string.Empty;
                var pTypeStr = pElem.Element(ns + "DataType")?.Value ?? "String";
                var prompt = pElem.Element(ns + "Prompt")?.Value;

                var paramDef = new ParameterDefinition
                {
                    Name = pName,
                    Label = prompt ?? pName,
                    Type = MapParameterType(pTypeStr)
                };

                var defaultVal = pElem.Element(ns + "DefaultValue")?.Element(ns + "Values")?.Element(ns + "Value")?.Value;
                if (!string.IsNullOrEmpty(defaultVal))
                {
                    paramDef.DefaultValue = defaultVal;
                }

                report.Parameters.Add(paramDef);
            }
        }

        // 3. Datasets
        var datasetsContainer = root.Element(ns + "DataSets");
        if (datasetsContainer != null)
        {
            foreach (var dsElem in datasetsContainer.Elements(ns + "DataSet"))
            {
                var dsName = dsElem.Attribute("Name")?.Value ?? "DataSet1";
                var queryElem = dsElem.Element(ns + "Query");
                var commandText = queryElem?.Element(ns + "CommandText")?.Value ?? string.Empty;
                var connRef = queryElem?.Element(ns + "DataSourceName")?.Value;

                report.Datasets.Add(new DatasetDefinition
                {
                    Name = dsName,
                    Type = DatasetType.Sql,
                    ConnectionRef = connRef,
                    QueryOrUrl = commandText
                });
            }
        }

        // 4. Page Header
        var pageHeaderElem = pageElem.Element(ns + "PageHeader");
        if (pageHeaderElem != null)
        {
            var headerHeight = LegacyUnitNormalizer.ConvertToPoints(pageHeaderElem.Element(ns + "Height")?.Value, 40.0);
            report.Bands.PageHeader = new BandDefinition
            {
                Height = headerHeight,
                Elements = ParseReportItems(pageHeaderElem.Element(ns + "ReportItems"), ns)
            };
        }

        // 5. Body & Detail Band
        var bodyElem = root.Element(ns + "Body");
        if (bodyElem != null)
        {
            var bodyHeight = LegacyUnitNormalizer.ConvertToPoints(bodyElem.Element(ns + "Height")?.Value, 100.0);
            report.Bands.Detail = new BandDefinition
            {
                Height = bodyHeight,
                Elements = ParseReportItems(bodyElem.Element(ns + "ReportItems"), ns)
            };
        }

        // 6. Page Footer
        var pageFooterElem = pageElem.Element(ns + "PageFooter");
        if (pageFooterElem != null)
        {
            var footerHeight = LegacyUnitNormalizer.ConvertToPoints(pageFooterElem.Element(ns + "Height")?.Value, 30.0);
            report.Bands.PageFooter = new BandDefinition
            {
                Height = footerHeight,
                Elements = ParseReportItems(pageFooterElem.Element(ns + "ReportItems"), ns)
            };
        }

        return report;
    }

    private static List<ElementDefinition> ParseReportItems(XElement? itemsContainer, XNamespace ns)
    {
        var elements = new List<ElementDefinition>();
        if (itemsContainer == null) return elements;

        // Textboxes
        foreach (var tb in itemsContainer.Elements(ns + "Textbox"))
        {
            var elem = new ElementDefinition
            {
                Id = tb.Attribute("Name")?.Value,
                Type = ElementType.Text,
                X = LegacyUnitNormalizer.ConvertToPoints(tb.Element(ns + "Left")?.Value, 0),
                Y = LegacyUnitNormalizer.ConvertToPoints(tb.Element(ns + "Top")?.Value, 0),
                Width = LegacyUnitNormalizer.ConvertToPoints(tb.Element(ns + "Width")?.Value, 100),
                Height = LegacyUnitNormalizer.ConvertToPoints(tb.Element(ns + "Height")?.Value, 20),
                Style = ParseStyle(tb.Element(ns + "Style"), ns)
            };

            var valueStr = tb.Element(ns + "Value")?.Value ??
                           tb.Element(ns + "Paragraphs")?.Element(ns + "Paragraph")?.Element(ns + "TextRuns")?.Element(ns + "TextRun")?.Element(ns + "Value")?.Value;

            if (!string.IsNullOrEmpty(valueStr))
            {
                if (valueStr.StartsWith('='))
                {
                    elem.Expression = LegacyExpressionTranspiler.Transpile(valueStr, "SSRS");
                }
                else
                {
                    elem.Text = valueStr;
                }
            }

            elements.Add(elem);
        }

        // Images
        foreach (var img in itemsContainer.Elements(ns + "Image"))
        {
            var elem = new ElementDefinition
            {
                Id = img.Attribute("Name")?.Value,
                Type = ElementType.Image,
                X = LegacyUnitNormalizer.ConvertToPoints(img.Element(ns + "Left")?.Value, 0),
                Y = LegacyUnitNormalizer.ConvertToPoints(img.Element(ns + "Top")?.Value, 0),
                Width = LegacyUnitNormalizer.ConvertToPoints(img.Element(ns + "Width")?.Value, 100),
                Height = LegacyUnitNormalizer.ConvertToPoints(img.Element(ns + "Height")?.Value, 100),
                ImageSource = img.Element(ns + "Value")?.Value ?? img.Element(ns + "Source")?.Value
            };
            elements.Add(elem);
        }

        // Lines & Shapes
        foreach (var line in itemsContainer.Elements(ns + "Line"))
        {
            var elem = new ElementDefinition
            {
                Id = line.Attribute("Name")?.Value,
                Type = ElementType.Shape,
                ShapeType = ShapeType.Line,
                X = LegacyUnitNormalizer.ConvertToPoints(line.Element(ns + "Left")?.Value, 0),
                Y = LegacyUnitNormalizer.ConvertToPoints(line.Element(ns + "Top")?.Value, 0),
                Width = LegacyUnitNormalizer.ConvertToPoints(line.Element(ns + "Width")?.Value, 100),
                Height = LegacyUnitNormalizer.ConvertToPoints(line.Element(ns + "Height")?.Value, 1)
            };
            elements.Add(elem);
        }

        return elements;
    }

    private static StyleDefinition ParseStyle(XElement? styleElem, XNamespace ns)
    {
        var style = new StyleDefinition();
        if (styleElem == null) return style;

        var fontFam = styleElem.Element(ns + "FontFamily")?.Value;
        if (!string.IsNullOrEmpty(fontFam)) style.FontFamily = fontFam;

        var fontSize = styleElem.Element(ns + "FontSize")?.Value;
        if (!string.IsNullOrEmpty(fontSize)) style.FontSize = LegacyUnitNormalizer.ConvertToPoints(fontSize, 10.0);

        var fontWeight = styleElem.Element(ns + "FontWeight")?.Value;
        if (!string.IsNullOrEmpty(fontWeight)) style.FontWeight = fontWeight;

        var color = styleElem.Element(ns + "Color")?.Value;
        if (!string.IsNullOrEmpty(color)) style.Color = color;

        var align = styleElem.Element(ns + "TextAlign")?.Value;
        if (!string.IsNullOrEmpty(align) && Enum.TryParse<HorizontalAlign>(align, true, out var hAlign))
        {
            style.Align = hAlign;
        }

        return style;
    }

    private static ParameterType MapParameterType(string ssrsType)
    {
        return ssrsType.ToLowerInvariant() switch
        {
            "integer" or "float" => ParameterType.Number,
            "datetime" => ParameterType.DateTime,
            "boolean" => ParameterType.Boolean,
            _ => ParameterType.String
        };
    }
}
