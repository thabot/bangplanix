using System;
using System.IO;
using System.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class RealFileConversionIntegrationTests
{
    private static string GetSamplesDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var directPath = Path.Combine(baseDir, "Samples");
        if (Directory.Exists(directPath)) return directPath;

        // Fallback to source directory if running in IDE without output copy
        var current = new DirectoryInfo(baseDir);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "tests", "Bangplanix.Adapters.Tests", "Samples");
            if (Directory.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Samples directory not found.");
    }

    [Fact]
    public void ConvertFile_ShouldSuccessfullyConvertAllSampleFiles()
    {
        var samplesDir = GetSamplesDirectory();
        var files = Directory.GetFiles(samplesDir);
        files.Should().NotBeEmpty("Samples directory should contain standard test files");

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var adapter = LegacyAdapterFactory.GetAdapterByExtension(file);
            adapter.Should().NotBeNull($"An adapter must exist for '{fileName}'");

            ReportDefinition report;
            using (var stream = File.OpenRead(file))
            {
                report = adapter.Convert(stream);
            }

            report.Should().NotBeNull($"Report converted from '{fileName}' must not be null");
            report.Metadata.Should().NotBeNull($"Metadata for '{fileName}' must exist");
            report.PageSetup.Should().NotBeNull($"PageSetup for '{fileName}' must exist");
            report.Bands.Should().NotBeNull($"Bands for '{fileName}' must exist");

            // Ensure at least one band is populated (PageHeader, ReportHeader, Detail, etc.)
            var hasAnyElements = (report.Bands.Detail?.Elements.Count ?? 0) > 0 ||
                                 (report.Bands.PageHeader?.Elements.Count ?? 0) > 0 ||
                                 (report.Bands.ReportHeader?.Elements.Count ?? 0) > 0 ||
                                 (report.Bands.PageFooter?.Elements.Count ?? 0) > 0;

            hasAnyElements.Should().BeTrue($"Report converted from '{fileName}' should contain report elements");
        }
    }

    [Theory]
    [InlineData("sample_ssrs.rdl", "SSRS", "Enterprise Sales Performance")]
    [InlineData("sample_ssrs.rdlc", "SSRS", "Enterprise Sales Performance")]
    [InlineData("sample_jaspersoft.jrxml", "Jaspersoft", "Staff Directory Report")]
    [InlineData("sample_fastreport.frx", "FastReport", "Commercial Tax Invoice")]
    [InlineData("sample_stimulsoft.mrt", "Stimulsoft", "Executive Performance KPI")]
    [InlineData("sample_telerik.trdx", "Telerik", "Warehouse Stock Balance")]
    [InlineData("sample_telerik.trdp", "Telerik", "Warehouse Stock Balance")]
    [InlineData("sample_devexpress.repx", "DevExpress", "Regional Sales Summary")]
    [InlineData("sample_crystal.rpt.xml", "Crystal", "Monthly Sales Ledger 2026")]
    [InlineData("sample_crystal.crystal.xml", "Crystal", "Monthly Sales Ledger 2026")]
    [InlineData("sample_birt.rptdesign", "BIRT", "Department Financials")]
    [InlineData("sample_birt.birt.xml", "BIRT", "Department Financials")]
    [InlineData("sample_activereports.rdlx", "ActiveReports", "ActiveReports Invoice")]
    [InlineData("sample_activereports.rpx", "ActiveReports", "ActiveReports Invoice")]
    [InlineData("sample_activereports.ar.xml", "ActiveReports", "ActiveReports Invoice")]
    [InlineData("sample_oracle.rex", "Oracle", "Oracle General Ledger")]
    [InlineData("sample_oracle.oracle.xml", "Oracle", "Oracle General Ledger")]
    [InlineData("sample_pentaho.prpt", "Pentaho", "Pentaho Sales Analysis")]
    [InlineData("sample_pentaho.prpt.xml", "Pentaho", "Pentaho Sales Analysis")]
    [InlineData("sample_oraclebip.rtf", "Oracle BIP", "Oracle BI Publisher Report")]
    [InlineData("sample_cognos.spec", "Cognos", "CognosFinancialSummary")]
    [InlineData("sample_cognos.cognos.xml", "Cognos", "CognosFinancialSummary")]
    [InlineData("sample_template.hbs", "Handlebars", "Payroll Summary")]
    [InlineData("sample_template.mustache", "Handlebars", "Payroll Summary")]
    [InlineData("sample_invoice.ubl.xml", "UBL", "INV-2026-9901")]
    [InlineData("sample_invoice.ubl", "UBL", "INV-2026-9901")]
    [InlineData("sample_zebra.zpl", "Zebra", "SHIPPING LABEL")]
    [InlineData("sample_eltron.epl", "EPL", "PACKAGE RECEIPT")]
    [InlineData("sample_receipt.escpos", "ESC/POS", "STORE RECEIPT")]
    [InlineData("sample_receipt.pos", "ESC/POS", "STORE RECEIPT")]
    [InlineData("sample_adobe.xdp", "Adobe XDP", "PURCHASE ORDER")]
    [InlineData("sample_bartender.btw.json", "BarTender", "BANGPLANIX LOGISTICS")]
    [InlineData("sample_bartender.btw.xml", "BarTender", "PalletTag")]
    [InlineData("sample_access.access.txt", "MS Access", "OFFICIAL TAX INVOICE")]
    [InlineData("sample_web.html", "HTML", "Customer Statement")]
    [InlineData("sample_web.liquid", "Liquid", "Customer Statement")]
    public void ConvertFile_SpecificFormatSamples_ShouldMatchExpectedTextOrTitle(string sampleFile, string formatTag, string expectedKeyword)
    {
        var samplesDir = GetSamplesDirectory();
        var filePath = Path.Combine(samplesDir, sampleFile);
        File.Exists(filePath).Should().BeTrue($"Sample file '{sampleFile}' must exist on disk");

        var report = LegacyAdapterFactory.ConvertFile(filePath);
        report.Should().NotBeNull();

        // Check if expected keyword matches metadata title or any band element text
        var titleMatches = report.Metadata.Title?.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase) ?? false;
        var headerMatches = report.Bands.PageHeader?.Elements.Any(e => e.Text?.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase) == true) ?? false;
        var reportHeaderMatches = report.Bands.ReportHeader?.Elements.Any(e => e.Text?.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase) == true) ?? false;
        var detailMatches = report.Bands.Detail?.Elements.Any(e => (e.Text?.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase) == true) || (e.Expression?.Contains(expectedKeyword, StringComparison.OrdinalIgnoreCase) == true)) ?? false;

        var matched = titleMatches || headerMatches || reportHeaderMatches || detailMatches;
        matched.Should().BeTrue($"Report from '{sampleFile}' ({formatTag}) should contain '{expectedKeyword}' in title or elements");
    }
}
