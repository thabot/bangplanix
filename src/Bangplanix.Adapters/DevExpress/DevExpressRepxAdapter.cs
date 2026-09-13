using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.DevExpress;

public sealed class DevExpressRepxAdapter : ILegacyReportAdapter
{
    public string FormatName => "DevExpress XtraReports (REPX)";
    public IReadOnlyList<string> SupportedExtensions => [".repx"];

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
        var root = doc.Root ?? throw new InvalidOperationException("Invalid REPX XML: missing root element.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("Name")?.Value ?? "DevExpress Report",
                Author = "DevExpress Migration"
            }
        };

        var pWidth = root.Attribute("PageWidth")?.Value;
        var pHeight = root.Attribute("PageHeight")?.Value;
        if (!string.IsNullOrEmpty(pWidth)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(pWidth, 595);
        if (!string.IsNullOrEmpty(pHeight)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(pHeight, 842);

        var margins = root.Attribute("Margins")?.Value;
        if (!string.IsNullOrEmpty(margins))
        {
            var parts = margins.Split(',');
            if (parts.Length >= 4)
            {
                report.PageSetup.Margins = new MarginDefinition
                {
                    Left = LegacyUnitNormalizer.ConvertToPoints(parts[0].Trim(), 28.35),
                    Right = LegacyUnitNormalizer.ConvertToPoints(parts[1].Trim(), 28.35),
                    Top = LegacyUnitNormalizer.ConvertToPoints(parts[2].Trim(), 28.35),
                    Bottom = LegacyUnitNormalizer.ConvertToPoints(parts[3].Trim(), 28.35)
                };
            }
        }

        var paramsElem = root.Element("Parameters");
        if (paramsElem != null)
        {
            foreach (var p in paramsElem.Elements())
            {
                var pName = p.Attribute("Name")?.Value ?? string.Empty;
                var desc = p.Attribute("Description")?.Value ?? pName;
                var val = p.Attribute("Value")?.Value;

                report.Parameters.Add(new ParameterDefinition
                {
                    Name = pName,
                    Label = desc,
                    DefaultValue = val
                });
            }
        }

        var bandsElem = root.Element("Bands");
        if (bandsElem != null)
        {
            foreach (var band in bandsElem.Elements())
            {
                var bandType = band.Attribute("Name")?.Value ?? band.Attribute("ControlType")?.Value ?? band.Name.LocalName;

                if (bandType.Contains("PageHeader", StringComparison.OrdinalIgnoreCase) || bandType.Contains("TopMargin", StringComparison.OrdinalIgnoreCase) || bandType.Contains("ReportHeader", StringComparison.OrdinalIgnoreCase))
                {
                    report.Bands.PageHeader = ParseDevExpressBand(band);
                }
                else if (bandType.Contains("Detail", StringComparison.OrdinalIgnoreCase))
                {
                    report.Bands.Detail = ParseDevExpressBand(band);
                }
                else if (bandType.Contains("PageFooter", StringComparison.OrdinalIgnoreCase) || bandType.Contains("BottomMargin", StringComparison.OrdinalIgnoreCase) || bandType.Contains("ReportFooter", StringComparison.OrdinalIgnoreCase))
                {
                    report.Bands.PageFooter = ParseDevExpressBand(band);
                }
            }
        }

        return report;
    }

    private static BandDefinition ParseDevExpressBand(XElement bandElem)
    {
        var band = new BandDefinition
        {
            Height = LegacyUnitNormalizer.ConvertToPoints(bandElem.Attribute("HeightF")?.Value ?? bandElem.Attribute("Height")?.Value, 40)
        };

        var controlsElem = bandElem.Element("Controls");
        if (controlsElem != null)
        {
            foreach (var ctrl in controlsElem.Elements())
            {
                var elem = new ElementDefinition
                {
                    Id = ctrl.Attribute("Name")?.Value,
                    Type = ElementType.Text
                };

                var locF = ctrl.Attribute("LocationFloat")?.Value ?? ctrl.Attribute("Location")?.Value;
                if (!string.IsNullOrEmpty(locF))
                {
                    var parts = locF.Split(',');
                    if (parts.Length >= 2)
                    {
                        elem.X = LegacyUnitNormalizer.ConvertToPoints(parts[0].Trim(), 0);
                        elem.Y = LegacyUnitNormalizer.ConvertToPoints(parts[1].Trim(), 0);
                    }
                }

                var sizeF = ctrl.Attribute("SizeF")?.Value ?? ctrl.Attribute("Size")?.Value;
                if (!string.IsNullOrEmpty(sizeF))
                {
                    var parts = sizeF.Split(',');
                    if (parts.Length >= 2)
                    {
                        elem.Width = LegacyUnitNormalizer.ConvertToPoints(parts[0].Trim(), 100);
                        elem.Height = LegacyUnitNormalizer.ConvertToPoints(parts[1].Trim(), 20);
                    }
                }

                var text = ctrl.Attribute("Text")?.Value;
                var exprBindings = ctrl.Element("ExpressionBindings");
                var textBinding = exprBindings?.Elements()?.FirstOrDefault(e => e.Attribute("PropertyName")?.Value == "Text")?.Attribute("Expression")?.Value;

                if (!string.IsNullOrEmpty(textBinding))
                {
                    elem.Expression = LegacyExpressionTranspiler.Transpile(textBinding, "DEVEXPRESS");
                }
                else if (!string.IsNullOrEmpty(text))
                {
                    if (text.StartsWith('['))
                    {
                        elem.Expression = LegacyExpressionTranspiler.Transpile(text, "DEVEXPRESS");
                    }
                    else
                    {
                        elem.Text = text;
                    }
                }

                band.Elements.Add(elem);
            }
        }

        return band;
    }
}
