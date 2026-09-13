using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Oracle;

public sealed class OracleReportsAdapter : ILegacyReportAdapter
{
    public string FormatName => "Oracle Reports (REX) & BI Publisher";
    public IReadOnlyList<string> SupportedExtensions => [".rex", ".xdo", ".oracle.xml", ".rtf.xml"];

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var doc = XDocument.Parse(content);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid Oracle Reports XML.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("name")?.Value ?? root.Element("name")?.Value ?? "Oracle Report",
                Author = "Oracle Reports Converter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Orientation = PageOrientation.Portrait
            }
        };

        // 1. Parameters (from <parameters> or <dataTemplate>/<parameters>)
        var paramElements = root.Descendants("parameter").Concat(root.Descendants("userParameter"));
        foreach (var p in paramElements)
        {
            string name = p.Attribute("name")?.Value ?? $"param_{report.Parameters.Count + 1}";
            string dt = p.Attribute("dataType")?.Value ?? p.Attribute("type")?.Value ?? "character";
            string prompt = p.Element("prompt")?.Value ?? p.Attribute("prompt")?.Value ?? name;

            report.Parameters.Add(new ParameterDefinition
            {
                Name = name,
                Label = prompt,
                Type = MapOracleParamType(dt)
            });
        }

        // 2. Data Queries (from <dataQuery>/<sqlStatement> or <query>)
        var queries = root.Descendants("sqlStatement").Concat(root.Descendants("query"));
        foreach (var q in queries)
        {
            string name = q.Attribute("name")?.Value ?? $"Query_{report.Datasets.Count + 1}";
            string sql = q.Value?.Trim() ?? string.Empty;

            report.Datasets.Add(new DatasetDefinition
            {
                Name = name,
                QueryOrUrl = sql
            });
        }

        // 3. Layout Frames and Fields
        var detailElements = new List<ElementDefinition>();
        var headerElements = new List<ElementDefinition>();

        var fields = root.Descendants("field").Concat(root.Descendants("displayItem"));
        double x = 10;
        double y = 10;

        foreach (var f in fields)
        {
            string fName = f.Attribute("name")?.Value ?? $"fld_{detailElements.Count + 1}";
            string source = f.Attribute("source")?.Value ?? f.Element("source")?.Value ?? f.Value;

            detailElements.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                Id = fName,
                X = x,
                Y = y,
                Width = 120,
                Height = 20,
                Expression = LegacyExpressionTranspiler.Transpile(source, "ORACLE")
            });
            x += 130;
            if (x > 500)
            {
                x = 10;
                y += 25;
            }
        }

        // Header boilerplate texts
        var texts = root.Descendants("text").Concat(root.Descendants("boilerplate"));
        double hx = 10;
        foreach (var t in texts)
        {
            string tVal = t.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(tVal)) continue;

            headerElements.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                Id = t.Attribute("name")?.Value ?? $"lbl_{headerElements.Count + 1}",
                X = hx,
                Y = 10,
                Width = 140,
                Height = 25,
                Text = tVal,
                Style = new StyleDefinition { FontWeight = "bold", FontSize = 12 }
            });
            hx += 150;
        }

        if (headerElements.Count > 0)
        {
            report.Bands.ReportHeader = new BandDefinition
            {
                Height = 45,
                Elements = headerElements
            };
        }

        report.Bands.Detail = new BandDefinition
        {
            Height = Math.Max(40, y + 30),
            Elements = detailElements
        };

        return report;
    }

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    private static ParameterType MapOracleParamType(string dt) => dt.ToUpperInvariant() switch
    {
        "NUMBER" or "NUMERIC" or "INTEGER" => ParameterType.Number,
        "DATE" or "DATETIME" => ParameterType.DateTime,
        "BOOLEAN" => ParameterType.Boolean,
        _ => ParameterType.String
    };
}