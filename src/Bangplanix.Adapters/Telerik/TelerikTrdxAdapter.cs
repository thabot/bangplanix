using System.IO.Compression;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Telerik;

public sealed class TelerikTrdxAdapter : ILegacyReportAdapter
{
    public string FormatName => "Telerik Reporting (TRDX/TRDP)";
    public IReadOnlyList<string> SupportedExtensions => [".trdx", ".trdp"];

    public ReportDefinition Convert(Stream stream)
    {
        if (IsZipStream(stream))
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var entry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".trdx", StringComparison.OrdinalIgnoreCase) ||
                                                           e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) && !e.FullName.StartsWith("["))
                        ?? archive.Entries.FirstOrDefault(e => !e.FullName.StartsWith("["));

            if (entry == null) throw new InvalidOperationException("No valid report definition found in TRDP package.");
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            return ConvertXml(reader.ReadToEnd());
        }

        using var txtReader = new StreamReader(stream);
        return ConvertXml(txtReader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return ConvertXml(content);
    }

    private static bool IsZipStream(Stream stream)
    {
        if (!stream.CanSeek || stream.Length < 4) return false;
        var pos = stream.Position;
        var header = new byte[4];
        _ = stream.Read(header, 0, 4);
        stream.Position = pos;
        return header[0] == 0x50 && header[1] == 0x4B && (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07);
    }

    private static ReportDefinition ConvertXml(string xml)
    {
        var sanitized = LegacyScriptSanitizer.Sanitize(xml);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid TRDX XML: missing root.");
        var ns = root.Name.Namespace;

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("Name")?.Value ?? "Telerik Report",
                Author = "Telerik Migration"
            }
        };

        var pageSettings = root.Element(ns + "PageSettings");
        if (pageSettings != null)
        {
            var pSize = pageSettings.Element(ns + "PageSize") ?? pageSettings;
            var pw = pSize.Attribute("Width")?.Value;
            var ph = pSize.Attribute("Height")?.Value;
            if (!string.IsNullOrEmpty(pw)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(pw, 595);
            if (!string.IsNullOrEmpty(ph)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(ph, 842);

            var margins = pageSettings.Element(ns + "Margins");
            if (margins != null)
            {
                var l = margins.Attribute("Left")?.Value;
                var r = margins.Attribute("Right")?.Value;
                var t = margins.Attribute("Top")?.Value;
                var b = margins.Attribute("Bottom")?.Value;

                report.PageSetup.Margins = new MarginDefinition
                {
                    Left = LegacyUnitNormalizer.ConvertToPoints(l, 28.35),
                    Right = LegacyUnitNormalizer.ConvertToPoints(r, 28.35),
                    Top = LegacyUnitNormalizer.ConvertToPoints(t, 28.35),
                    Bottom = LegacyUnitNormalizer.ConvertToPoints(b, 28.35)
                };
            }
        }

        var reportParams = root.Element(ns + "ReportParameters");
        if (reportParams != null)
        {
            foreach (var p in reportParams.Elements(ns + "ReportParameter"))
            {
                var pName = p.Attribute("Name")?.Value ?? string.Empty;
                var pType = p.Attribute("Type")?.Value ?? "String";
                var prompt = p.Attribute("Text")?.Value ?? pName;

                var paramDef = new ParameterDefinition
                {
                    Name = pName,
                    Label = prompt,
                    Type = pType.ToUpperInvariant() switch
                    {
                        "INTEGER" or "FLOAT" => ParameterType.Number,
                        "DATETIME" => ParameterType.DateTime,
                        "BOOLEAN" => ParameterType.Boolean,
                        _ => ParameterType.String
                    }
                };

                var valElem = p.Element(ns + "Value") ?? p.Element(ns + "ValueExpression");
                if (valElem != null)
                {
                    paramDef.DefaultValue = valElem.Value.Trim('=', '"');
                }

                report.Parameters.Add(paramDef);
            }
        }

        var dataSources = root.Element(ns + "DataSources");
        if (dataSources != null)
        {
            foreach (var ds in dataSources.Elements())
            {
                var name = ds.Attribute("Name")?.Value ?? "SqlDataSource1";
                var sql = ds.Element(ns + "SelectCommand")?.Value ?? ds.Attribute("SelectCommand")?.Value ?? string.Empty;

                report.Datasets.Add(new DatasetDefinition
                {
                    Name = name,
                    Type = DatasetType.Sql,
                    QueryOrUrl = sql
                });
            }
        }

        var items = root.Element(ns + "Items");
        if (items != null)
        {
            foreach (var section in items.Elements())
            {
                var sectionType = section.Name.LocalName;
                if (sectionType.Contains("PageHeaderSection", StringComparison.OrdinalIgnoreCase) || sectionType.Contains("HeaderSection", StringComparison.OrdinalIgnoreCase))
                {
                    report.Bands.PageHeader = ParseTelerikSection(section, ns);
                }
                else if (sectionType.Contains("DetailSection", StringComparison.OrdinalIgnoreCase))
                {
                    report.Bands.Detail = ParseTelerikSection(section, ns);
                }
                else if (sectionType.Contains("PageFooterSection", StringComparison.OrdinalIgnoreCase) || sectionType.Contains("FooterSection", StringComparison.OrdinalIgnoreCase))
                {
                    report.Bands.PageFooter = ParseTelerikSection(section, ns);
                }
            }
        }

        return report;
    }

    private static BandDefinition ParseTelerikSection(XElement section, XNamespace ns)
    {
        var band = new BandDefinition
        {
            Height = LegacyUnitNormalizer.ConvertToPoints(section.Attribute("Height")?.Value, 40)
        };

        var items = section.Element(ns + "Items");
        if (items != null)
        {
            foreach (var item in items.Elements())
            {
                var elem = new ElementDefinition
                {
                    Id = item.Attribute("Name")?.Value,
                    Type = ElementType.Text,
                    X = LegacyUnitNormalizer.ConvertToPoints(item.Attribute("Location")?.Value?.Split(',').FirstOrDefault()?.Trim(), 0),
                    Y = LegacyUnitNormalizer.ConvertToPoints(item.Attribute("Location")?.Value?.Split(',').Skip(1).FirstOrDefault()?.Trim(), 0),
                    Width = LegacyUnitNormalizer.ConvertToPoints(item.Attribute("Size")?.Value?.Split(',').FirstOrDefault()?.Trim(), 100),
                    Height = LegacyUnitNormalizer.ConvertToPoints(item.Attribute("Size")?.Value?.Split(',').Skip(1).FirstOrDefault()?.Trim(), 20)
                };

                var val = item.Attribute("Value")?.Value ?? item.Element(ns + "Value")?.Value;
                if (!string.IsNullOrEmpty(val))
                {
                    if (val.StartsWith('='))
                    {
                        elem.Expression = LegacyExpressionTranspiler.Transpile(val, "TELERIK");
                    }
                    else
                    {
                        elem.Text = val;
                    }
                }

                band.Elements.Add(elem);
            }
        }

        return band;
    }
}
