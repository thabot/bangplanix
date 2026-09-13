using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Adapters.Ssrs;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.ActiveReports;

public sealed class ActiveReportsAdapter : ILegacyReportAdapter
{
    private readonly SsrsRdlAdapter _rdlFallbackAdapter = new();

    public string FormatName => "ActiveReports (RPX & RDLX)";
    public IReadOnlyList<string> SupportedExtensions => [".rdlx", ".rpx", ".ar.xml"];

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var trimmed = content.Trim();

        // 1. If it's an RDLX (Page Report), delegate to RDL adapter logic
        if (trimmed.Contains("<Report ", StringComparison.OrdinalIgnoreCase) && 
           (trimmed.Contains("http://schemas.microsoft.com/sqlserver/reporting/", StringComparison.OrdinalIgnoreCase) || 
            trimmed.Contains("http://schemas.grapecity.com/activereports/", StringComparison.OrdinalIgnoreCase)))
        {
            return _rdlFallbackAdapter.Convert(content);
        }

        // 2. Otherwise parse ActiveReports Section Report (.rpx XML)
        var doc = XDocument.Parse(content);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid ActiveReports RPX layout XML.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("ReportName")?.Value ?? "ActiveReports Section Report",
                Author = "ActiveReports Converter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Orientation = PageOrientation.Portrait
            }
        };

        // Parse PageSettings / Margins
        var pageSettings = root.Element("PageSettings");
        if (pageSettings != null)
        {
            string orientation = pageSettings.Attribute("Orientation")?.Value ?? "Portrait";
            report.PageSetup.Orientation = orientation.Equals("Landscape", StringComparison.OrdinalIgnoreCase)
                ? PageOrientation.Landscape
                : PageOrientation.Portrait;
        }

        // Parse Parameters
        var paramElements = root.Descendants("Parameter");
        foreach (var p in paramElements)
        {
            string name = p.Attribute("Key")?.Value ?? p.Attribute("Name")?.Value ?? $"param_{report.Parameters.Count + 1}";
            string prompt = p.Attribute("Prompt")?.Value ?? name;

            report.Parameters.Add(new ParameterDefinition
            {
                Name = name,
                Label = prompt,
                Type = ParameterType.String
            });
        }

        // Parse Sections
        var sections = root.Descendants("Section");
        foreach (var sec in sections)
        {
            string secType = sec.Attribute("Type")?.Value ?? "Detail";
            double height = ParseArUnit(sec.Attribute("Height")?.Value, 30.0);

            var elements = new List<ElementDefinition>();
            ExtractArControls(sec, elements);

            var band = new BandDefinition
            {
                Height = height,
                Elements = elements
            };

            switch (secType.ToUpperInvariant())
            {
                case "REPORTHEADER":
                    report.Bands.ReportHeader = band;
                    break;
                case "PAGEHEADER":
                    report.Bands.PageHeader = band;
                    break;
                case "GROUPHEADER":
                    report.Bands.GroupHeaders.Add(new GroupBandDefinition
                    {
                        Height = height,
                        GroupBy = sec.Attribute("DataField")?.Value ?? string.Empty,
                        Elements = elements
                    });
                    break;
                case "DETAIL":
                    report.Bands.Detail = band;
                    break;
                case "GROUPFOOTER":
                    report.Bands.GroupFooters.Add(new GroupBandDefinition
                    {
                        Height = height,
                        Elements = elements
                    });
                    break;
                case "PAGEFOOTER":
                    report.Bands.PageFooter = band;
                    break;
                case "REPORTFOOTER":
                    report.Bands.ReportFooter = band;
                    break;
                default:
                    if (report.Bands.Detail == null)
                        report.Bands.Detail = band;
                    break;
            }
        }

        return report;
    }

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    private static void ExtractArControls(XElement sec, List<ElementDefinition> elements)
    {
        var controls = sec.Descendants("Control");
        foreach (var ctrl in controls)
        {
            string ctrlType = ctrl.Attribute("Type")?.Value ?? "AR.Label";
            string name = ctrl.Attribute("Name")?.Value ?? $"ctrl_{elements.Count + 1}";
            double left = ParseArUnit(ctrl.Attribute("Left")?.Value, 0.0);
            double top = ParseArUnit(ctrl.Attribute("Top")?.Value, 0.0);
            double width = ParseArUnit(ctrl.Attribute("Width")?.Value, 100.0);
            double height = ParseArUnit(ctrl.Attribute("Height")?.Value, 20.0);

            if (ctrlType.Contains("Label", StringComparison.OrdinalIgnoreCase))
            {
                string caption = ctrl.Attribute("Caption")?.Value ?? ctrl.Attribute("Text")?.Value ?? string.Empty;
                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Text,
                    Id = name,
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height,
                    Text = caption,
                    Style = ExtractArStyle(ctrl)
                });
            }
            else if (ctrlType.Contains("Field", StringComparison.OrdinalIgnoreCase) || ctrlType.Contains("TextBox", StringComparison.OrdinalIgnoreCase))
            {
                string dataField = ctrl.Attribute("DataField")?.Value ?? ctrl.Attribute("Text")?.Value ?? string.Empty;
                bool isExpr = dataField.StartsWith('=') || dataField.StartsWith('[');

                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Text,
                    Id = name,
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height,
                    Expression = isExpr ? LegacyExpressionTranspiler.Transpile(dataField, "ACTIVEREPORTS") : $"=Fields.{dataField}",
                    Style = ExtractArStyle(ctrl)
                });
            }
            else if (ctrlType.Contains("Image", StringComparison.OrdinalIgnoreCase) || ctrlType.Contains("Picture", StringComparison.OrdinalIgnoreCase))
            {
                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Image,
                    Id = name,
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height,
                    Text = ctrl.Attribute("ImageData")?.Value ?? ctrl.Attribute("Picture")?.Value
                });
            }
            else if (ctrlType.Contains("Barcode", StringComparison.OrdinalIgnoreCase))
            {
                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Barcode,
                    Id = name,
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height,
                    Text = ctrl.Attribute("Text")?.Value ?? "12345678"
                });
            }
            else if (ctrlType.Contains("Shape", StringComparison.OrdinalIgnoreCase) || ctrlType.Contains("Line", StringComparison.OrdinalIgnoreCase))
            {
                elements.Add(new ElementDefinition
                {
                    Type = ElementType.Shape,
                    Id = name,
                    X = left,
                    Y = top,
                    Width = width,
                    Height = height
                });
            }
        }
    }

    private static StyleDefinition ExtractArStyle(XElement ctrl)
    {
        string font = ctrl.Attribute("Font")?.Value ?? string.Empty;
        var style = new StyleDefinition();

        if (font.Contains("Bold", StringComparison.OrdinalIgnoreCase))
        {
            style.FontWeight = "bold";
        }

        return style;
    }

    private static double ParseArUnit(string? val, double fallback)
    {
        if (string.IsNullOrWhiteSpace(val)) return fallback;

        // ActiveReports Section reports often use inches e.g. "1.5" or twips "1440" or "1.5in"
        if (val.EndsWith("in", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(val[..^2], NumberStyles.Any, CultureInfo.InvariantCulture, out double inches))
        {
            return Math.Round(inches * 72.0, 2);
        }

        if (val.EndsWith("cm", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(val[..^2], NumberStyles.Any, CultureInfo.InvariantCulture, out double cm))
        {
            return Math.Round(cm * 28.3465, 2);
        }

        if (double.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
        {
            // If > 200, assume twips (1 pt = 20 twips)
            if (num > 200) return Math.Round(num / 20.0, 2);
            // If <= 15, assume inches
            if (num <= 15) return Math.Round(num * 72.0, 2);
            return num;
        }

        return fallback;
    }
}