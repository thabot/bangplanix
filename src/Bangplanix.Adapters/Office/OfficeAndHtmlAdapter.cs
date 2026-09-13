using System.Text.RegularExpressions;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Office;

public sealed class OfficeAndHtmlAdapter : ILegacyReportAdapter
{
    public string FormatName => "Office & HTML/Liquid Templates";
    public IReadOnlyList<string> SupportedExtensions => [".html", ".htm", ".liquid", ".docx", ".xlsx"];

    public ReportDefinition Convert(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var sanitized = LegacyScriptSanitizer.Sanitize(content);

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "Imported Template Report",
                Author = "Template Migration Engine"
            },
            PageSetup = new PageSetup
            {
                Width = 595.28,
                Height = 841.89,
                Margins = new MarginDefinition { Left = 36, Right = 36, Top = 36, Bottom = 36 }
            }
        };

        var titleMatch = Regex.Match(sanitized, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            report.Metadata.Title = titleMatch.Groups[1].Value.Trim();
        }

        var paramMatches = Regex.Matches(sanitized, @"\{\{\s*(?:Parameters\.|params\.|p_)?([a-zA-Z0-9_]+)\s*\}\}");
        var seenParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in paramMatches)
        {
            var pName = match.Groups[1].Value;
            if (seenParams.Add(pName))
            {
                report.Parameters.Add(new ParameterDefinition
                {
                    Name = pName,
                    Label = pName,
                    Type = ParameterType.String
                });
            }
        }

        var detailBand = new BandDefinition { Height = 200 };
        double currentY = 10;

        var lines = sanitized.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("</html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<head", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("</head", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<title", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("</title", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("<body", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("</body", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cleanText = Regex.Replace(trimmed, @"<[^>]+>", " ").Trim();
            if (string.IsNullOrWhiteSpace(cleanText)) continue;

            var elem = new ElementDefinition
            {
                Type = ElementType.Text,
                X = 0,
                Y = currentY,
                Width = 500,
                Height = 20
            };

            if (cleanText.Contains("{{", StringComparison.Ordinal) && cleanText.Contains("}}", StringComparison.Ordinal))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(cleanText, "LIQUID");
            }
            else
            {
                elem.Text = cleanText;
            }

            if (trimmed.StartsWith("<h1", StringComparison.OrdinalIgnoreCase))
            {
                elem.Style = new StyleDefinition { FontSize = 18, FontWeight = "Bold" };
                elem.Height = 28;
                currentY += 32;
            }
            else if (trimmed.StartsWith("<h2", StringComparison.OrdinalIgnoreCase))
            {
                elem.Style = new StyleDefinition { FontSize = 14, FontWeight = "Bold" };
                elem.Height = 22;
                currentY += 26;
            }
            else
            {
                elem.Style = new StyleDefinition { FontSize = 10 };
                currentY += 22;
            }

            detailBand.Elements.Add(elem);
        }

        detailBand.Height = Math.Max(detailBand.Height, currentY + 10);
        report.Bands.Detail = detailBand;

        return report;
    }
}
