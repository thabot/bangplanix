using Bangplanix.Core.Models;
using Bangplanix.Printing.EscPos;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Printing.Tests;

public class EscPosTests
{
    [Fact]
    public void EscPosGenerator_BasicCommands_ShouldProduceCorrectByteSequence()
    {
        using var gen = EscPosGenerator.CreateUtf8();
        gen.Align(EscPosAlignment.Center)
           .Bold(true)
           .TextLine("STORE RECEIPT")
           .Bold(false)
           .TwoColumns("Item A", "$10.00", 20)
           .OpenCashDrawer()
           .Cut(EscPosCutType.Partial);

        var bytes = gen.ToByteArray();
        bytes.Should().NotBeEmpty();

        // Check ESC @ (Init)
        bytes[0].Should().Be(0x1B);
        bytes[1].Should().Be(0x40);

        // Check ESC a 1 (Align Center)
        bytes.Should().ContainInOrder([0x1B, 0x61, 0x01]);

        // Check ESC E 1 (Bold On)
        bytes.Should().ContainInOrder([0x1B, 0x45, 0x01]);

        // Check ESC p 0 (Cash Drawer)
        bytes.Should().ContainInOrder([0x1B, 0x70, 0x00]);

        // Check GS V (Cut)
        bytes.Should().ContainInOrder([0x1D, 0x56, 66, 0x00]);
    }

    [Fact]
    public void EscPosGenerator_ThaiEncoding_ShouldEncodeCodepage874()
    {
        using var gen = EscPosGenerator.CreateThai();
        gen.TextLine("ใบเสร็จรับเงิน");

        var bytes = gen.ToByteArray();
        bytes.Should().NotBeEmpty();
        bytes.Should().ContainInOrder([0x1B, 0x74, 21]); // Code page 874 command
    }

    [Fact]
    public void EscPosGenerator_BarcodeAndQr_ShouldGenerateValidCommands()
    {
        using var gen = EscPosGenerator.CreateUtf8();
        gen.Barcode128("INV-2026-001")
           .QrCode("https://promptpay.io/0812345678/150.00");

        var bytes = gen.ToByteArray();

        // GS k 73 (Code128)
        bytes.Should().ContainInOrder([0x1D, 0x6B, 0x49]);

        // GS ( k (QR Code)
        bytes.Should().ContainInOrder([0x1D, 0x28, 0x6B]);
    }

    [Fact]
    public void EscPosReportRenderer_ShouldRenderReportDefinition()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "Fast Food POS" },
            Bands = new BandsDefinition
            {
                PageHeader = new BandDefinition
                {
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Branch 001 - Silom", Style = new StyleDefinition { Align = HorizontalAlign.Center } }]
                },
                Detail = new BandDefinition
                {
                    Elements = [new ElementDefinition { Type = ElementType.Text, Expression = "=Fields.ItemName" }]
                }
            }
        };

        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["ItemName"] = "Cheeseburger x 1" },
            new Dictionary<string, object?> { ["ItemName"] = "Coca Cola x 1" }
        };

        var bytes = EscPosReportRenderer.RenderToEscPos(report, rows);
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(50);
    }
}
