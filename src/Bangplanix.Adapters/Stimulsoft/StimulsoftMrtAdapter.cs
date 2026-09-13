using System.Text.Json;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Stimulsoft;

public sealed class StimulsoftMrtAdapter : ILegacyReportAdapter
{
    public string FormatName => "Stimulsoft Reports";
    public IReadOnlyList<string> SupportedExtensions => [".mrt"];

    public ReportDefinition Convert(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var trimmed = content.Trim();

        if (trimmed.StartsWith('{'))
        {
            return ConvertFromJson(trimmed);
        }
        else
        {
            return ConvertFromXml(trimmed);
        }
    }

    private static ReportDefinition ConvertFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.TryGetProperty("ReportName", out var rn) ? rn.GetString() ?? "Stimulsoft Report" : "Stimulsoft Report",
                Author = root.TryGetProperty("ReportAuthor", out var ra) ? ra.GetString() ?? "Stimulsoft Migration" : "Stimulsoft Migration"
            }
        };

        if (root.TryGetProperty("Pages", out var pages) && pages.ValueKind == JsonValueKind.Object)
        {
            foreach (var pageProp in pages.EnumerateObject())
            {
                var page = pageProp.Value;
                if (page.TryGetProperty("PageWidth", out var pw))
                {
                    report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(pw.GetRawText().Trim('"'), 595);
                }
                if (page.TryGetProperty("PageHeight", out var ph))
                {
                    report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(ph.GetRawText().Trim('"'), 842);
                }
                if (page.TryGetProperty("Margins", out var margins))
                {
                    var mParts = margins.GetString()?.Split(',') ?? [];
                    if (mParts.Length >= 4)
                    {
                        report.PageSetup.Margins = new MarginDefinition
                        {
                            Left = LegacyUnitNormalizer.ConvertToPoints(mParts[0].Trim(), 28.35),
                            Right = LegacyUnitNormalizer.ConvertToPoints(mParts[1].Trim(), 28.35),
                            Top = LegacyUnitNormalizer.ConvertToPoints(mParts[2].Trim(), 28.35),
                            Bottom = LegacyUnitNormalizer.ConvertToPoints(mParts[3].Trim(), 28.35)
                        };
                    }
                }

                if (page.TryGetProperty("Components", out var comps) && comps.ValueKind == JsonValueKind.Object)
                {
                    foreach (var compProp in comps.EnumerateObject())
                    {
                        var comp = compProp.Value;
                        var ident = comp.TryGetProperty("Ident", out var idElem) ? idElem.GetString() : string.Empty;

                        if (ident is "StiPageHeaderBand" or "StiHeaderBand")
                        {
                            report.Bands.PageHeader = ParseJsonBand(comp);
                        }
                        else if (ident is "StiDataBand" or "StiDetailBand")
                        {
                            report.Bands.Detail = ParseJsonBand(comp);
                        }
                        else if (ident is "StiPageFooterBand" or "StiFooterBand")
                        {
                            report.Bands.PageFooter = ParseJsonBand(comp);
                        }
                    }
                }
                break;
            }
        }

        if (root.TryGetProperty("Dictionary", out var dict))
        {
            if (dict.TryGetProperty("DataSources", out var dataSources) && dataSources.ValueKind == JsonValueKind.Object)
            {
                foreach (var dsProp in dataSources.EnumerateObject())
                {
                    var ds = dsProp.Value;
                    var name = ds.TryGetProperty("Name", out var n) ? n.GetString() ?? dsProp.Name : dsProp.Name;
                    var sql = ds.TryGetProperty("SqlCommand", out var s) ? s.GetString() ?? string.Empty : string.Empty;

                    report.Datasets.Add(new DatasetDefinition
                    {
                        Name = name,
                        Type = DatasetType.Sql,
                        QueryOrUrl = sql
                    });
                }
            }
        }

        return report;
    }

    private static BandDefinition ParseJsonBand(JsonElement bandElem)
    {
        var band = new BandDefinition();
        if (bandElem.TryGetProperty("ClientRectangle", out var rectElem))
        {
            var parts = rectElem.GetString()?.Split(',') ?? [];
            if (parts.Length >= 4)
            {
                band.Height = LegacyUnitNormalizer.ConvertToPoints(parts[3].Trim(), 40);
            }
        }

        if (bandElem.TryGetProperty("Components", out var comps) && comps.ValueKind == JsonValueKind.Object)
        {
            foreach (var cProp in comps.EnumerateObject())
            {
                var c = cProp.Value;
                var elem = new ElementDefinition { Type = ElementType.Text };

                if (c.TryGetProperty("Name", out var name)) elem.Id = name.GetString();
                if (c.TryGetProperty("ClientRectangle", out var cr))
                {
                    var parts = cr.GetString()?.Split(',') ?? [];
                    if (parts.Length >= 4)
                    {
                        elem.X = LegacyUnitNormalizer.ConvertToPoints(parts[0].Trim(), 0);
                        elem.Y = LegacyUnitNormalizer.ConvertToPoints(parts[1].Trim(), 0);
                        elem.Width = LegacyUnitNormalizer.ConvertToPoints(parts[2].Trim(), 100);
                        elem.Height = LegacyUnitNormalizer.ConvertToPoints(parts[3].Trim(), 20);
                    }
                }

                if (c.TryGetProperty("Text", out var textElem))
                {
                    var rawText = textElem.ValueKind == JsonValueKind.Object && textElem.TryGetProperty("Value", out var v)
                        ? v.GetString()
                        : textElem.GetString();
                    if (!string.IsNullOrEmpty(rawText))
                    {
                        if (rawText.Contains('{', StringComparison.Ordinal) && rawText.Contains('}', StringComparison.Ordinal))
                        {
                            elem.Expression = LegacyExpressionTranspiler.Transpile(rawText, "STIMULSOFT");
                        }
                        else
                        {
                            elem.Text = rawText;
                        }
                    }
                }

                band.Elements.Add(elem);
            }
        }

        return band;
    }

    private static ReportDefinition ConvertFromXml(string xml)
    {
        var sanitized = LegacyScriptSanitizer.Sanitize(xml);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid MRT XML: missing root.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Element("ReportName")?.Value ?? "Stimulsoft Report",
                Author = "Stimulsoft Migration"
            }
        };

        var page = root.Element("Pages")?.Elements().FirstOrDefault();
        if (page != null)
        {
            var pw = page.Element("PageWidth")?.Value;
            var ph = page.Element("PageHeight")?.Value;
            if (!string.IsNullOrEmpty(pw)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(pw, 595);
            if (!string.IsNullOrEmpty(ph)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(ph, 842);

            var comps = page.Element("Components")?.Elements();
            if (comps != null)
            {
                foreach (var c in comps)
                {
                    var typeName = c.Attribute("type")?.Value ?? c.Name.LocalName;
                    if (typeName.Contains("HeaderBand", StringComparison.OrdinalIgnoreCase))
                    {
                        report.Bands.PageHeader = ParseXmlBand(c);
                    }
                    else if (typeName.Contains("DataBand", StringComparison.OrdinalIgnoreCase) || typeName.Contains("DetailBand", StringComparison.OrdinalIgnoreCase))
                    {
                        report.Bands.Detail = ParseXmlBand(c);
                    }
                    else if (typeName.Contains("FooterBand", StringComparison.OrdinalIgnoreCase))
                    {
                        report.Bands.PageFooter = ParseXmlBand(c);
                    }
                }
            }
        }

        return report;
    }

    private static BandDefinition ParseXmlBand(XElement bandElem)
    {
        var band = new BandDefinition();
        var rect = bandElem.Element("ClientRectangle")?.Value;
        if (!string.IsNullOrEmpty(rect))
        {
            var parts = rect.Split(',');
            if (parts.Length >= 4)
            {
                band.Height = LegacyUnitNormalizer.ConvertToPoints(parts[3].Trim(), 40);
            }
        }

        var comps = bandElem.Element("Components")?.Elements();
        if (comps != null)
        {
            foreach (var c in comps)
            {
                var elem = new ElementDefinition
                {
                    Id = c.Attribute("name")?.Value ?? c.Element("Name")?.Value,
                    Type = ElementType.Text
                };

                var cRect = c.Element("ClientRectangle")?.Value;
                if (!string.IsNullOrEmpty(cRect))
                {
                    var parts = cRect.Split(',');
                    if (parts.Length >= 4)
                    {
                        elem.X = LegacyUnitNormalizer.ConvertToPoints(parts[0].Trim(), 0);
                        elem.Y = LegacyUnitNormalizer.ConvertToPoints(parts[1].Trim(), 0);
                        elem.Width = LegacyUnitNormalizer.ConvertToPoints(parts[2].Trim(), 100);
                        elem.Height = LegacyUnitNormalizer.ConvertToPoints(parts[3].Trim(), 20);
                    }
                }

                var textVal = c.Element("Text")?.Value;
                if (!string.IsNullOrEmpty(textVal))
                {
                    if (textVal.Contains('{', StringComparison.Ordinal) && textVal.Contains('}', StringComparison.Ordinal))
                    {
                        elem.Expression = LegacyExpressionTranspiler.Transpile(textVal, "STIMULSOFT");
                    }
                    else
                    {
                        elem.Text = textVal;
                    }
                }

                band.Elements.Add(elem);
            }
        }

        return band;
    }
}
