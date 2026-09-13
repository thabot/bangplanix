using System;
using System.IO;
using System.Linq;
using Bangplanix.Adapters.Adobe;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class AdobeXdpAdapterTests
{
    private readonly AdobeXdpAdapter _adapter = new();

    private const string SampleXdpXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <xdp:xdp xmlns:xdp="http://ns.adobe.com/xdp/">
          <template xmlns="http://www.xfa.org/schema/xfa-template/3.0/" name="PurchaseOrder">
            <pageSet>
              <pageArea name="Page1" id="Page1" w="210mm" h="297mm">
                <medium stock="a4" short="210mm" long="297mm"/>
                <margin topInset="15mm" bottomInset="15mm" leftInset="10mm" rightInset="10mm"/>
              </pageArea>
            </pageSet>
            <subform name="form1" layout="tb">
              <subform name="Header" layout="position" h="40mm">
                <draw name="lblTitle" x="10mm" y="5mm" w="100mm" h="10mm">
                  <value><text>PURCHASE ORDER</text></value>
                </draw>
                <field name="PONumber" x="120mm" y="5mm" w="60mm" h="8mm">
                  <bind match="dataRef" ref="$.Header.PONum"/>
                </field>
              </subform>
              <subform name="Detail" layout="position" h="15mm">
                <occur min="0" max="-1"/>
                <field name="ItemCode" x="10mm" y="2mm" w="40mm" h="8mm">
                  <bind match="dataRef" ref="$.Items.Code"/>
                </field>
                <field name="Amount" x="60mm" y="2mm" w="40mm" h="8mm">
                  <bind match="dataRef" ref="$.Items.Price"/>
                </field>
              </subform>
              <subform name="Footer" layout="position" h="25mm">
                <field name="TotalAmount" x="60mm" y="5mm" w="50mm" h="8mm">
                  <calculate>
                    <script contentType="application/x-formcalc">Sum(Detail[*].Amount)</script>
                  </calculate>
                </field>
                <field name="Barcode" x="10mm" y="5mm" w="40mm" h="15mm">
                  <ui>
                    <barcode type="code128"/>
                  </ui>
                  <bind match="dataRef" ref="$.BarcodeVal"/>
                </field>
              </subform>
            </subform>
          </template>
        </xdp:xdp>
        """;

    [Fact]
    public void Convert_ShouldParseAdobeXdpSuccessfully()
    {
        var report = _adapter.Convert(SampleXdpXml);

        Assert.NotNull(report);
        Assert.Equal("PurchaseOrder", report.Metadata.Title);
        Assert.Equal(PaperKind.A4, report.PageSetup.PaperKind);
        Assert.True(report.PageSetup.Width > 500);
        Assert.True(report.PageSetup.Height > 800);
        Assert.NotNull(report.PageSetup.Margins);
        Assert.True(report.PageSetup.Margins.Top > 0);

        // Verify bands
        Assert.NotNull(report.Bands.PageHeader);
        Assert.NotNull(report.Bands.Detail);
        Assert.NotNull(report.Bands.PageFooter);

        // Verify elements
        var titleElem = report.Bands.PageHeader.Elements.FirstOrDefault(e => e.Text == "PURCHASE ORDER");
        Assert.NotNull(titleElem);
        Assert.Equal("PURCHASE ORDER", titleElem.Text);

        var totalElem = report.Bands.PageFooter.Elements.FirstOrDefault(e => e.Expression != null && e.Expression.Contains("Sum(Fields.Amount)"));
        Assert.NotNull(totalElem);

        var barcodeElem = report.Bands.PageFooter.Elements.FirstOrDefault(e => e.Type == ElementType.Barcode);
        Assert.NotNull(barcodeElem);
        Assert.Equal(ElementType.Barcode, barcodeElem.Type);
    }

    [Fact]
    public void LegacyAdapterFactory_ShouldDetectAndConvertXdp()
    {
        var adapter = LegacyAdapterFactory.GetAdapterByExtension("document.xdp");
        Assert.IsType<AdobeXdpAdapter>(adapter);

        var detected = LegacyAdapterFactory.DetectAdapter(SampleXdpXml);
        Assert.IsType<AdobeXdpAdapter>(detected);
    }

    [Theory]
    [InlineData("Sum(detail[*].amount)", "Sum(Fields.amount)")]
    [InlineData("Count(items[*].id)", "Count(Fields.id)")]
    [InlineData("if (amount > 100) then amount * 0.9 else amount endif", "(amount > 100 ? amount * 0.9 : (amount))")]
    [InlineData("this.getField(\"CustomerName\").value", "Fields.CustomerName")]
    public void LegacyExpressionTranspiler_ShouldTranspileFormCalcAndXfa(string formCalc, string expectedSubstring)
    {
        var result = LegacyExpressionTranspiler.Transpile(formCalc, "XFA");
        Assert.Contains(expectedSubstring, result);
    }
}
