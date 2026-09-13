using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Handlebars;

public sealed class HandlebarsHtmlAdapter : ILegacyReportAdapter
{
    public string FormatName => "Handlebars & Mustache (HTML/CSS)";
    public IReadOnlyList<string> SupportedExtensions => [".hbs", ".mustache"];

    private static readonly Regex BlockHelperRegex = new(@"\{\{#(each|if|with|unless)\s+([a-zA-Z0-9_\.]+)\s*\}\}", RegexOptions.Compiled);
    private static readonly Regex ParamHelperRegex = new(@"\{\{\s*(?:Parameters\.|params\.|p_)?([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

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

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "Handlebars Template Report",
                Author = "Handlebars/Mustache Converter"
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

        var titleMatch = Regex.Match(sanitized, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            report.Metadata.Title = titleMatch.Groups[1].Value.Trim();
        }

        var blockMatches = BlockHelperRegex.Matches(sanitized);
        foreach (Match m in blockMatches)
        {
            if (m.Groups[1].Value.Equals("each", StringComparison.OrdinalIgnoreCase))
            {
                var dsName = m.Groups[2].Value;
                if (!report.Datasets.Any(d => d.Name.Equals(dsName, StringComparison.OrdinalIgnoreCase)))
                {
                    report.Datasets.Add(new DatasetDefinition
                    {
                        Name = dsName,
                        QueryOrUrl = $"SELECT * FROM {dsName}"
                    });
                }
            }
        }

        // Find variables inside {{#each}} blocks to exclude them from report parameters
        var eachBlockVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var eachSections = Regex.Matches(sanitized, @"\{\{#each\s+([a-zA-Z0-9_\.]+)\s*\}\}(.*?)\{\{/each\}\}", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match eachSec in eachSections)
        {
            var innerContent = eachSec.Groups[2].Value;
            var innerVars = Regex.Matches(innerContent, @"\{\{\s*([a-zA-Z0-9_\.]+)\s*\}\}");
            foreach (Match iv in innerVars)
            {
                eachBlockVars.Add(iv.Groups[1].Value);
            }
        }

        var paramMatches = ParamHelperRegex.Matches(sanitized);
        var seenParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in paramMatches)
        {
            var pName = m.Groups[1].Value;
            if (!pName.StartsWith("each", StringComparison.OrdinalIgnoreCase) &&
                !pName.StartsWith("if", StringComparison.OrdinalIgnoreCase) &&
                !pName.StartsWith("else", StringComparison.OrdinalIgnoreCase) &&
                !pName.StartsWith("with", StringComparison.OrdinalIgnoreCase) &&
                !eachBlockVars.Contains(pName) &&
                seenParams.Add(pName))
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

        var lines = sanitized.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("<head", StringComparison.OrdinalIgnoreCase)) continue;

            var cleanText = Regex.Replace(trimmed, @"<[^>]+>", " ").Trim();
            if (string.IsNullOrWhiteSpace(cleanText)) continue;

            var elem = new ElementDefinition
            {
                Type = ElementType.Text,
                X = 20,
                Y = y,
                Width = 500,
                Height = 20
            };

            if (cleanText.Contains("{{") && cleanText.Contains("}}"))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(cleanText, "HANDLEBARS");
            }
            else
            {
                elem.Text = cleanText;
            }

            if (trimmed.StartsWith("<h1", StringComparison.OrdinalIgnoreCase))
            {
                elem.Style = new StyleDefinition { FontSize = 18, FontWeight = "Bold" };
                headerBand.Elements.Add(elem);
            }
            else if (trimmed.StartsWith("<h2", StringComparison.OrdinalIgnoreCase))
            {
                elem.Style = new StyleDefinition { FontSize = 14, FontWeight = "Bold" };
                headerBand.Elements.Add(elem);
            }
            else
            {
                elem.Style = new StyleDefinition { FontSize = 10 };
                detailBand.Elements.Add(elem);
                y += 24;
            }
        }

        if (headerBand.Elements.Count > 0)
        {
            headerBand.Height = Math.Max(40, headerBand.Elements.Count * 30 + 10);
            report.Bands.ReportHeader = headerBand;
        }

        detailBand.Height = Math.Max(40, y + 20);
        report.Bands.Detail = detailBand;

        return report;
    }
}
