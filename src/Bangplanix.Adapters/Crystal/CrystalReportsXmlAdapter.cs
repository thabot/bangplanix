using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Crystal;

public sealed class CrystalReportsXmlAdapter : ILegacyReportAdapter
{
    public string FormatName => "SAP Crystal Reports XML";
    public IReadOnlyList<string> SupportedExtensions => [".rpt.xml", ".crystal.xml", ".rptxml", ".rpt"];

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var doc = XDocument.Parse(content);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid Crystal Reports XML structure.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Element("SummaryInfo")?.Element("Title")?.Value ??
                        root.Attribute("Title")?.Value ??
                        root.Attribute("Name")?.Value ?? "Crystal Report",
                Author = root.Element("SummaryInfo")?.Element("Author")?.Value ?? "Crystal Reports Converter"
            }
        };

        // 1. Page Setup & Twips conversion (1 pt = 20 twips)
        var pageSetupEl = root.Element("PageSetup") ?? root.Element("PageMargins");
        if (pageSetupEl != null)
        {
            double w = ParseTwipsOrPoints(pageSetupEl.Element("PaperWidth")?.Value ?? pageSetupEl.Attribute("PaperWidth")?.Value, 595.28);
            double h = ParseTwipsOrPoints(pageSetupEl.Element("PaperHeight")?.Value ?? pageSetupEl.Attribute("PaperHeight")?.Value, 841.89);
            string orientation = pageSetupEl.Element("Orientation")?.Value ?? pageSetupEl.Attribute("Orientation")?.Value ?? "Portrait";

            report.PageSetup = new PageSetup
            {
                Width = w,
                Height = h,
                Orientation = orientation.Equals("Landscape", StringComparison.OrdinalIgnoreCase)
                    ? PageOrientation.Landscape
                    : PageOrientation.Portrait
            };
        }

        // 2. Parameters
        var paramFields = root.Descendants("ParameterField").Concat(root.Descendants("Parameter"));
        foreach (var p in paramFields)
        {
            string name = p.Attribute("Name")?.Value ?? p.Element("Name")?.Value ?? $"param_{report.Parameters.Count + 1}";
            string prompt = p.Element("PromptText")?.Value ?? p.Attribute("PromptText")?.Value ?? name;
            string valType = p.Attribute("ValueType")?.Value ?? p.Element("ValueType")?.Value ?? "String";

            report.Parameters.Add(new ParameterDefinition
            {
                Name = name.TrimStart('?'),
                Label = prompt,
                Type = MapCrystalParamType(valType)
            });
        }

        // 3. Datasets / Tables
        var tables = root.Descendants("Table").Concat(root.Descendants("DatabaseTable"));
        foreach (var t in tables)
        {
            string tName = t.Attribute("Name")?.Value ?? t.Element("Name")?.Value ?? "MainTable";
            string qry = t.Element("CommandText")?.Value ?? t.Element("SelectQuery")?.Value ?? $"SELECT * FROM {tName}";

            report.Datasets.Add(new DatasetDefinition
            {
                Name = tName,
                QueryOrUrl = qry
            });
        }

        // 4. Sections & Bands
        var sections = root.Descendants("Section").Concat(root.Descendants("Area"));
        foreach (var sec in sections)
        {
            string kind = sec.Attribute("Kind")?.Value ?? sec.Attribute("SectionType")?.Value ?? sec.Name.LocalName;
            double height = ParseTwipsOrPoints(sec.Attribute("Height")?.Value ?? sec.Element("Height")?.Value, 40.0);

            var elements = new List<ElementDefinition>();
            ExtractSectionElements(sec, elements);

            var band = new BandDefinition
            {
                Height = height,
                Elements = elements
            };

            if (kind.Contains("ReportHeader", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.ReportHeader = band;
            }
            else if (kind.Contains("PageHeader", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.PageHeader = band;
            }
            else if (kind.Contains("GroupHeader", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.GroupHeaders.Add(new GroupBandDefinition
                {
                    Height = height,
                    GroupBy = sec.Attribute("GroupBy")?.Value ?? string.Empty,
                    Elements = elements
                });
            }
            else if (kind.Contains("Detail", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.Detail = band;
            }
            else if (kind.Contains("GroupFooter", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.GroupFooters.Add(new GroupBandDefinition
                {
                    Height = height,
                    Elements = elements
                });
            }
            else if (kind.Contains("PageFooter", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.PageFooter = band;
            }
            else if (kind.Contains("ReportFooter", StringComparison.OrdinalIgnoreCase))
            {
                report.Bands.ReportFooter = band;
            }
            else if (report.Bands.Detail == null)
            {
                report.Bands.Detail = band;
            }
        }

        return report;
    }

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Check if stream is seekable to sniff binary vs XML
        if (stream.CanSeek)
        {
            long startPos = stream.Position;
            byte[] header = new byte[8];
            int read = stream.Read(header, 0, header.Length);
            stream.Position = startPos;

            // OLE Compound File Header magic bytes: 0xD0 0xCF 0x11 0xE0
            bool isBinaryRpt = read >= 4 && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0;
            if (isBinaryRpt)
            {
                var workerClient = new CrystalWorkerClient();
                return workerClient.ConvertRptStream(stream);
            }
        }

        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    private static void ExtractSectionElements(XElement sec, List<ElementDefinition> elements)
    {
        var objects = sec.Elements();
        foreach (var obj in objects)
        {
            string name = obj.Name.LocalName;
            double left = ParseTwipsOrPoints(obj.Attribute("Left")?.Value ?? obj.Element("Left")?.Value, 0.0);
            double top = ParseTwipsOrPoints(obj.Attribute("Top")?.Value ?? obj.Element("Top")?.Value, 0.0);
            double width = ParseTwipsOrPoints(obj.Attribute("Width")?.Value ?? obj.Element("Width")?.Value, 100.0);
            double height = ParseTwipsOrPoints(obj.Attribute("Height")?.Value ?? obj.Element("Height")?.Value, 20.0);

            if (name.Contains("Text", StringComparison.OrdinalIgnoreCase) || name.Contains("Field", StringComparison.OrdinalIgnoreCase))
            {
                string text = obj.Element("Text")?.Value ?? obj.Attribute("Text")?.Value ?? obj.Element("Value")?.Value ?? obj.Value;
                bool isExpr = text.StartsWith('{') || text.StartsWith('=') || name.Contains("Formula", StringComparison.OrdinalIgnoreCase);

                var el = new ElementDefinition
                {
                    Type = ElementType.Text,
                    Id = obj.Attribute("Name")?.Value ?? $"txt_{elements.Count + 1}",
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height,
                    Text = isExpr ? null : text,
                    Expression = isExpr ? LegacyExpressionTranspiler.Transpile(text, "CRYSTAL") : null,
                    Style = new StyleDefinition
                    {
                        FontSize = ParseFontSize(obj),
                        FontWeight = obj.Element("Font")?.Attribute("Bold")?.Value == "true" ? "bold" : "normal",
                        Color = obj.Element("Color")?.Value ?? "#000000"
                    }
                };
                elements.Add(el);
            }
            else if (name.Contains("Picture", StringComparison.OrdinalIgnoreCase) || name.Contains("Blob", StringComparison.OrdinalIgnoreCase))
            {
                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Image,
                    Id = obj.Attribute("Name")?.Value ?? $"img_{elements.Count + 1}",
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height,
                    Text = obj.Element("ImageSource")?.Value ?? obj.Attribute("Source")?.Value
                });
            }
            else if (name.Contains("Box", StringComparison.OrdinalIgnoreCase) || name.Contains("Line", StringComparison.OrdinalIgnoreCase))
            {
                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Shape,
                    Id = obj.Attribute("Name")?.Value ?? $"shape_{elements.Count + 1}",
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height
                });
            }
        }
    }

    private static double ParseTwipsOrPoints(string? val, double fallback)
    {
        if (string.IsNullOrWhiteSpace(val)) return fallback;
        if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
        {
            // If value > 300, it's almost certainly twips (1 pt = 20 twips)
            return num > 300 ? Math.Round(num / 20.0, 2) : num;
        }
        return fallback;
    }

    private static double ParseFontSize(XElement obj)
    {
        var fontEl = obj.Element("Font");
        if (fontEl != null && double.TryParse(fontEl.Attribute("Size")?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double sz))
        {
            return sz;
        }
        return 10.0;
    }

    private static ParameterType MapCrystalParamType(string valType) => valType.ToUpperInvariant() switch
    {
        "NUMBER" or "NUMERIC" or "CURRENCY" or "INTEGER" => ParameterType.Number,
        "BOOLEAN" => ParameterType.Boolean,
        "DATE" or "DATETIME" or "TIME" => ParameterType.DateTime,
        _ => ParameterType.String
    };
}