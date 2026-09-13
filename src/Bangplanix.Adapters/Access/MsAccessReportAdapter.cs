using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Access;

public sealed class MsAccessReportAdapter : ILegacyReportAdapter
{
    public string FormatName => "Microsoft Access Database Reports";
    public IReadOnlyList<string> SupportedExtensions => [".accdb", ".mdb", ".access.txt", ".access.xml"];

    public const double TwipsPerPoint = 20.0; // 1440 twips per inch / 72 points per inch = 20 twips/pt

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = reader.ReadToEnd();
        return Convert(content);
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var trimmed = content.Trim();

        if (trimmed.StartsWith('<'))
        {
            return ConvertFromXml(trimmed);
        }

        return ConvertFromSaveAsText(trimmed);
    }

    private static ReportDefinition ConvertFromSaveAsText(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "AccessReport",
                Author = "Microsoft Access Report Adapter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Width = 595.28,
                Height = 841.89,
                Orientation = PageOrientation.Portrait,
                Margins = new MarginDefinition { Left = 36, Right = 36, Top = 36, Bottom = 36 }
            }
        };

        var currentSectionIndex = -1; // 0=Detail, 1=Header, 2=Footer, 3=PageHeader, 4=PageFooter, 5=GroupHeader, 6=GroupFooter
        BandDefinition? currentBand = null;
        var currentControl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var currentControlType = string.Empty;
        var inControl = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Extract Report Caption or Name (only at report level before sections/controls)
            if (!inControl && currentSectionIndex == -1 && line.StartsWith("Caption =\"", StringComparison.OrdinalIgnoreCase))
            {
                report.Metadata.Title = ExtractQuotedValue(line);
            }

            // Extract RecordSource SQL query
            if (line.StartsWith("RecordSource =\"", StringComparison.OrdinalIgnoreCase))
            {
                var sql = ExtractQuotedValue(line);
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    report.Datasets.Add(new DatasetDefinition
                    {
                        Name = "AccessRecordSource",
                        QueryOrUrl = sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ? sql : $"SELECT * FROM [{sql}]"
                    });
                }
            }

            // Section Start
            if (line.StartsWith("Begin Section", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Section =", StringComparison.OrdinalIgnoreCase))
            {
                var secMatch = Regex.Match(line, @"(?:Section\s*=\s*|Begin\s+Section\s+)?(\d+)", RegexOptions.IgnoreCase);
                if (secMatch.Success && int.TryParse(secMatch.Groups[1].Value, out var sIdx))
                {
                    currentSectionIndex = sIdx;
                    currentBand = new BandDefinition { Height = 40 };

                    switch (currentSectionIndex)
                    {
                        case 1 or 3: // ReportHeader or PageHeader
                            report.Bands.PageHeader = currentBand;
                            break;
                        case 2 or 4: // ReportFooter or PageFooter
                            report.Bands.PageFooter = currentBand;
                            break;
                        default: // 0=Detail
                            report.Bands.Detail = currentBand;
                            break;
                    }
                }
            }

            // Control Start
            if (line.StartsWith("Begin ", StringComparison.OrdinalIgnoreCase))
            {
                var cType = line[6..].Trim();
                if (cType is "TextBox" or "Label" or "Line" or "Rectangle" or "ImageControl" or "Subform")
                {
                    inControl = true;
                    currentControlType = cType;
                    currentControl.Clear();
                }
            }
            else if (inControl && line.StartsWith("End", StringComparison.OrdinalIgnoreCase))
            {
                if (currentBand != null && !string.IsNullOrWhiteSpace(currentControlType))
                {
                    var elem = BuildElementFromAccessProperties(currentControlType, currentControl);
                    if (elem != null) currentBand.Elements.Add(elem);
                }
                inControl = false;
                currentControlType = string.Empty;
                currentControl.Clear();
            }
            else if (inControl && line.Contains('='))
            {
                var eqIdx = line.IndexOf('=');
                var key = line[..eqIdx].Trim();
                var val = line[(eqIdx + 1)..].Trim();
                currentControl[key] = val;
            }
            else if (currentBand != null && line.StartsWith("Height =", StringComparison.OrdinalIgnoreCase))
            {
                var hVal = line[8..].Trim();
                if (double.TryParse(hVal, NumberStyles.Any, CultureInfo.InvariantCulture, out var hTwips))
                {
                    currentBand.Height = Math.Max(currentBand.Height, hTwips / TwipsPerPoint);
                }
            }
        }

        // Ensure Detail Band exists
        report.Bands.Detail ??= new BandDefinition { Height = 100 };

        return report;
    }

    private static ElementDefinition? BuildElementFromAccessProperties(string controlType, Dictionary<string, string> props)
    {
        props.TryGetValue("Name", out var name);
        props.TryGetValue("Left", out var leftStr);
        props.TryGetValue("Top", out var topStr);
        props.TryGetValue("Width", out var widthStr);
        props.TryGetValue("Height", out var heightStr);

        var x = double.TryParse(leftStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var lTwips) ? lTwips / TwipsPerPoint : 10;
        var y = double.TryParse(topStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tTwips) ? tTwips / TwipsPerPoint : 10;
        var w = double.TryParse(widthStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var wTwips) ? wTwips / TwipsPerPoint : 120;
        var h = double.TryParse(heightStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var hTwips) ? hTwips / TwipsPerPoint : 20;

        var elem = new ElementDefinition
        {
            Id = $"acc_{name?.Trim('"', ' ') ?? Guid.NewGuid().ToString("N")}",
            X = x,
            Y = y,
            Width = w,
            Height = h,
            Type = ElementType.Text
        };

        if (controlType.Equals("Label", StringComparison.OrdinalIgnoreCase))
        {
            elem.Type = ElementType.Text;
            if (props.TryGetValue("Caption", out var caption))
            {
                elem.Text = caption.Trim('"', ' ');
            }
        }
        else if (controlType.Equals("TextBox", StringComparison.OrdinalIgnoreCase))
        {
            elem.Type = ElementType.Text;
            if (props.TryGetValue("ControlSource", out var cs))
            {
                var cleanCs = cs.Trim('"', ' ');
                if (cleanCs.StartsWith('='))
                {
                    elem.Expression = LegacyExpressionTranspiler.Transpile(cleanCs, "ACCESS");
                }
                else
                {
                    elem.Expression = $"={{Fields.{cleanCs.Trim('[', ']')}}}";
                }
            }
        }
        else if (controlType.Equals("Line", StringComparison.OrdinalIgnoreCase) || controlType.Equals("Rectangle", StringComparison.OrdinalIgnoreCase))
        {
            elem.Type = ElementType.Shape;
        }

        // Font and Styling
        elem.Style = new StyleDefinition();
        if (props.TryGetValue("FontName", out var fn)) elem.Style.FontFamily = fn.Trim('"', ' ');
        if (props.TryGetValue("FontSize", out var fs) && double.TryParse(fs, NumberStyles.Any, CultureInfo.InvariantCulture, out var fSize)) elem.Style.FontSize = fSize;
        if (props.TryGetValue("FontWeight", out var fw) && int.TryParse(fw, out var fWeight) && fWeight >= 700) elem.Style.FontWeight = "Bold";
        if (props.TryGetValue("FontItalic", out var fi) && (fi.Equals("true", StringComparison.OrdinalIgnoreCase) || fi.Equals("-1"))) elem.Style.FontStyle = "Italic";

        return elem;
    }

    private static ReportDefinition ConvertFromXml(string xml)
    {
        var sanitized = LegacyScriptSanitizer.Sanitize(xml);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid Access XML report.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("name")?.Value ?? root.Element("Name")?.Value ?? "AccessXmlReport",
                Author = "Microsoft Access XML Adapter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Width = 595.28,
                Height = 841.89,
                Orientation = PageOrientation.Portrait
            }
        };

        var detailBand = new BandDefinition { Height = 100 };
        foreach (var ctrlEl in root.Descendants().Where(e => e.Name.LocalName is "TextBox" or "Label" or "Control"))
        {
            var x = ctrlEl.Attribute("Left") != null ? LegacyUnitNormalizer.ConvertToPoints(ctrlEl.Attribute("Left")!.Value) : 10;
            var y = ctrlEl.Attribute("Top") != null ? LegacyUnitNormalizer.ConvertToPoints(ctrlEl.Attribute("Top")!.Value) : 10;
            var w = ctrlEl.Attribute("Width") != null ? LegacyUnitNormalizer.ConvertToPoints(ctrlEl.Attribute("Width")!.Value) : 120;
            var h = ctrlEl.Attribute("Height") != null ? LegacyUnitNormalizer.ConvertToPoints(ctrlEl.Attribute("Height")!.Value) : 20;

            var elem = new ElementDefinition
            {
                Id = $"acc_{Guid.NewGuid():N}",
                X = x,
                Y = y,
                Width = w,
                Height = h,
                Type = ElementType.Text
            };

            var cs = ctrlEl.Attribute("ControlSource")?.Value ?? ctrlEl.Element("ControlSource")?.Value;
            var cap = ctrlEl.Attribute("Caption")?.Value ?? ctrlEl.Element("Caption")?.Value;

            if (!string.IsNullOrWhiteSpace(cs))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(cs, "ACCESS");
            }
            else if (!string.IsNullOrWhiteSpace(cap))
            {
                elem.Text = cap;
            }

            detailBand.Elements.Add(elem);
        }

        report.Bands.Detail = detailBand;
        return report;
    }

    private static string ExtractQuotedValue(string line)
    {
        var firstQuote = line.IndexOf('"');
        var lastQuote = line.LastIndexOf('"');
        if (firstQuote >= 0 && lastQuote > firstQuote)
        {
            return line.Substring(firstQuote + 1, lastQuote - firstQuote - 1);
        }
        return string.Empty;
    }
}
