using System;
using System.IO;
using System.Linq;
using Bangplanix.Adapters.BarTender;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class BarTenderAdapterTests
{
    private readonly BarTenderBtwAdapter _adapter = new();

    private const string SampleJsonBtw = """
        {
          "name": "ShippingLabel_100x50",
          "labelSetup": {
            "width": "100mm",
            "height": "50mm"
          },
          "objects": [
            {
              "type": "text",
              "x": "5mm",
              "y": "5mm",
              "width": "90mm",
              "height": "10mm",
              "value": "BANGPLANIX LOGISTICS",
              "font": { "family": "Arial", "size": 12, "bold": true }
            },
            {
              "type": "barcode",
              "symbology": "code128",
              "x": "5mm",
              "y": "18mm",
              "width": "60mm",
              "height": "20mm",
              "dataSource": "%TrackingNumber%"
            },
            {
              "type": "barcode",
              "symbology": "qr",
              "x": "70mm",
              "y": "18mm",
              "width": "20mm",
              "height": "20mm",
              "dataSource": "[Database.QrPayload]"
            }
          ]
        }
        """;

    private const string SampleXmlBtw = """
        <?xml version="1.0" encoding="utf-8"?>
        <BarTenderFormat name="PalletTag">
          <Media Width="100mm" Height="150mm" />
          <Objects>
            <Text Left="10mm" Top="10mm" Width="80mm" Height="15mm" Value="WAREHOUSE PALLET #%PalletID%" />
            <Barcode Symbology="Code128" Left="10mm" Top="30mm" Width="80mm" Height="25mm" Field="PalletBarcode" />
          </Objects>
        </BarTenderFormat>
        """;

    [Fact]
    public void Convert_FromJsonTemplate_ShouldParseLabelPropertiesAndObjects()
    {
        var report = _adapter.Convert(SampleJsonBtw);

        report.Should().NotBeNull();
        report.Metadata.Title.Should().Be("ShippingLabel_100x50");
        report.PageSetup.Width.Should().BeGreaterThan(250); // ~283.46 pt
        report.PageSetup.Height.Should().BeGreaterThan(130); // ~141.73 pt

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(3);

        var titleElem = report.Bands.Detail.Elements[0];
        titleElem.Type.Should().Be(ElementType.Text);
        titleElem.Text.Should().Be("BANGPLANIX LOGISTICS");

        var bcElem = report.Bands.Detail.Elements[1];
        bcElem.Type.Should().Be(ElementType.Barcode);
        bcElem.BarcodeType.Should().Be(BarcodeType.Code128);
        bcElem.Expression.Should().Contain("Fields.TrackingNumber");

        var qrElem = report.Bands.Detail.Elements[2];
        qrElem.Type.Should().Be(ElementType.QrCode);
        qrElem.Expression.Should().Contain("Fields.QrPayload");
    }

    [Fact]
    public void Convert_FromXmlTemplate_ShouldParseXmlLabelFormat()
    {
        var report = _adapter.Convert(SampleXmlBtw);

        report.Should().NotBeNull();
        report.Metadata.Title.Should().Be("PalletTag");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);

        var textElem = report.Bands.Detail.Elements[0];
        textElem.Expression.Should().Contain("Fields.PalletID");

        var bcElem = report.Bands.Detail.Elements[1];
        bcElem.Type.Should().Be(ElementType.Barcode);
        bcElem.Expression.Should().Contain("Fields.PalletBarcode");
    }

    [Fact]
    public void LegacyAdapterFactory_ShouldDetectBarTenderFormat()
    {
        var adapter = LegacyAdapterFactory.GetAdapterByExtension("shipping.btw");
        adapter.Should().BeOfType<BarTenderBtwAdapter>();

        var detected = LegacyAdapterFactory.DetectAdapter(SampleXmlBtw);
        detected.Should().BeOfType<BarTenderBtwAdapter>();
    }
}
