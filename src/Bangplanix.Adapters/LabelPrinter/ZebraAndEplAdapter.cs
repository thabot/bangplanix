using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.LabelPrinter;

public sealed class ZebraAndEplAdapter : ILegacyReportAdapter
{
    public string FormatName => "Zebra ZPL & Eltron EPL";
    public IReadOnlyList<string> SupportedExtensions => [".zpl", ".epl", ".prn", ".lbl"];

    private static readonly Regex ZplFieldDataRegex = new(@"\^FO(\d+),(\d+)(?:\^A[A-Z0-9,]+)?\^FD(.*?)\^FS", RegexOptions.Compiled);
    private static readonly Regex ZplBarcodeRegex = new(@"\^FO(\d+),(\d+)\^B[C39](?:[A-Z0-9,]*)\^FD(.*?)\^FS", RegexOptions.Compiled);

    private static readonly Regex EplAsciiTextRegex = new(@"^A(\d+),(\d+),(\d+),(\d+),(\d+),(\d+),([NR]),""(.*?)""", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex EplBarcodeRegex = new(@"^B(\d+),(\d+),(\d+),([139E]),(\d+),(\d+),(\d+),([NB]),""(.*?)""", RegexOptions.Multiline | RegexOptions.Compiled);

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

        if (sanitized.Contains("^XA", StringComparison.OrdinalIgnoreCase) || sanitized.Contains("^FO", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertZpl(sanitized);
        }
        else
        {
            return ConvertEpl(sanitized);
        }
    }

    private static ReportDefinition ConvertZpl(string zpl)
    {
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "Zebra ZPL Label",
                Author = "Zebra ZPL Transpiler"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.Custom,
                Width = 288,
                Height = 432,
                Margins = new MarginDefinition { Left = 0, Right = 0, Top = 0, Bottom = 0 }
            }
        };

        var detailBand = new BandDefinition { Height = 432 };

        var barcodeMatches = ZplBarcodeRegex.Matches(zpl);
        foreach (Match m in barcodeMatches)
        {
            double x = double.TryParse(m.Groups[1].Value, out var px) ? px * 0.35 : 10;
            double y = double.TryParse(m.Groups[2].Value, out var py) ? py * 0.35 : 10;
            var data = m.Groups[3].Value;

            detailBand.Elements.Add(new ElementDefinition
            {
                Type = ElementType.Barcode,
                X = x,
                Y = y,
                Width = 200,
                Height = 60,
                Text = data,
                Expression = data.StartsWith('{') ? LegacyExpressionTranspiler.Transpile(data, "LABEL") : null
            });
        }

        var textMatches = ZplFieldDataRegex.Matches(zpl);
        foreach (Match m in textMatches)
        {
            if (m.Value.Contains("^B")) continue;

            double x = double.TryParse(m.Groups[1].Value, out var px) ? px * 0.35 : 10;
            double y = double.TryParse(m.Groups[2].Value, out var py) ? py * 0.35 : 10;
            var data = m.Groups[3].Value;

            var elem = new ElementDefinition
            {
                Type = ElementType.Text,
                X = x,
                Y = y,
                Width = 220,
                Height = 24
            };

            if (data.StartsWith('{') && data.EndsWith('}'))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(data, "LABEL");
            }
            else
            {
                elem.Text = data;
            }

            detailBand.Elements.Add(elem);
        }

        report.Bands.Detail = detailBand;
        return report;
    }

    private static ReportDefinition ConvertEpl(string epl)
    {
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "Eltron EPL Label",
                Author = "Eltron EPL Transpiler"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.Custom,
                Width = 288,
                Height = 432,
                Margins = new MarginDefinition { Left = 0, Right = 0, Top = 0, Bottom = 0 }
            }
        };

        var detailBand = new BandDefinition { Height = 432 };

        var bMatches = EplBarcodeRegex.Matches(epl);
        foreach (Match m in bMatches)
        {
            double x = double.TryParse(m.Groups[1].Value, out var px) ? px * 0.35 : 10;
            double y = double.TryParse(m.Groups[2].Value, out var py) ? py * 0.35 : 10;
            var data = m.Groups[9].Value;

            detailBand.Elements.Add(new ElementDefinition
            {
                Type = ElementType.Barcode,
                X = x,
                Y = y,
                Width = 180,
                Height = 50,
                Text = data
            });
        }

        var tMatches = EplAsciiTextRegex.Matches(epl);
        foreach (Match m in tMatches)
        {
            double x = double.TryParse(m.Groups[1].Value, out var px) ? px * 0.35 : 10;
            double y = double.TryParse(m.Groups[2].Value, out var py) ? py * 0.35 : 10;
            var data = m.Groups[8].Value;

            detailBand.Elements.Add(new ElementDefinition
            {
                Type = ElementType.Text,
                X = x,
                Y = y,
                Width = 200,
                Height = 20,
                Text = data
            });
        }

        report.Bands.Detail = detailBand;
        return report;
    }
}
