using Bangplanix.Core.Models;
using Bangplanix.Engine.Accessibility;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class MatterhornAccessibilityProtocolTests
{
    [Fact]
    public void Matterhorn_Checkpoint01_RealContentVsArtifact_ShouldTagCorrectly()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Matterhorn Accessibility Audit Report" },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 50,
                    Elements =
                    [
                        new() { Type = ElementType.Text, Text = "Enterprise Annual Report" },
                        new() { Type = ElementType.Shape, Width = 500, Height = 2 } // Decorative line
                    ]
                }
            }
        };

        var root = PdfUaTagEngine.BuildStructureTreeFromReport(report);

        Assert.NotNull(root);
        Assert.Equal(PdfTagType.Document, root.TagType);

        var section = root.Children[0];
        Assert.Equal(PdfTagType.Section, section.TagType);

        // Heading 1 for title
        Assert.Contains(section.Children, c => c.TagType == PdfTagType.H1 && c.ActualText == "Enterprise Annual Report");

        // Decorative shape must be tagged as Artifact
        Assert.Contains(section.Children, c => c.TagType == PdfTagType.Artifact);
    }

    [Fact]
    public void Matterhorn_Checkpoint09_HeadingsHierarchy_ShouldFollowLogicalSequence()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Hierarchical Document" },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Elements = [new() { Type = ElementType.Text, Text = "Document Level 1 Title" }]
                },
                PageHeader = new BandDefinition
                {
                    Elements = [new() { Type = ElementType.Text, Text = "Section Level 2 Header" }]
                }
            }
        };

        var root = PdfUaTagEngine.BuildStructureTreeFromReport(report);

        var h1Node = root.Children[0].Children.FirstOrDefault(c => c.TagType == PdfTagType.H1);
        var h2Node = root.Children[1].Children.FirstOrDefault(c => c.TagType == PdfTagType.H2);

        Assert.NotNull(h1Node);
        Assert.NotNull(h2Node);
        Assert.Equal("Document Level 1 Title", h1Node.ActualText);
        Assert.Equal("Section Level 2 Header", h2Node.ActualText);
    }

    [Fact]
    public void Matterhorn_Checkpoint13_GraphicsAltText_ShouldProvideMeaningfulDescriptions()
    {
        var report = new ReportDefinition
        {
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Elements =
                    [
                        new()
                        {
                            Type = ElementType.Chart,
                            Chart = new ChartDefinition { Title = "Q4 Regional Sales Trends" }
                        },
                        new()
                        {
                            Type = ElementType.Barcode,
                            Text = "INV-2026-998811"
                        }
                    ]
                }
            }
        };

        var root = PdfUaTagEngine.BuildStructureTreeFromReport(report);
        var tableSec = root.Children[0];
        var table = tableSec.Children[0];
        var row = table.Children[0];

        var figureNodes = row.Children.SelectMany(td => td.Children).Where(c => c.TagType == PdfTagType.Figure).ToList();

        Assert.Equal(2, figureNodes.Count);
        Assert.Contains(figureNodes, f => f.AltText != null && f.AltText.Contains("Q4 Regional Sales Trends", StringComparison.Ordinal));
        Assert.Contains(figureNodes, f => f.AltText != null && f.AltText.Contains("INV-2026-998811", StringComparison.Ordinal));
    }
}
