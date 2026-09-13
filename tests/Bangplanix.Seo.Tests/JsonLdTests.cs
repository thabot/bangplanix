using System;
using System.Collections.Generic;
using Bangplanix.Engine.Seo;
using Xunit;

namespace Bangplanix.Seo.Tests;

public class JsonLdTests
{
    [Fact]
    public void GenerateJsonLdInvoiceShouldContainCorrectSchemaOrgProperties()
    {
        var metadata = new ReportSeoMetadata
        {
            Title = "Tax Invoice INV-9001",
            Description = "Commercial tax invoice for software license",
            Author = "Bangplanix Inc.",
            SchemaType = "Invoice",
            CanonicalUrl = "https://example.com/invoices/9001",
            PublishedAt = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc)
        };

        var data = new Dictionary<string, object>
        {
            ["invoiceNumber"] = "INV-9001",
            ["totalAmount"] = 99000,
            ["currency"] = "THB",
            ["customerName"] = "Enterprise Client Ltd."
        };

        var jsonLd = JsonLdGenerator.GenerateJsonLd(metadata, data);

        Assert.Contains("\"@context\": \"https://schema.org\"", jsonLd, StringComparison.Ordinal);
        Assert.Contains("\"@type\": \"Invoice\"", jsonLd, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"Tax Invoice INV-9001\"", jsonLd, StringComparison.Ordinal);
        Assert.Contains("\"identifier\": \"INV-9001\"", jsonLd, StringComparison.Ordinal);
        Assert.Contains("\"currency\": \"THB\"", jsonLd, StringComparison.Ordinal);
        Assert.Contains("99000", jsonLd, StringComparison.Ordinal);
        Assert.Contains("\"Enterprise Client Ltd.\"", jsonLd, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateJsonLdReportShouldDefaultToReportType()
    {
        var metadata = new ReportSeoMetadata
        {
            Title = "Quarterly Sales Analytics",
            Description = "Q3 Summary report",
            Author = "Analytics Team",
            SchemaType = "Report"
        };

        var jsonLd = JsonLdGenerator.GenerateJsonLd(metadata);

        Assert.Contains("\"@type\": \"Report\"", jsonLd, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"Quarterly Sales Analytics\"", jsonLd, StringComparison.Ordinal);
    }
}
