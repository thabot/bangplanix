using System.Text;
using System.Text.Json;
using Bangplanix.Connectors.Excel;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Pdf;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class ReportRenderMultiFormatTests
{
    private const string SampleReportBpx = """
    {
      "version": "1.0",
      "metadata": {
        "title": "Monthly Sales Performance",
        "author": "Bangplanix Test"
      },
      "pageSetup": {
        "width": 595.28,
        "height": 841.89,
        "orientation": "Portrait"
      },
      "bands": {
        "PageHeader": {
          "height": 50,
          "elements": [
            {
              "type": "Text",
              "bounds": { "x": 0, "y": 10, "width": 400, "height": 30 },
              "text": "Monthly Sales Performance"
            }
          ]
        },
        "Detail": {
          "height": 25,
          "elements": [
            {
              "type": "Text",
              "bounds": { "x": 0, "y": 0, "width": 200, "height": 20 },
              "expression": "[ItemName]"
            },
            {
              "type": "Text",
              "bounds": { "x": 220, "y": 0, "width": 100, "height": 20 },
              "expression": "[Amount]"
            }
          ]
        }
      }
    }
    """;

    private static readonly List<IDictionary<string, object?>> TestRows =
    [
        new Dictionary<string, object?> { ["ItemName"] = "Enterprise License A", ["Amount"] = 499.00 },
        new Dictionary<string, object?> { ["ItemName"] = "Support Subscription B", ["Amount"] = 149.50 },
        new Dictionary<string, object?> { ["ItemName"] = "Custom Integration C", ["Amount"] = 1200.00 }
    ];

    [Fact]
    public async Task RenderToPdf_WithParametersAndRows_ShouldProduceValidPdfBytes()
    {
        var report = BpxParser.Parse(SampleReportBpx);
        var renderer = new SkiaPdfRenderer();
        var parameters = new Dictionary<string, object?> { ["Tenant"] = "Bangkok Corp" };

        var pdfBytes = await renderer.RenderToPdfAsync(report, parameters, TestRows);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
        var header = Encoding.ASCII.GetString(pdfBytes[..5]);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task RenderToExcel_WithRows_ShouldProduceValidXlsxStream()
    {
        var report = BpxParser.Parse(SampleReportBpx);
        using var ms = new MemoryStream();

        await MiniExcelReportExporter.ExportReportToExcelAsync(report, TestRows, ms);

        Assert.True(ms.Length > 0);
        var bytes = ms.ToArray();
        // PK zip header (0x50, 0x4B)
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void GenerateCsv_WithRows_ShouldIncludeProperHeadersAndEscapedValues()
    {
        var rows = TestRows;
        var sb = new StringBuilder();
        var keys = rows[0].Keys.ToList();
        sb.AppendLine(string.Join(",", keys.Select(k => $"\"{k.Replace("\"", "\"\"")}\"")));
        foreach (var row in rows)
        {
            var line = string.Join(",", keys.Select(k =>
            {
                var val = row.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "";
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }));
            sb.AppendLine(line);
        }

        var csv = sb.ToString();
        Assert.Contains("\"ItemName\",\"Amount\"", csv);
        Assert.Contains("\"Enterprise License A\",\"499\"", csv);
        Assert.Contains("\"Custom Integration C\",\"1200\"", csv);
    }

    [Fact]
    public void GenerateJson_WithRows_ShouldProduceValidJsonArray()
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(TestRows, new JsonSerializerOptions { WriteIndented = true });
        Assert.NotNull(jsonBytes);
        Assert.True(jsonBytes.Length > 0);

        using var doc = JsonDocument.Parse(jsonBytes);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(3, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public void GenerateHtml_WithReportTitleAndRows_ShouldIncludeHtmlTable()
    {
        var reportTitle = "Monthly Sales Performance";
        var rows = TestRows;
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><title>" + System.Net.WebUtility.HtmlEncode(reportTitle) + "</title></head><body>");
        sb.AppendLine("<h2>" + System.Net.WebUtility.HtmlEncode(reportTitle) + "</h2>");
        if (rows.Count > 0)
        {
            sb.AppendLine("<table><thead><tr>");
            var keys = rows[0].Keys.ToList();
            foreach (var k in keys) sb.AppendLine("<th>" + System.Net.WebUtility.HtmlEncode(k) + "</th>");
            sb.AppendLine("</tr></thead><tbody>");
            foreach (var row in rows)
            {
                sb.AppendLine("<tr>");
                foreach (var k in keys)
                {
                    var val = row.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "";
                    sb.AppendLine("<td>" + System.Net.WebUtility.HtmlEncode(val) + "</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody></table>");
        }
        sb.AppendLine("</body></html>");

        var html = sb.ToString();
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<h2>Monthly Sales Performance</h2>", html);
        Assert.Contains("<th>ItemName</th>", html);
        Assert.Contains("<td>Enterprise License A</td>", html);
    }

    [Fact]
    public void GenerateSvg_WithReportTitle_ShouldProduceValidSvgXml()
    {
        var reportTitle = "Monthly Sales Performance";
        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"800\" height=\"600\"><text x=\"20\" y=\"40\" font-family=\"sans-serif\" font-size=\"20\">{System.Net.WebUtility.HtmlEncode(reportTitle)}</text></svg>";

        Assert.StartsWith("<svg", svg);
        Assert.EndsWith("</svg>", svg);
        Assert.Contains("Monthly Sales Performance", svg);
    }
}
