using System.IO;
using System.Text.Json;
using Bangplanix.Core.Models;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class StarterTemplatesTests
{
    private static readonly string TemplatesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "templates");

    [Theory]
    [InlineData("thai-etax-invoice.bpx")]
    [InlineData("pos-thermal-receipt-80mm.bpx")]
    [InlineData("shipping-logistics-label-4x6.bpx")]
    [InlineData("employee-payslip.bpx")]
    [InlineData("commercial-invoice-ubl.bpx")]
    public void StarterTemplate_MustBeValidJsonAndParseProperly(string templateFileName)
    {
        string filePath = Path.Combine(TemplatesDir, templateFileName);
        Assert.True(File.Exists(filePath), $"Template file must exist: {filePath}");

        string json = File.ReadAllText(filePath);
        Assert.NotEmpty(json);

        // Verify standard JSON deserialization
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);

        Assert.True(doc.RootElement.TryGetProperty("version", out _));
        Assert.True(doc.RootElement.TryGetProperty("bands", out var bands));
        Assert.True(bands.TryGetProperty("title", out _));
    }
}
