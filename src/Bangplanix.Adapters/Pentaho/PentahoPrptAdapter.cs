using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Pentaho;

public sealed class PentahoPrptAdapter : ILegacyReportAdapter
{
    public string FormatName => "Pentaho Report Designer";
    public IReadOnlyList<string> SupportedExtensions => [".prpt", ".prpt.xml"];

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.CanSeek)
        {
            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                return ConvertFromZip(archive);
            }
            catch (InvalidDataException)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
        }

        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return ConvertFromLayoutXml(content, null);
    }

    private static ReportDefinition ConvertFromZip(ZipArchive archive)
    {
        string? layoutXml = null;
        string? dataDefinitionXml = null;

        var layoutEntry = archive.GetEntry("layout.xml") ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("layout.xml", StringComparison.OrdinalIgnoreCase));
        if (layoutEntry != null)
        {
            using var reader = new StreamReader(layoutEntry.Open());
            layoutXml = reader.ReadToEnd();
        }

        var dataEntry = archive.GetEntry("datadefinition.xml") ?? archive.GetEntry("datasources/sql-ds.xml") ?? archive.Entries.FirstOrDefault(e => e.FullName.EndsWith("datadefinition.xml", StringComparison.OrdinalIgnoreCase));
        if (dataEntry != null)
        {
            using var reader = new StreamReader(dataEntry.Open());
            dataDefinitionXml = reader.ReadToEnd();
        }

        if (string.IsNullOrWhiteSpace(layoutXml))
        {
            throw new InvalidOperationException("Pentaho .prpt archive missing layout.xml.");
        }

        return ConvertFromLayoutXml(layoutXml, dataDefinitionXml);
    }

    private static ReportDefinition ConvertFromLayoutXml(string layoutContent, string? dataDefinitionContent)
    {
        var sanitizedLayout = LegacyScriptSanitizer.Sanitize(layoutContent);
        var doc = XDocument.Parse(sanitizedLayout);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid Pentaho Layout XML.");

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = root.Attribute("name")?.Value ?? "Pentaho Report",
                Author = "Pentaho Report Converter"
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

        if (!string.IsNullOrWhiteSpace(dataDefinitionContent))
        {
            try
            {
                var dataDoc = XDocument.Parse(dataDefinitionContent);
                var sqlNodes = dataDoc.Descendants().Where(e => e.Name.LocalName.Equals("static-query", StringComparison.OrdinalIgnoreCase) ||
                                                                e.Name.LocalName.Equals("query", StringComparison.OrdinalIgnoreCase) ||
                                                                e.Name.LocalName.Equals("sql-query", StringComparison.OrdinalIgnoreCase));
                foreach (var sqlNode in sqlNodes)
                {
                    var qName = sqlNode.Attribute("name")?.Value ?? $"Query_{report.Datasets.Count + 1}";
                    var sqlText = sqlNode.Value?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(sqlText))
                    {
                        report.Datasets.Add(new DatasetDefinition
                        {
                            Name = qName,
                            QueryOrUrl = sqlText
                        });
                    }
                }

                var paramNodes = dataDoc.Descendants().Where(e => e.Name.LocalName.Equals("parameter", StringComparison.OrdinalIgnoreCase) ||
                                                                  e.Name.LocalName.Equals("list-parameter", StringComparison.OrdinalIgnoreCase) ||
                                                                  e.Name.LocalName.Equals("plain-parameter", StringComparison.OrdinalIgnoreCase));
                foreach (var pNode in paramNodes)
                {
                    var pName = pNode.Attribute("name")?.Value;
                    if (string.IsNullOrWhiteSpace(pName)) continue;

                    var pTypeStr = pNode.Attribute("type")?.Value ?? pNode.Attribute("value-type")?.Value ?? "string";
                    report.Parameters.Add(new ParameterDefinition
                    {
                        Name = pName,
                        Label = pNode.Attribute("label")?.Value ?? pName,
                        Type = MapParameterType(pTypeStr)
                    });
                }
            }
            catch
            {
            }
        }

        var detailBand = new BandDefinition { Height = 40 };
        var headerBand = new BandDefinition { Height = 40 };
        var footerBand = new BandDefinition { Height = 30 };

        foreach (var elementNode in root.Descendants())
        {
            var localName = elementNode.Name.LocalName.ToLowerInvariant();

            if (localName is "string-field" or "number-field" or "date-field" or "message-field" or "label" or "resource-label" or "element")
            {
                var elem = ParsePentahoElement(elementNode);
                if (elem == null) continue;

                var parentBandName = FindParentBandName(elementNode);
                if (parentBandName.Contains("header", StringComparison.OrdinalIgnoreCase))
                {
                    headerBand.Elements.Add(elem);
                    headerBand.Height = Math.Max(headerBand.Height, elem.Y + elem.Height + 5);
                }
                else if (parentBandName.Contains("footer", StringComparison.OrdinalIgnoreCase))
                {
                    footerBand.Elements.Add(elem);
                    footerBand.Height = Math.Max(footerBand.Height, elem.Y + elem.Height + 5);
                }
                else
                {
                    detailBand.Elements.Add(elem);
                    detailBand.Height = Math.Max(detailBand.Height, elem.Y + elem.Height + 5);
                }
            }
        }

        if (headerBand.Elements.Count > 0)
        {
            report.Bands.ReportHeader = headerBand;
        }

        if (footerBand.Elements.Count > 0)
        {
            report.Bands.ReportFooter = footerBand;
        }

        report.Bands.Detail = detailBand;

        return report;
    }

    private static string FindParentBandName(XElement elem)
    {
        var current = elem.Parent;
        while (current != null)
        {
            var name = current.Name.LocalName.ToLowerInvariant();
            var bandType = current.Attribute("type")?.Value?.ToLowerInvariant() ?? "";
            if (name.Contains("header") || bandType.Contains("header")) return "header";
            if (name.Contains("footer") || bandType.Contains("footer")) return "footer";
            if (name.Contains("detail") || name.Contains("itemband") || bandType.Contains("detail")) return "detail";
            current = current.Parent;
        }
        return "detail";
    }

    private static ElementDefinition? ParsePentahoElement(XElement node)
    {
        var elem = new ElementDefinition
        {
            Type = ElementType.Text,
            X = ParseDouble(node.Attribute("x")?.Value ?? node.Descendants().FirstOrDefault(d => d.Name.LocalName == "x")?.Value, 0),
            Y = ParseDouble(node.Attribute("y")?.Value ?? node.Descendants().FirstOrDefault(d => d.Name.LocalName == "y")?.Value, 0),
            Width = ParseDouble(node.Attribute("width")?.Value ?? node.Descendants().FirstOrDefault(d => d.Name.LocalName == "width")?.Value, 120),
            Height = ParseDouble(node.Attribute("height")?.Value ?? node.Descendants().FirstOrDefault(d => d.Name.LocalName == "height")?.Value, 20)
        };

        var fieldName = node.Attribute("field")?.Value ??
                        node.Descendants().FirstOrDefault(d => d.Name.LocalName == "field-name" || d.Name.LocalName == "field")?.Value;

        var textVal = node.Attribute("value")?.Value ??
                      node.Descendants().FirstOrDefault(d => d.Name.LocalName == "value" || d.Name.LocalName == "text")?.Value ??
                      node.Value?.Trim();

        var format = node.Attribute("format")?.Value ?? node.Descendants().FirstOrDefault(d => d.Name.LocalName == "format")?.Value;
        if (!string.IsNullOrWhiteSpace(format))
        {
            elem.Style = new StyleDefinition { Format = format };
        }

        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            elem.Expression = LegacyExpressionTranspiler.Transpile(fieldName, "PENTAHO");
        }
        else if (!string.IsNullOrWhiteSpace(textVal))
        {
            if (textVal.StartsWith("$("))
            {
                var expr = textVal.Replace("$(", "{").Replace(")", "}");
                elem.Expression = LegacyExpressionTranspiler.Transpile(expr, "PENTAHO");
            }
            else
            {
                elem.Text = textVal;
            }
        }

        return elem;
    }

    private static double ParseDouble(string? val, double fallback)
    {
        if (string.IsNullOrWhiteSpace(val)) return fallback;
        val = val.Replace("pt", "").Replace("px", "").Trim();
        return double.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var res) ? res : fallback;
    }

    private static ParameterType MapParameterType(string typeStr) => typeStr.ToLowerInvariant() switch
    {
        "int" or "integer" or "long" or "double" or "float" or "bigdecimal" or "number" or "java.lang.integer" or "java.lang.double" => ParameterType.Number,
        "date" or "datetime" or "timestamp" or "java.util.date" or "java.sql.date" => ParameterType.DateTime,
        "bool" or "boolean" or "java.lang.boolean" => ParameterType.Boolean,
        _ => ParameterType.String
    };
}
