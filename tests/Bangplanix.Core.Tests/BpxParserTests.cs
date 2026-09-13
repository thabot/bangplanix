using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Core.Parser;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Core.Tests;

public class BpxParserTests
{
    [Fact]
    public void Parse_SampleInvoiceTemplate_ShouldSucceedWithCorrectProperties()
    {
        // Arrange
        var samplePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "schema", "v1", "samples", "invoice.bpx");
        if (!File.Exists(samplePath))
        {
            // Fallback for different working directories
            samplePath = Path.GetFullPath("../../../../../schema/v1/samples/invoice.bpx");
        }

        var json = File.ReadAllText(samplePath);

        // Act
        var report = BpxParser.Parse(json);

        // Assert
        report.Should().NotBeNull();
        report.Version.Should().Be("1.0");
        report.Metadata.Title.Should().Be("Commercial Tax Invoice");
        report.PageSetup.PaperKind.Should().Be(PaperKind.A4);
        report.PageSetup.Orientation.Should().Be(PageOrientation.Portrait);
        report.PageSetup.Margins.Top.Should().Be(15.0);

        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("InvoiceNo");
        report.Parameters[0].Type.Should().Be(ParameterType.String);

        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("InvoiceItems");
        report.Datasets[0].Type.Should().Be(DatasetType.Static);

        report.Styles.Should().ContainKey("TitleStyle");
        report.Styles["TitleStyle"].FontFamily.Should().Be("Prompt");
        report.Styles["TitleStyle"].FontSize.Should().Be(18.0);

        report.Bands.ReportHeader.Should().NotBeNull();
        report.Bands.ReportHeader!.Elements.Should().HaveCount(2);
        report.Bands.ReportHeader.Elements[0].Type.Should().Be(ElementType.Text);
        report.Bands.ReportHeader.Elements[1].Type.Should().Be(ElementType.Barcode);

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_FromUtf8Bytes_ShouldMatchStringParse()
    {
        // Arrange
        var samplePath = Path.GetFullPath("../../../../../schema/v1/samples/invoice.bpx");
        var bytes = File.ReadAllBytes(samplePath);

        // Act
        var report = BpxParser.Parse(bytes.AsSpan());

        // Assert
        report.Should().NotBeNull();
        report.Metadata.Title.Should().Be("Commercial Tax Invoice");
    }

    [Fact]
    public void Roundtrip_SerializeAndDeserialize_ShouldPreserveIntegrity()
    {
        // Arrange
        var original = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata { Title = "Roundtrip Test", Author = "Unit Test" },
            PageSetup = new PageSetup { PaperKind = PaperKind.A4, Orientation = PageOrientation.Landscape }
        };

        // Act
        var json = BpxParser.ToJson(original);
        var restored = BpxParser.Parse(json);

        // Assert
        restored.Metadata.Title.Should().Be("Roundtrip Test");
        restored.PageSetup.PaperKind.Should().Be(PaperKind.A4);
        restored.PageSetup.Orientation.Should().Be(PageOrientation.Landscape);
    }

    [Fact]
    public void Parse_InvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ invalid json string ";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => BpxParser.Parse(invalidJson));
    }
}
