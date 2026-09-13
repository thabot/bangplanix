using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Birt;

public sealed class EclipseBirtAdapter : ILegacyReportAdapter
{
    private static readonly XNamespace BirtNs = "http://www.eclipse.org/birt/2005/design";

    public string FormatName => "Eclipse BIRT Report Design";
    public IReadOnlyList<string> SupportedExtensions => [".rptdesign", ".birt.xml"];

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var doc = XDocument.Parse(content);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid BIRT .rptdesign XML.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = GetPropertyValue(root, "title") ?? "BIRT Report",
                Author = GetPropertyValue(root, "author") ?? "Eclipse BIRT Converter"
            }
        };

        // 1. Page Setup
        var masterPage = root.Descendants(BirtNs + "simple-master-page").FirstOrDefault() ??
                         root.Descendants("simple-master-page").FirstOrDefault();
        if (masterPage != null)
        {
            string orientation = GetPropertyValue(masterPage, "orientation") ?? "portrait";
            string type = GetPropertyValue(masterPage, "type") ?? "a4";

            report.PageSetup = new PageSetup
            {
                Orientation = orientation.Equals("landscape", StringComparison.OrdinalIgnoreCase)
                    ? PageOrientation.Landscape
                    : PageOrientation.Portrait,
                PaperKind = MapBirtPaperKind(type)
            };
        }

        // 2. Parameters
        var parameters = root.Descendants(BirtNs + "scalar-parameter").Concat(root.Descendants("scalar-parameter"));
        foreach (var p in parameters)
        {
            string name = p.Attribute("name")?.Value ?? $"param_{report.Parameters.Count + 1}";
            string prompt = GetPropertyValue(p, "promptText") ?? name;
            string dataType = p.Attribute("dataType")?.Value ?? GetPropertyValue(p, "dataType") ?? "string";

            report.Parameters.Add(new ParameterDefinition
            {
                Name = name,
                Label = prompt,
                Type = MapBirtParamType(dataType)
            });
        }

        // 3. Data Sets
        var dataSets = root.Descendants(BirtNs + "oda-data-set").Concat(root.Descendants("oda-data-set"));
        foreach (var ds in dataSets)
        {
            string name = ds.Attribute("name")?.Value ?? $"DataSet_{report.Datasets.Count + 1}";
            string qry = GetPropertyValue(ds, "queryText") ?? $"SELECT * FROM {name}";

            report.Datasets.Add(new DatasetDefinition
            {
                Name = name,
                QueryOrUrl = qry
            });
        }

        // 4. Layout Elements from Body
        var body = root.Element(BirtNs + "body") ?? root.Element("body");
        if (body != null)
        {
            ProcessBirtBody(body, report);
        }

        return report;
    }

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    private static void ProcessBirtBody(XElement body, ReportDefinition report)
    {
        var tables = body.Descendants(BirtNs + "table").Concat(body.Descendants("table")).ToList();

        if (tables.Count > 0)
        {
            var tbl = tables[0];

            // Header -> PageHeader / ReportHeader
            var headerEl = tbl.Element(BirtNs + "header") ?? tbl.Element("header");
            if (headerEl != null)
            {
                var elements = new List<ElementDefinition>();
                ExtractBirtElements(headerEl, elements);
                report.Bands.PageHeader = new BandDefinition { Height = 40, Elements = elements };
            }

            // Detail -> Detail Band
            var detailEl = tbl.Element(BirtNs + "detail") ?? tbl.Element("detail");
            if (detailEl != null)
            {
                var elements = new List<ElementDefinition>();
                ExtractBirtElements(detailEl, elements);
                report.Bands.Detail = new BandDefinition { Height = 30, Elements = elements };
            }

            // Footer -> PageFooter / ReportFooter
            var footerEl = tbl.Element(BirtNs + "footer") ?? tbl.Element("footer");
            if (footerEl != null)
            {
                var elements = new List<ElementDefinition>();
                ExtractBirtElements(footerEl, elements);
                report.Bands.PageFooter = new BandDefinition { Height = 30, Elements = elements };
            }
        }
        else
        {
            // Free-form body items
            var elements = new List<ElementDefinition>();
            ExtractBirtElements(body, elements);
            report.Bands.Detail = new BandDefinition { Height = 200, Elements = elements };
        }
    }

    private static void ExtractBirtElements(XElement container, List<ElementDefinition> elements)
    {
        double currentX = 10;
        double currentY = 5;

        foreach (var cell in container.Descendants(BirtNs + "cell").Concat(container.Descendants("cell")))
        {
            foreach (var item in cell.Elements())
            {
                string localName = item.Name.LocalName;

                if (localName is "label" or "text")
                {
                    string text = GetPropertyValue(item, "text") ?? item.Value;
                    elements.Add(new ElementDefinition
                    {
                        Type = ElementType.Text,
                        Id = item.Attribute("name")?.Value ?? $"lbl_{elements.Count + 1}",
                        X = currentX,
                        Y = currentY,
                        Width = 120,
                        Height = 20,
                        Text = text
                    });
                    currentX += 130;
                }
                else if (localName is "data")
                {
                    string? expr = GetExpressionValue(item, "resultSetColumn") ??
                                   GetExpressionValue(item, "valueExpr") ?? item.Value;

                    elements.Add(new ElementDefinition
                    {
                        Type = ElementType.Text,
                        Id = item.Attribute("name")?.Value ?? $"data_{elements.Count + 1}",
                        X = currentX,
                        Y = currentY,
                        Width = 120,
                        Height = 20,
                        Expression = LegacyExpressionTranspiler.Transpile(expr, "BIRT")
                    });
                    currentX += 130;
                }
                else if (localName is "image")
                {
                    string src = GetPropertyValue(item, "uri") ?? GetPropertyValue(item, "imageName") ?? "image.png";
                    elements.Add(new ElementDefinition
                    {
                        Type = ElementType.Image,
                        Id = item.Attribute("name")?.Value ?? $"img_{elements.Count + 1}",
                        X = currentX,
                        Y = currentY,
                        Width = 100,
                        Height = 50,
                        Text = src
                    });
                    currentX += 110;
                }
            }
        }
    }

    private static string? GetPropertyValue(XElement el, string propName)
    {
        return el.Elements()
            .FirstOrDefault(e => (e.Name.LocalName == "property" || e.Name.LocalName == "text-property") &&
                                 e.Attribute("name")?.Value == propName)?.Value;
    }

    private static string? GetExpressionValue(XElement el, string exprName)
    {
        return el.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "expression" && e.Attribute("name")?.Value == exprName)?.Value;
    }

    private static ParameterType MapBirtParamType(string type) => type.ToUpperInvariant() switch
    {
        "INTEGER" or "DECIMAL" or "FLOAT" => ParameterType.Number,
        "BOOLEAN" => ParameterType.Boolean,
        "DATE" or "TIME" or "DATE-TIME" => ParameterType.DateTime,
        _ => ParameterType.String
    };

    private static PaperKind MapBirtPaperKind(string type) => type.ToUpperInvariant() switch
    {
        "A4" => PaperKind.A4,
        "A3" => PaperKind.A3,
        "A5" => PaperKind.A5,
        "LETTER" => PaperKind.Letter,
        "LEGAL" => PaperKind.Legal,
        _ => PaperKind.A4
    };
}