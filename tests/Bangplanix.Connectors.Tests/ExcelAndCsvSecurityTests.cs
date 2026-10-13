using System.Text;
using Bangplanix.Connectors.Csv;
using Bangplanix.Connectors.Excel;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;
using Xunit;

namespace Bangplanix.Connectors.Tests;

public class ExcelAndCsvSecurityTests
{
    [Fact]
    public async Task CsvStreamingExporter_ShouldIncludeBom_AndSanitizeFormulaInjection()
    {
        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["ID"] = 1, ["Name"] = "Normal Customer", ["FormulaPayload"] = "Safe Text" },
            new Dictionary<string, object?> { ["ID"] = 2, ["Name"] = "Malicious Cell", ["FormulaPayload"] = "=cmd|' /C calc'!A0" },
            new Dictionary<string, object?> { ["ID"] = 3, ["Name"] = "Plus Payload", ["FormulaPayload"] = "+12345" },
            new Dictionary<string, object?> { ["ID"] = 4, ["Name"] = "Minus Payload", ["FormulaPayload"] = "-cmd|' /C calc'!A0" },
            new Dictionary<string, object?> { ["ID"] = 5, ["Name"] = "At Payload", ["FormulaPayload"] = "@SUM(A1:A10)" }
        };

        var options = new CsvExportOptions
        {
            IncludeUtf8Bom = true,
            SanitizeFormulas = true,
            Delimiter = ","
        };

        using var memoryStream = new MemoryStream();
        await CsvStreamingExporter.ExportToStreamAsync(memoryStream, rows, options);

        var bytes = memoryStream.ToArray();
        Assert.True(bytes.Length >= 3);

        // Verify UTF-8 BOM: 0xEF, 0xBB, 0xBF
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);

        var csvText = Encoding.UTF8.GetString(bytes);

        // Verify sanitization against CWE-123 (starts with tab + apostrophe or apostrophe)
        Assert.Contains("'=cmd|' /C calc'!A0", csvText);
        Assert.Contains("'+12345", csvText);
        Assert.Contains("'-cmd|' /C calc'!A0", csvText);
        Assert.Contains("'@SUM(A1:A10)", csvText);
    }

    [Fact]
    public async Task CsvStreamingExporter_WithoutBomOrSanitization_PreservesRawText()
    {
        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Formula"] = "=SUM(A1:A2)" }
        };

        var options = new CsvExportOptions
        {
            IncludeUtf8Bom = false,
            SanitizeFormulas = false
        };

        using var memoryStream = new MemoryStream();
        await CsvStreamingExporter.ExportToStreamAsync(memoryStream, rows, options);

        var bytes = memoryStream.ToArray();
        Assert.NotEqual(0xEF, bytes[0]); // No BOM

        var csvText = Encoding.UTF8.GetString(bytes);
        Assert.Contains("=SUM(A1:A2)", csvText);
        Assert.DoesNotContain("'=SUM(A1:A2)", csvText);
    }

    [Fact]
    public async Task MiniExcelReportExporter_WithPasswordProtection_GeneratesValidPackage()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Financial Ledger" },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 30,
                    Elements = new List<ElementDefinition>
                    {
                        new() { Type = ElementType.Text, X = 10, Y = 10, Text = "Account Code" },
                        new() { Type = ElementType.Text, X = 100, Y = 10, Text = "Balance" }
                    }
                }
            }
        };

        var dataRows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Account Code"] = "1001", ["Balance"] = 50000 }
        };

        var options = new ExcelExportOptions
        {
            Password = "CompanySecretPassword123",
            ProtectSheets = true,
            EnableNativeFormulas = true
        };

        using var memoryStream = new MemoryStream();
        await MiniExcelReportExporter.ExportReportToExcelAsync(report, dataRows, memoryStream, sheetName: "Ledger", options: options);

        var bytes = memoryStream.ToArray();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // ZIP header check for XLSX package (PK\x03\x04)
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
        Assert.Equal(0x03, bytes[2]);
        Assert.Equal(0x04, bytes[3]);
    }
}
