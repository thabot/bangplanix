using Bangplanix.Core.Models;
using Bangplanix.Engine.Accessibility;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PdfUaComplianceTests
{
    [Fact]
    public void PdfUaTagEngine_ShouldBuildStandardStructureTree()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Compliance Check Report", Author = "Auditor" },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 40,
                    Elements = new List<ElementDefinition>
                    {
                        new() { Type = ElementType.Text, Text = "1. Financial Compliance" }
                    }
                },
                Detail = new BandDefinition
                {
                    Height = 30,
                    Elements = new List<ElementDefinition>
                    {
                        new() { Type = ElementType.Text, Text = "Row Item A" },
                        new() { Type = ElementType.Image, ImageSource = "chart.png" }
                    }
                }
            }
        };

        var options = new AccessibilityOptions
        {
            EnablePdfUa = true,
            PrimaryLanguage = "th-TH",
            DisplayDocTitle = true
        };

        var root = PdfUaTagEngine.BuildStructureTreeFromReport(report, options);

        Assert.NotNull(root);
        Assert.Equal(PdfTagType.Document, root.TagType);
        Assert.True(root.Children.Count > 0);
    }

    [Fact]
    public void PdfAccessibilityValidator_ShouldAuditMissingTitleAndLanguage()
    {
        var root = new PdfStructElement { TagType = PdfTagType.Document };
        var invalidOptions = new AccessibilityOptions
        {
            DocumentTitle = "", // Missing title
            PrimaryLanguage = "" // Missing language
        };

        var auditReport = PdfAccessibilityValidator.Validate(root, invalidOptions);

        Assert.NotNull(auditReport);
        Assert.False(auditReport.IsCompliant);
        Assert.Contains(auditReport.Violations, v => v.RuleId == "PDFUA-TITLE-001");
        Assert.Contains(auditReport.Violations, v => v.RuleId == "PDFUA-LANG-001");
    }

    [Fact]
    public void ColorContrastAuditor_ShouldVerifyWcag21AaContrastRatios()
    {
        // High contrast: Black on White (Ratio 21:1)
        var highContrast = ColorContrastAuditor.CalculateContrast("#000000", "#FFFFFF");
        Assert.True(highContrast.Ratio >= 7.0);
        Assert.True(highContrast.PassesAaNormal);

        // Low contrast: Light gray on White (Ratio < 3:1)
        var lowContrast = ColorContrastAuditor.CalculateContrast("#CCCCCC", "#FFFFFF");
        Assert.True(lowContrast.Ratio < 3.0);
        Assert.False(lowContrast.PassesAaNormal);
    }

    [Fact]
    public void PdfReadingOrderOptimizer_ShouldSortElementsInNaturalVisualReadingFlow()
    {
        var root = new PdfStructElement
        {
            TagType = PdfTagType.Document,
            Children = new List<PdfStructElement>
            {
                new() { TagType = PdfTagType.Paragraph, ActualText = "Paragraph 2", X = 50, Y = 120 },
                new() { TagType = PdfTagType.H1, ActualText = "Header 1", X = 50, Y = 20 },
                new() { TagType = PdfTagType.Paragraph, ActualText = "Paragraph 1", X = 50, Y = 70 },
                new() { TagType = PdfTagType.Paragraph, ActualText = "Footer Note", X = 50, Y = 750 }
            }
        };

        var optimizedRoot = PdfReadingOrderOptimizer.OptimizeReadingOrder(root);

        Assert.Equal("Header 1", optimizedRoot.Children[0].ActualText);
        Assert.Equal("Paragraph 1", optimizedRoot.Children[1].ActualText);
        Assert.Equal("Paragraph 2", optimizedRoot.Children[2].ActualText);
        Assert.Equal("Footer Note", optimizedRoot.Children[3].ActualText);
    }
}
