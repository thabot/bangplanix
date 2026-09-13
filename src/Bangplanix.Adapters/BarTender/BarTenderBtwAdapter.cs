using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.BarTender;

public sealed class BarTenderBtwAdapter : ILegacyReportAdapter
{
    public string FormatName => "Seagull BarTender Label Designer";
    public IReadOnlyList<string> SupportedExtensions => [".btw", ".btw.xml", ".btw.json"];

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

        if (trimmed.StartsWith('{'))
        {
            return ConvertFromJson(trimmed);
        }

        if (trimmed.StartsWith('<'))
        {
            return ConvertFromXml(trimmed);
        }

        return ConvertFromPlainTextOrBinary(trimmed);
    }

    private static ReportDefinition ConvertFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var title = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "BarTenderLabel" : "BarTenderLabel";
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = title,
                Author = "BarTender Label Adapter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.Custom,
                Width = 283.46, // 100mm default
                Height = 141.73, // 50mm default
                Orientation = PageOrientation.Landscape,
                Margins = new MarginDefinition { Left = 5.67, Right = 5.67, Top = 5.67, Bottom = 5.67 }
            }
        };

        if (root.TryGetProperty("pageSetup", out var psEl) || root.TryGetProperty("labelSetup", out psEl))
        {
            if (psEl.TryGetProperty("width", out var w)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(w.ToString());
            if (psEl.TryGetProperty("height", out var h)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(h.ToString());
            if (report.PageSetup.Width < report.PageSetup.Height) report.PageSetup.Orientation = PageOrientation.Portrait;
        }

        var detailBand = new BandDefinition
        {
            Height = report.PageSetup.Height
        };

        if (root.TryGetProperty("objects", out var objectsEl) && objectsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var objEl in objectsEl.EnumerateArray())
            {
                var elem = ParseJsonObject(objEl);
                if (elem != null) detailBand.Elements.Add(elem);
            }
        }

        report.Bands.Detail = detailBand;
        return report;
    }

    private static ElementDefinition? ParseJsonObject(JsonElement objEl)
    {
        var typeStr = objEl.TryGetProperty("type", out var t) ? t.GetString()?.ToLowerInvariant() : "text";
        var x = objEl.TryGetProperty("x", out var xProp) ? LegacyUnitNormalizer.ConvertToPoints(xProp.ToString()) : 10;
        var y = objEl.TryGetProperty("y", out var yProp) ? LegacyUnitNormalizer.ConvertToPoints(yProp.ToString()) : 10;
        var w = objEl.TryGetProperty("width", out var wProp) ? LegacyUnitNormalizer.ConvertToPoints(wProp.ToString()) : 100;
        var h = objEl.TryGetProperty("height", out var hProp) ? LegacyUnitNormalizer.ConvertToPoints(hProp.ToString()) : 30;

        var elem = new ElementDefinition
        {
            Id = $"btw_{Guid.NewGuid():N}",
            X = x,
            Y = y,
            Width = w,
            Height = h,
            Type = ElementType.Text
        };

        var val = objEl.TryGetProperty("value", out var v) ? v.GetString() : objEl.TryGetProperty("text", out var txt) ? txt.GetString() : null;
        var dataSource = objEl.TryGetProperty("dataSource", out var ds) ? ds.GetString() : null;

        if (typeStr is "barcode" or "2dbarcode" or "qrcode")
        {
            var symbology = objEl.TryGetProperty("symbology", out var sym) ? sym.GetString()?.ToLowerInvariant() : "code128";
            if (symbology is "qr" or "qrcode" or "datamatrix")
            {
                elem.Type = ElementType.QrCode;
            }
            else
            {
                elem.Type = ElementType.Barcode;
                elem.BarcodeType = BarcodeType.Code128;
            }

            elem.Expression = !string.IsNullOrWhiteSpace(dataSource)
                ? LegacyExpressionTranspiler.Transpile(dataSource, "BTW")
                : $"={val ?? "\"12345678\""}";
        }
        else if (typeStr is "box" or "line" or "shape" or "rectangle")
        {
            elem.Type = ElementType.Shape;
        }
        else
        {
            elem.Type = ElementType.Text;
            if (!string.IsNullOrWhiteSpace(dataSource))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(dataSource, "BTW");
            }
            else if (!string.IsNullOrWhiteSpace(val) && (val.Contains('%') || val.Contains('[')))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(val, "BTW");
            }
            else
            {
                elem.Text = val ?? string.Empty;
            }
        }

        return elem;
    }

    private static ReportDefinition ConvertFromXml(string xml)
    {
        var sanitized = LegacyScriptSanitizer.Sanitize(xml);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid BarTender XML template.");

        var title = root.Attribute("name")?.Value ?? root.Element("Name")?.Value ?? "BarTenderLabel";
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = title,
                Author = "BarTender XML Adapter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.Custom,
                Width = 283.46, // 100mm
                Height = 141.73, // 50mm
                Orientation = PageOrientation.Landscape,
                Margins = new MarginDefinition { Left = 5.67, Right = 5.67, Top = 5.67, Bottom = 5.67 }
            }
        };

        var mediaEl = root.Descendants().FirstOrDefault(e => e.Name.LocalName is "Media" or "PageSetup" or "LabelSize");
        if (mediaEl != null)
        {
            var w = mediaEl.Attribute("Width")?.Value ?? mediaEl.Element("Width")?.Value;
            var h = mediaEl.Attribute("Height")?.Value ?? mediaEl.Element("Height")?.Value;
            if (!string.IsNullOrWhiteSpace(w)) report.PageSetup.Width = LegacyUnitNormalizer.ConvertToPoints(w);
            if (!string.IsNullOrWhiteSpace(h)) report.PageSetup.Height = LegacyUnitNormalizer.ConvertToPoints(h);
            if (report.PageSetup.Width < report.PageSetup.Height) report.PageSetup.Orientation = PageOrientation.Portrait;
        }

        var detailBand = new BandDefinition
        {
            Height = report.PageSetup.Height
        };

        foreach (var objEl in root.Descendants().Where(e => e.Name.LocalName is "Object" or "Text" or "Barcode" or "Box" or "Line" or "Graphic"))
        {
            var elem = ParseXmlObject(objEl);
            if (elem != null) detailBand.Elements.Add(elem);
        }

        report.Bands.Detail = detailBand;
        return report;
    }

    private static ElementDefinition? ParseXmlObject(XElement objEl)
    {
        var localName = objEl.Name.LocalName.ToLowerInvariant();
        var xStr = objEl.Attribute("X")?.Value ?? objEl.Attribute("Left")?.Value ?? objEl.Element("X")?.Value ?? "10";
        var yStr = objEl.Attribute("Y")?.Value ?? objEl.Attribute("Top")?.Value ?? objEl.Element("Y")?.Value ?? "10";
        var wStr = objEl.Attribute("Width")?.Value ?? objEl.Element("Width")?.Value ?? "100";
        var hStr = objEl.Attribute("Height")?.Value ?? objEl.Element("Height")?.Value ?? "25";

        var elem = new ElementDefinition
        {
            Id = $"btw_{Guid.NewGuid():N}",
            X = LegacyUnitNormalizer.ConvertToPoints(xStr),
            Y = LegacyUnitNormalizer.ConvertToPoints(yStr),
            Width = LegacyUnitNormalizer.ConvertToPoints(wStr),
            Height = LegacyUnitNormalizer.ConvertToPoints(hStr),
            Type = ElementType.Text
        };

        var val = objEl.Attribute("Value")?.Value ?? objEl.Element("Value")?.Value ?? objEl.Element("Text")?.Value ?? objEl.Value.Trim();
        var fieldName = objEl.Attribute("Field")?.Value ?? objEl.Element("Field")?.Value ?? objEl.Attribute("Source")?.Value;

        if (localName is "barcode")
        {
            var sym = (objEl.Attribute("Symbology")?.Value ?? objEl.Element("Symbology")?.Value ?? "code128").ToLowerInvariant();
            if (sym is "qr" or "qrcode" or "datamatrix")
            {
                elem.Type = ElementType.QrCode;
            }
            else
            {
                elem.Type = ElementType.Barcode;
                elem.BarcodeType = BarcodeType.Code128;
            }

            elem.Expression = !string.IsNullOrWhiteSpace(fieldName)
                ? $"={{Fields.{fieldName}}}"
                : !string.IsNullOrWhiteSpace(val) && val.Contains('%')
                    ? LegacyExpressionTranspiler.Transpile(val, "BTW")
                    : $"={val ?? "\"BARCODE123\""}";
        }
        else if (localName is "box" or "line")
        {
            elem.Type = ElementType.Shape;
        }
        else
        {
            elem.Type = ElementType.Text;
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                elem.Expression = $"={{Fields.{fieldName}}}";
            }
            else if (!string.IsNullOrWhiteSpace(val) && (val.Contains('%') || val.Contains('[')))
            {
                elem.Expression = LegacyExpressionTranspiler.Transpile(val, "BTW");
            }
            else
            {
                elem.Text = val;
            }
        }

        return elem;
    }

    private static ReportDefinition ConvertFromPlainTextOrBinary(string rawText)
    {
        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = "BarTenderBinaryLabel",
                Author = "BarTender Text/Binary Extractor"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.Custom,
                Width = 283.46,
                Height = 141.73,
                Orientation = PageOrientation.Landscape
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 141.73,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Id = "btw_auto_extracted",
                            X = 15,
                            Y = 15,
                            Width = 250,
                            Height = 30,
                            Type = ElementType.Text,
                            Text = rawText.Length > 200 ? rawText[..200] : rawText
                        }
                    ]
                }
            }
        };

        return report;
    }
}
