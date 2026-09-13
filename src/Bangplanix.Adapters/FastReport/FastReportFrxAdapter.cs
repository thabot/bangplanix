using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.FastReport;

public sealed class FastReportFrxAdapter : ILegacyReportAdapter
{
    public string FormatName => "FastReport FRX";
    public IReadOnlyList<string> SupportedExtensions => [".frx"];

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
        var root = doc.Root;
        if (root == null) throw new InvalidOperationException("Invalid FRX XML document: missing root element.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("ReportName")?.Value ?? "FastReport Imported Document",
                Author = "FastReport Migration"
            }
        };

        var pageElem = root.Element("ReportPage") ?? root.Descendants("ReportPage").FirstOrDefault();
        if (pageElem == null) return report;

        // 1. Page Dimensions (FastReport uses millimeters by default or pixels)
        var pw = pageElem.Attribute("PaperWidth")?.Value;
        var ph = pageElem.Attribute("PaperHeight")?.Value;
        if (!string.IsNullOrEmpty(pw)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints($"{pw}mm", 595);
        if (!string.IsNullOrEmpty(ph)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints($"{ph}mm", 842);

        var lm = pageElem.Attribute("LeftMargin")?.Value;
        var rm = pageElem.Attribute("RightMargin")?.Value;
        var tm = pageElem.Attribute("TopMargin")?.Value;
        var bm = pageElem.Attribute("BottomMargin")?.Value;
        report.PageSetup.Margins = new MarginDefinition
        {
            Left = LegacyUnitNormalizer.ConvertToPoints($"{lm}mm", 28),
            Right = LegacyUnitNormalizer.ConvertToPoints($"{rm}mm", 28),
            Top = LegacyUnitNormalizer.ConvertToPoints($"{tm}mm", 28),
            Bottom = LegacyUnitNormalizer.ConvertToPoints($"{bm}mm", 28)
        };

        // 2. Page Header / Report Title
        var titleBand = pageElem.Element("ReportTitleBand");
        var headerBand = pageElem.Element("PageHeaderBand") ?? titleBand;
        if (headerBand != null)
        {
            var h = LegacyUnitNormalizer.ConvertToPoints($"{headerBand.Attribute("Height")?.Value}mm", 40);
            report.Bands.PageHeader = new BandDefinition
            {
                Height = h,
                Elements = ParseFrxElements(headerBand)
            };
        }

        // 3. DataBand (Detail)
        var dataBand = pageElem.Element("DataBand") ?? pageElem.Descendants("DataBand").FirstOrDefault();
        if (dataBand != null)
        {
            var h = LegacyUnitNormalizer.ConvertToPoints($"{dataBand.Attribute("Height")?.Value}mm", 60);
            report.Bands.Detail = new BandDefinition
            {
                Height = h,
                Elements = ParseFrxElements(dataBand)
            };

            var dsName = dataBand.Attribute("DataSource")?.Value;
            if (!string.IsNullOrEmpty(dsName))
            {
                report.Datasets.Add(new DatasetDefinition
                {
                    Name = dsName,
                    Type = DatasetType.Sql
                });
            }
        }

        // 4. Page Footer / Report Summary
        var summaryBand = pageElem.Element("ReportSummaryBand");
        var footerBand = pageElem.Element("PageFooterBand") ?? summaryBand;
        if (footerBand != null)
        {
            var h = LegacyUnitNormalizer.ConvertToPoints($"{footerBand.Attribute("Height")?.Value}mm", 30);
            report.Bands.PageFooter = new BandDefinition
            {
                Height = h,
                Elements = ParseFrxElements(footerBand)
            };
        }

        return report;
    }

    private static List<ElementDefinition> ParseFrxElements(XElement bandElem)
    {
        var list = new List<ElementDefinition>();

        // TextObjects
        foreach (var tb in bandElem.Elements("TextObject"))
        {
            var text = tb.Attribute("Text")?.Value ?? string.Empty;
            var elem = new ElementDefinition
            {
                Id = tb.Attribute("Name")?.Value,
                Type = ElementType.Text,
                X = LegacyUnitNormalizer.ConvertToPoints($"{tb.Attribute("Left")?.Value}mm", 0),
                Y = LegacyUnitNormalizer.ConvertToPoints($"{tb.Attribute("Top")?.Value}mm", 0),
                Width = LegacyUnitNormalizer.ConvertToPoints($"{tb.Attribute("Width")?.Value}mm", 100),
                Height = LegacyUnitNormalizer.ConvertToPoints($"{tb.Attribute("Height")?.Value}mm", 20),
                Style = ParseFrxStyle(tb)
            };

            if (text.Contains('[') && text.Contains(']'))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(text, "FASTREPORT");
            }
            else
            {
                elem.Text = text;
            }

            list.Add(elem);
        }

        // PictureObjects
        foreach (var pic in bandElem.Elements("PictureObject"))
        {
            list.Add(new ElementDefinition
            {
                Id = pic.Attribute("Name")?.Value,
                Type = ElementType.Image,
                ImageSource = pic.Attribute("Image")?.Value ?? pic.Attribute("ImageLocation")?.Value,
                X = LegacyUnitNormalizer.ConvertToPoints($"{pic.Attribute("Left")?.Value}mm", 0),
                Y = LegacyUnitNormalizer.ConvertToPoints($"{pic.Attribute("Top")?.Value}mm", 0),
                Width = LegacyUnitNormalizer.ConvertToPoints($"{pic.Attribute("Width")?.Value}mm", 100),
                Height = LegacyUnitNormalizer.ConvertToPoints($"{pic.Attribute("Height")?.Value}mm", 100)
            });
        }

        // BarcodeObjects
        foreach (var bc in bandElem.Elements("BarcodeObject"))
        {
            list.Add(new ElementDefinition
            {
                Id = bc.Attribute("Name")?.Value,
                Type = ElementType.Barcode,
                BarcodeType = BarcodeType.Code128,
                Text = bc.Attribute("Text")?.Value,
                X = LegacyUnitNormalizer.ConvertToPoints($"{bc.Attribute("Left")?.Value}mm", 0),
                Y = LegacyUnitNormalizer.ConvertToPoints($"{bc.Attribute("Top")?.Value}mm", 0),
                Width = LegacyUnitNormalizer.ConvertToPoints($"{bc.Attribute("Width")?.Value}mm", 120),
                Height = LegacyUnitNormalizer.ConvertToPoints($"{bc.Attribute("Height")?.Value}mm", 40)
            });
        }

        return list;
    }

    private static StyleDefinition ParseFrxStyle(XElement elem)
    {
        var style = new StyleDefinition();
        var font = elem.Attribute("Font")?.Value; // e.g. "Arial, 10pt, style=Bold"
        if (!string.IsNullOrEmpty(font))
        {
            var parts = font.Split(',');
            if (parts.Length > 0) style.FontFamily = parts[0].Trim();
            if (parts.Length > 1) style.FontSize = LegacyUnitNormalizer.ConvertToPoints(parts[1].Trim(), 10.0);
            if (parts.Length > 2 && parts[2].Contains("Bold", StringComparison.OrdinalIgnoreCase)) style.FontWeight = "Bold";
        }

        var align = elem.Attribute("HorzAlign")?.Value;
        if (!string.IsNullOrEmpty(align) && Enum.TryParse<HorizontalAlign>(align, true, out var hAlign))
        {
            style.Align = hAlign;
        }

        return style;
    }
}
