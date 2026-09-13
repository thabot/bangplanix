using Bangplanix.Core.Models;
using Bangplanix.Printing.Zpl;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Printing.Tests;

public class ZplTests
{
    [Fact]
    public void ZplGenerator_BasicCommands_ShouldProduceValidZplII()
    {
        var gen = new ZplGenerator(ZplDpi.Dpi203);
        gen.Text(50, 50, "LOGISTICS SHIPPING LABEL", fontHeightDots: 35)
           .Line(50, 95, 400, thickness: 3)
           .Box(50, 110, 400, 150, borderThickness: 2)
           .Barcode128(60, 280, "TH-987654321", height: 70)
           .QrCode(280, 280, "https://track.example.com/TH987654321", magnification: 6)
           .EndLabel();

        var zpl = gen.Build();
        zpl.Should().StartWith("^XA");
        zpl.TrimEnd().Should().EndWith("^XZ");
        zpl.Should().Contain("^FO50,50^A0N,35,35^FDLOGISTICS SHIPPING LABEL^FS");
        zpl.Should().Contain("^FO50,95^GB400,3,3^FS");
        zpl.Should().Contain("^FO50,110^GB400,150,2,B,0^FS");
        zpl.Should().Contain("^FO60,280^BY2^BCN,70,Y,N,N^FDTH-987654321^FS");
        zpl.Should().Contain("^FO280,280^BQN,2,6,M^FDQA,https://track.example.com/TH987654321^FS");
    }

    [Fact]
    public void ZplGenerator_DpiScaling_ShouldConvertCorrectly()
    {
        var gen203 = new ZplGenerator(ZplDpi.Dpi203);
        gen203.MmToDots(10).Should().Be(80); // 10mm * 8 dots/mm = 80 dots

        var gen300 = new ZplGenerator(ZplDpi.Dpi300);
        gen300.MmToDots(10).Should().Be(120); // 10mm * 12 dots/mm = 120 dots

        var gen600 = new ZplGenerator(ZplDpi.Dpi600);
        gen600.MmToDots(10).Should().Be(240); // 10mm * 24 dots/mm = 240 dots
    }

    [Fact]
    public void ZplReportRenderer_ShouldRenderReportToZpl()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Warehouse Label" },
            Bands = new BandsDefinition
            {
                PageHeader = new BandDefinition
                {
                    Height = 20,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "PALLET TAG #402", X = 10, Y = 5, Width = 100, Height = 10 }]
                },
                Detail = new BandDefinition
                {
                    Height = 40,
                    Elements =
                    [
                        new ElementDefinition { Type = ElementType.Text, Expression = "=Fields.SKU", X = 10, Y = 0, Width = 60, Height = 10 },
                        new ElementDefinition { Type = ElementType.Barcode, Text = "SKU-999-AAA", X = 10, Y = 12, Width = 80, Height = 25 }
                    ]
                }
            }
        };

        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["SKU"] = "PROD-A-001" }
        };

        var zpl = ZplReportRenderer.RenderToZpl(report, rows, ZplDpi.Dpi203);
        zpl.Should().NotBeNullOrWhiteSpace();
        zpl.Should().Contain("^XA");
        zpl.Should().Contain("PALLET TAG #402");
        zpl.Should().Contain("PROD-A-001");
        zpl.Should().Contain("^BC");
        zpl.Should().Contain("^XZ");
    }
}
