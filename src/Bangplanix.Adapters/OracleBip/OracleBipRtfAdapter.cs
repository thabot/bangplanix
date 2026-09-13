using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.OracleBip;

public sealed class OracleBipRtfAdapter : ILegacyReportAdapter
{
    public string FormatName => "Oracle BI Publisher (RTF / XSL-FO)";
    public IReadOnlyList<string> SupportedExtensions => [".rtf", ".xpt"];

    private static readonly Regex XslTagRegex = new(@"<\?([^\?>]+)\?>", RegexOptions.Compiled);

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
                Title = "Oracle BI Publisher Report",
                Author = "Oracle BI Publisher Converter"
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

        var rawText = ExtractPlainTextAndTagsFromRtf(sanitized);

        var detailBand = new BandDefinition { Height = 60 };
        var headerBand = new BandDefinition { Height = 40 };

        double y = 10;
        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var matches = XslTagRegex.Matches(trimmed);
            if (matches.Count > 0)
            {
                double x = 10;
                foreach (Match m in matches)
                {
                    var tagContent = m.Groups[1].Value.Trim();

                    if (tagContent.StartsWith("value-of:", StringComparison.OrdinalIgnoreCase))
                    {
                        var fieldExpr = tagContent["value-of:".Length..].Trim();
                        var elem = new ElementDefinition
                        {
                            Type = ElementType.Text,
                            X = x,
                            Y = y,
                            Width = 140,
                            Height = 20,
                            Expression = LegacyExpressionTranspiler.Transpile(fieldExpr, "ORACLE")
                        };
                        detailBand.Elements.Add(elem);
                        x += 150;
                    }
                    else if (tagContent.StartsWith("for-each:", StringComparison.OrdinalIgnoreCase) ||
                             tagContent.StartsWith("for-each-group:", StringComparison.OrdinalIgnoreCase))
                    {
                        var groupName = tagContent.Substring(tagContent.IndexOf(':') + 1).Trim();
                        if (!report.Datasets.Any(d => d.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
                        {
                            report.Datasets.Add(new DatasetDefinition
                            {
                                Name = groupName,
                                QueryOrUrl = $"SELECT * FROM {groupName}"
                            });
                        }
                    }
                }

                if (x > 10)
                {
                    y += 25;
                }
            }
            else
            {
                var elem = new ElementDefinition
                {
                    Type = ElementType.Text,
                    X = 10,
                    Y = headerBand.Elements.Count * 25 + 10,
                    Width = 500,
                    Height = 22,
                    Text = trimmed,
                    Style = new StyleDefinition { FontWeight = "bold", FontSize = 12 }
                };
                headerBand.Elements.Add(elem);
                headerBand.Height = Math.Max(headerBand.Height, elem.Y + 30);
            }
        }

        if (headerBand.Elements.Count > 0)
        {
            report.Bands.ReportHeader = headerBand;
        }

        detailBand.Height = Math.Max(40, y + 20);
        report.Bands.Detail = detailBand;

        return report;
    }

    private static string ExtractPlainTextAndTagsFromRtf(string rtf)
    {
        if (!rtf.StartsWith("{\rtf", StringComparison.OrdinalIgnoreCase))
        {
            return rtf;
        }

        var sb = new StringBuilder();
        bool inTag = false;
        var tagSb = new StringBuilder();

        for (int i = 0; i < rtf.Length; i++)
        {
            if (i + 1 < rtf.Length && rtf[i] == '<' && rtf[i + 1] == '?')
            {
                inTag = true;
                tagSb.Clear();
            }

            if (inTag)
            {
                tagSb.Append(rtf[i]);
                if (rtf[i] == '>' && tagSb.ToString().EndsWith("?>"))
                {
                    inTag = false;
                    sb.Append(' ').Append(tagSb.ToString()).Append(' ');
                }
                continue;
            }

            if (rtf[i] == '\\')
            {
                while (i < rtf.Length && !char.IsWhiteSpace(rtf[i]) && rtf[i] != '{' && rtf[i] != '}' && rtf[i] != '\\' && rtf[i] != ';')
                {
                    i++;
                }
                continue;
            }

            if (rtf[i] == '{' || rtf[i] == '}')
            {
                continue;
            }

            if (rtf[i] == '\r' || rtf[i] == '\n')
            {
                sb.AppendLine();
                continue;
            }

            sb.Append(rtf[i]);
        }

        return sb.ToString();
    }
}
