using Bangplanix.Adapters.DevExpress;
using Bangplanix.Adapters.FastReport;
using Bangplanix.Adapters.Jaspersoft;
using Bangplanix.Adapters.Office;
using Bangplanix.Adapters.Ssrs;
using Bangplanix.Adapters.Stimulsoft;
using Bangplanix.Adapters.Telerik;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

#pragma warning disable CA1707, CA2007

namespace Bangplanix.Adapters.Tests;

public class LegacyParityTests
{
    [Fact]
    public void SsrsToBpxParity_ShouldMaintainPageDimensionsAndBandHierarchy()
    {
        var rdlXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report xmlns="http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition">
          <Description>Quarterly Sales</Description>
          <Page>
            <PageWidth>8.5in</PageWidth>
            <PageHeight>11in</PageHeight>
            <PageHeader>
              <Height>1in</Height>
              <ReportItems>
                <Textbox Name="HeaderTitle">
                  <Value>Quarterly Header</Value>
                </Textbox>
              </ReportItems>
            </PageHeader>
            <PageFooter>
              <Height>0.8in</Height>
            </PageFooter>
          </Page>
          <Body>
            <Height>2in</Height>
            <ReportItems>
              <Textbox Name="TitleBox">
                <Value>="Quarterly Report: " &amp; Parameters!Quarter.Value</Value>
              </Textbox>
            </ReportItems>
          </Body>
        </Report>
        """;

        var adapter = new SsrsRdlAdapter();
        var report = adapter.Convert(rdlXml);

        report.Metadata.Title.Should().Be("Quarterly Sales");
        report.PageSetup.Width.Should().BeApproximately(612.0, 0.1);
        report.PageSetup.Height.Should().BeApproximately(792.0, 0.1);
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageFooter.Should().NotBeNull();
        report.Bands.Detail.Should().NotBeNull();
    }

    [Fact]
    public void JaspersoftToBpxParity_ShouldTranspileJavaExpressionsToBpx()
    {
        var jrxml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <jasperReport xmlns="http://jasperreports.sourceforge.net/jasperreports" pageWidth="595" pageHeight="842">
          <detail>
            <band height="40">
              <textField>
                <reportElement x="10" y="5" width="200" height="25"/>
                <textFieldExpression><![CDATA[$F{ProductName}.toUpperCase()]]></textFieldExpression>
              </textField>
            </band>
          </detail>
        </jasperReport>
        """;

        var adapter = new JaspersoftJrxmlAdapter();
        var report = adapter.Convert(jrxml);

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().NotBeEmpty();
        report.Bands.Detail.Elements[0].Expression.Should().Contain("ProductName");
    }

    [Fact]
    public void FastReportToBpxParity_ShouldConvertMillimetersToPointsAccurately()
    {
        var frxXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Report>
          <ReportPage Name="Page1" PaperWidth="210" PaperHeight="297">
            <ReportTitleBand Name="ReportTitle1" Height="37.8">
              <TextObject Name="Text1" Left="18.9" Top="9.45" Width="170.1" Height="18.9" Text="Tax Invoice"/>
            </ReportTitleBand>
          </ReportPage>
        </Report>
        """;

        var adapter = new FastReportFrxAdapter();
        var report = adapter.Convert(frxXml);

        report.PageSetup.Width.Should().BeInRange(590, 600);
        report.PageSetup.Height.Should().BeInRange(835, 845);
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Tax Invoice");
    }

    [Fact]
    public void StimulsoftToBpxParity_ShouldPreserveJsonDictionaryAndBands()
    {
        var mrtJson = """
        {
          "ReportVersion": "2024.1.1",
          "Pages": {
            "0": {
              "Ident": "StiPage",
              "Name": "Page1",
              "PageWidth": 21.0,
              "PageHeight": 29.7,
              "Components": {
                "0": {
                  "Ident": "StiHeaderBand",
                  "Name": "HeaderBand1",
                  "ClientRectangle": "0,0.4,19.0,1.2",
                  "Components": {
                    "0": {
                      "Ident": "StiText",
                      "Name": "Text1",
                      "ClientRectangle": "0.2,0.2,8.0,0.8",
                      "Text": {"Value": "Monthly Summary"}
                    }
                  }
                }
              }
            }
          }
        }
        """;

        var adapter = new StimulsoftMrtAdapter();
        var report = adapter.Convert(mrtJson);

        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Monthly Summary");
    }

    [Fact]
    public void DevExpressToBpxParity_ShouldConvertRepxXmlCorrectly()
    {
        var repx = """
        <?xml version="1.0" encoding="utf-8"?>
        <XtraReportsLayoutSerializer>
          <Bands>
            <Item1 Ref="1" ControlType="DetailBand" Name="Detail" HeightF="30">
              <Controls>
                <Item1 Ref="2" ControlType="XRLabel" Name="label1" Text="Total Amount" BoundsF="10, 5, 100, 20" />
              </Controls>
            </Item1>
          </Bands>
        </XtraReportsLayoutSerializer>
        """;

        var adapter = new DevExpressRepxAdapter();
        var report = adapter.Convert(repx);

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Text.Should().Be("Total Amount");
    }
}
