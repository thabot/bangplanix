using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Cognos;

public sealed class CognosSpecAdapter : ILegacyReportAdapter
{
    public string FormatName => "IBM Cognos Analytics";
    public IReadOnlyList<string> SupportedExtensions => [".spec", ".cognos.xml"];

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
        var root = doc.Root ?? throw new InvalidOperationException("Invalid IBM Cognos XML specification.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Element("name")?.Value ?? root.Attribute("name")?.Value ?? "Cognos Report",
                Author = "IBM Cognos Converter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Orientation = PageOrientation.Portrait,
                Width = 595.28,
                Height = 841.89,
                Margins = new MarginDefinition { Left = 36, Right = 36, Top = 36, Bottom = 36 }
            }
        };

        var queries = root.Descendants().Where(e => e.Name.LocalName.Equals("query", StringComparison.OrdinalIgnoreCase));
        foreach (var q in queries)
        {
            var qName = q.Attribute("name")?.Value ?? $"Query_{report.Datasets.Count + 1}";
            var sqlNode = q.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("sqlText", StringComparison.OrdinalIgnoreCase));
            var sql = sqlNode?.Value?.Trim() ?? $"SELECT * FROM {qName}";

            var dataset = new DatasetDefinition
            {
                Name = qName,
                QueryOrUrl = sql
            };

            var dataItems = q.Descendants().Where(e => e.Name.LocalName.Equals("dataItem", StringComparison.OrdinalIgnoreCase));
            foreach (var di in dataItems)
            {
                var diName = di.Attribute("name")?.Value;
                var expr = di.Element("expression")?.Value ?? di.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("expression", StringComparison.OrdinalIgnoreCase))?.Value;
                if (!string.IsNullOrWhiteSpace(diName))
                {
                    dataset.CalculatedColumns.Add(new CalculatedColumnDefinition
                    {
                        Name = diName,
                        Expression = LegacyExpressionTranspiler.Transpile(expr ?? diName, "COGNOS")
                    });
                }
            }

            report.Datasets.Add(dataset);
        }

        var promptParams = root.Descendants().Where(e => e.Name.LocalName.Contains("prompt", StringComparison.OrdinalIgnoreCase) || e.Name.LocalName.Equals("parameter", StringComparison.OrdinalIgnoreCase));
        foreach (var p in promptParams)
        {
            var pName = p.Attribute("name")?.Value ?? p.Attribute("parameter")?.Value;
            if (!string.IsNullOrWhiteSpace(pName) && !report.Parameters.Any(x => x.Name.Equals(pName, StringComparison.OrdinalIgnoreCase)))
            {
                report.Parameters.Add(new ParameterDefinition
                {
                    Name = pName,
                    Label = pName,
                    Type = ParameterType.String
                });
            }
        }

        var detailBand = new BandDefinition { Height = 40 };
        var headerBand = new BandDefinition { Height = 40 };
        double y = 10;

        var listColumns = root.Descendants().Where(e => e.Name.LocalName.Equals("listColumn", StringComparison.OrdinalIgnoreCase) || e.Name.LocalName.Equals("crosstabNode", StringComparison.OrdinalIgnoreCase) || e.Name.LocalName.Equals("dataItemValue", StringComparison.OrdinalIgnoreCase));
        double x = 10;
        foreach (var col in listColumns)
        {
            var refItem = col.Attribute("refDataItem")?.Value ?? col.Descendants().FirstOrDefault(e => e.Name.LocalName == "dataItemValue")?.Attribute("refDataItem")?.Value ?? col.Value?.Trim();
            if (string.IsNullOrWhiteSpace(refItem)) continue;

            detailBand.Elements.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                X = x,
                Y = y,
                Width = 120,
                Height = 20,
                Expression = LegacyExpressionTranspiler.Transpile(refItem, "COGNOS")
            });

            headerBand.Elements.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                X = x,
                Y = 10,
                Width = 120,
                Height = 20,
                Text = refItem,
                Style = new StyleDefinition { FontWeight = "bold" }
            });

            x += 130;
            if (x > 500)
            {
                x = 10;
                y += 25;
            }
        }

        var textItems = root.Descendants().Where(e => e.Name.LocalName.Equals("textItem", StringComparison.OrdinalIgnoreCase) || e.Name.LocalName.Equals("staticValue", StringComparison.OrdinalIgnoreCase));
        foreach (var ti in textItems)
        {
            var txt = ti.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(txt))
            {
                headerBand.Elements.Add(new ElementDefinition
                {
                    Type = ElementType.Text,
                    X = 10,
                    Y = headerBand.Elements.Count * 22 + 10,
                    Width = 400,
                    Height = 20,
                    Text = txt,
                    Style = new StyleDefinition { FontSize = 14, FontWeight = "bold" }
                });
            }
        }

        if (headerBand.Elements.Count > 0)
        {
            headerBand.Height = Math.Max(40, headerBand.Elements.Max(e => e.Y + e.Height) + 10);
            report.Bands.ReportHeader = headerBand;
        }

        detailBand.Height = Math.Max(40, y + 30);
        report.Bands.Detail = detailBand;

        return report;
    }
}
