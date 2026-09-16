using System.Text;
using Bangplanix.Client;
using Bangplanix.Connectors.Excel;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using Bangplanix.Engine.Pdf;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class ExecutionModesTests
{
    private static string GetTemplatePath(string relativePath)
    {
        var samplePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath);
        if (!File.Exists(samplePath))
        {
            samplePath = Path.GetFullPath(Path.Combine("../../../../../", relativePath));
        }
        return samplePath;
    }

    [Fact]
    public async Task Mode1_InProcess_RenderToPdf_DirectLibraryUsage_ShouldSucceed()
    {
        // Arrange - Load template and data without any server or container (Like QuestPDF)
        var templatePath = GetTemplatePath("schema/v1/samples/invoice.bpx");
        var json = await File.ReadAllTextAsync(templatePath);
        var report = BpxParser.Parse(json);
        report.Should().NotBeNull();

        var dataJson = "[{\"itemNo\":1,\"description\":\"Server Rack\",\"qty\":2,\"unitPrice\":1500,\"amount\":3000}]";
        var dataRows = JsonPushStreamConnector.ParseJsonStringToRows(dataJson);

        // Act - In-Process Vector PDF Rendering via SkiaSharp
        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report, null, dataRows);

        // Assert - PDF Header %PDF- and content verification
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
        var header = Encoding.ASCII.GetString(pdfBytes.AsSpan(0, 8));
        header.Should().StartWith("%PDF-");
    }

    [Fact]
    public async Task Mode1_InProcess_ExportToExcel_DirectLibraryUsage_ShouldSucceed()
    {
        // Arrange
        var templatePath = GetTemplatePath("schema/v1/samples/invoice.bpx");
        var json = await File.ReadAllTextAsync(templatePath);
        var report = BpxParser.Parse(json);
        var dataJson = "[{\"itemNo\":1,\"description\":\"Cloud Node\",\"qty\":10,\"unitPrice\":250,\"amount\":2500}]";
        var dataRows = JsonPushStreamConnector.ParseJsonStringToRows(dataJson);

        // Act - In-Process XLSX Stream Generation
        using var memoryStream = new MemoryStream();
        await MiniExcelReportExporter.ExportReportToExcelAsync(report, dataRows, memoryStream);

        // Assert - XLSX output stream verification (PK zip header)
        var xlsxBytes = memoryStream.ToArray();
        xlsxBytes.Length.Should().BeGreaterThan(200);
        xlsxBytes[0].Should().Be(0x50); // 'P'
        xlsxBytes[1].Should().Be(0x4B); // 'K'
    }

    [Fact]
    public async Task Mode2_Cli_RenderPdfAndXlsx_ShouldExecuteWithoutDocker()
    {
        // Arrange - Simulate CLI Batch Pipeline
        var templatePath = GetTemplatePath("schema/v1/samples/invoice.bpx");
        var templateJson = await File.ReadAllTextAsync(templatePath);
        var report = BpxParser.Parse(templateJson);

        var tempPdf = Path.Combine(Path.GetTempPath(), $"cli_test_{Guid.NewGuid():N}.pdf");
        var tempXlsx = Path.Combine(Path.GetTempPath(), $"cli_test_{Guid.NewGuid():N}.xlsx");

        try
        {
            // Act 1: CLI PDF Render
            var renderer = new SkiaPdfRenderer();
            var pdfBytes = await renderer.RenderToPdfAsync(report);
            await File.WriteAllBytesAsync(tempPdf, pdfBytes);

            // Act 2: CLI XLSX Render
            using (var fs = File.Create(tempXlsx))
            {
                await MiniExcelReportExporter.ExportReportToExcelAsync(report, Array.Empty<IDictionary<string, object?>>(), fs);
            }

            // Assert
            File.Exists(tempPdf).Should().BeTrue();
            new FileInfo(tempPdf).Length.Should().BeGreaterThan(500);

            File.Exists(tempXlsx).Should().BeTrue();
            new FileInfo(tempXlsx).Length.Should().BeGreaterThan(200);
        }
        finally
        {
            if (File.Exists(tempPdf)) File.Delete(tempPdf);
            if (File.Exists(tempXlsx)) File.Delete(tempXlsx);
        }
    }

    [Fact]
    public async Task Mode2_Cli_ValidateTemplate_ShouldPassValidBpx()
    {
        // Arrange
        var templatePath = GetTemplatePath("schema/v1/samples/invoice.bpx");
        var json = await File.ReadAllTextAsync(templatePath);

        // Act
        var report = BpxParser.Parse(json);

        // Assert
        report.Version.Should().Be("1.0");
        report.PageSetup.Should().NotBeNull();
        report.Bands.Should().NotBeNull();
    }

    [Fact]
    public async Task Mode3_Microservice_ClientSdk_ReturnTypes_FilePath_Stream_Base64_ShouldSucceed()
    {
        // Arrange - Simulate Client SDK RenderResult
        var sampleBytes = Encoding.UTF8.GetBytes("%PDF-1.7 Simulated enterprise report payload for SDK verification");
        var renderResult = new RenderResult(
            Data: sampleBytes,
            Format: "pdf",
            ContentType: "application/pdf",
            CorrelationId: "req-12345",
            DurationMs: 15
        );

        // Act 1: Base64 Return
        var base64String = renderResult.ToBase64();

        // Act 2: Stream Return
        using var stream = renderResult.ToStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        // Act 3: FilePath Save
        var tempFile = Path.Combine(Path.GetTempPath(), $"sdk_test_{Guid.NewGuid():N}.pdf");
        try
        {
            await renderResult.SaveToFileAsync(tempFile);

            // Assert
            base64String.Should().NotBeNullOrEmpty();
            Convert.FromBase64String(base64String).Should().Equal(sampleBytes);

            ms.ToArray().Should().Equal(sampleBytes);

            File.Exists(tempFile).Should().BeTrue();
            (await File.ReadAllBytesAsync(tempFile)).Should().Equal(sampleBytes);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
