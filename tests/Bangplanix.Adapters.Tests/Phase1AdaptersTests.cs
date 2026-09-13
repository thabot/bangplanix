using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Bangplanix.Adapters.Cognos;
using Bangplanix.Adapters.EscPos;
using Bangplanix.Adapters.Handlebars;
using Bangplanix.Adapters.LabelPrinter;
using Bangplanix.Adapters.OracleBip;
using Bangplanix.Adapters.Pentaho;
using Bangplanix.Adapters.Ubl;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class Phase1AdaptersTests
{
    [Fact]
    public void PentahoPrptAdapter_ShouldConvertLayoutXmlAndZipCorrectly()
    {
        var layoutXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<report name=""PentahoSalesAnalysis"">
  <report-header height=""40"">
    <label x=""10"" y=""5"" width=""300"" height=""25"" value=""Pentaho Sales Analysis""/>
  </report-header>
  <items height=""25"">
    <string-field x=""10"" y=""0"" width=""150"" height=""20"" field=""CustomerName""/>
    <number-field x=""170"" y=""0"" width=""100"" height=""20"" field=""TotalRevenue"" format=""$#,##0.00""/>
  </items>
</report>";

        var dataDefXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<data-definition>
  <data-sources>
    <sql-query name=""SalesQuery"">SELECT * FROM SalesData</sql-query>
  </data-sources>
  <parameters>
    <plain-parameter name=""RegionParam"" type=""string"" label=""Select Region""/>
  </parameters>
</data-definition>";

        var adapter = new PentahoPrptAdapter();

        // 1. Convert Layout XML directly
        var report = adapter.Convert(layoutXml);
        report.Metadata.Title.Should().Be("PentahoSalesAnalysis");
        report.Bands.ReportHeader.Should().NotBeNull();
        report.Bands.ReportHeader!.Elements[0].Text.Should().Be("Pentaho Sales Analysis");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);
        report.Bands.Detail.Elements[0].Expression.Should().Be("=Fields.CustomerName");
        report.Bands.Detail.Elements[1].Style?.Format.Should().Be("$#,##0.00");

        // 2. Convert from Zip Stream
        using var memStream = new MemoryStream();
        using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
        {
            var lEntry = zip.CreateEntry("layout.xml");
            using (var writer = new StreamWriter(lEntry.Open())) writer.Write(layoutXml);

            var dEntry = zip.CreateEntry("datadefinition.xml");
            using (var writer = new StreamWriter(dEntry.Open())) writer.Write(dataDefXml);
        }
        memStream.Position = 0;

        var zipReport = adapter.Convert(memStream);
        zipReport.Metadata.Title.Should().Be("PentahoSalesAnalysis");
        zipReport.Datasets.Should().HaveCount(1);
        zipReport.Datasets[0].Name.Should().Be("SalesQuery");
        zipReport.Parameters.Should().HaveCount(1);
        zipReport.Parameters[0].Name.Should().Be("RegionParam");
    }

    [Fact]
    public void OracleBipRtfAdapter_ShouldConvertRtfXslFoTagsCorrectly()
    {
        var rtfSample = @"{\rtf1\ansi\deff0
{\fonttbl{\f0\fnil\fcharset0 Arial;}}
\viewkind4\uc1\pard\lang1033\b\f0\fs24 Oracle BI Publisher Invoice Report\b0\par
<?for-each:InvoiceGroup?>\par
<?value-of:InvoiceNo?> - <?value-of:CustomerName?>\par
<?end for-each?>
}";

        var adapter = new OracleBipRtfAdapter();
        var report = adapter.Convert(rtfSample);

        report.Metadata.Title.Should().Be("Oracle BI Publisher Report");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("InvoiceGroup");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);
        report.Bands.Detail.Elements[0].Expression.Should().Be("=Fields.InvoiceNo");
        report.Bands.Detail.Elements[1].Expression.Should().Be("=Fields.CustomerName");
    }

    [Fact]
    public void CognosSpecAdapter_ShouldConvertCognosSpecXmlCorrectly()
    {
        var cognosXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<report xmlns=""http://developer.cognos.com/schemas/report/14.0/"" name=""CognosFinancialSummary"">
  <queries>
    <query name=""MainQuery"">
      <source>
        <sqlQuery name=""SQL1"">
          <sqlText>SELECT AccountCode, Balance FROM GeneralLedger</sqlText>
        </sqlQuery>
      </source>
      <selection>
        <dataItem name=""AccountCode"">
          <expression>[AccountCode]</expression>
        </dataItem>
        <dataItem name=""Balance"">
          <expression>[Balance]</expression>
        </dataItem>
      </selection>
    </query>
  </queries>
  <layouts>
    <layout>
      <reportPages>
        <page name=""Page1"">
          <pageBody>
            <contents>
              <list name=""List1"" refQuery=""MainQuery"">
                <listColumns>
                  <listColumn name=""col1"" refDataItem=""AccountCode""/>
                  <listColumn name=""col2"" refDataItem=""Balance""/>
                </listColumns>
              </list>
            </contents>
          </pageBody>
        </page>
      </reportPages>
    </layout>
  </layouts>
</report>";

        var adapter = new CognosSpecAdapter();
        var report = adapter.Convert(cognosXml);

        report.Metadata.Title.Should().Be("CognosFinancialSummary");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("MainQuery");
        report.Datasets[0].CalculatedColumns.Should().HaveCount(2);
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);
        report.Bands.Detail.Elements[0].Expression.Should().Be("=Fields.AccountCode");
        report.Bands.Detail.Elements[1].Expression.Should().Be("=Fields.Balance");
    }

    [Fact]
    public void HandlebarsHtmlAdapter_ShouldConvertMustacheTemplatesCorrectly()
    {
        var hbsTemplate = @"<!DOCTYPE html>
<html>
<head><title>Payroll Summary</title></head>
<body>
  <h1>Monthly Payroll: {{ MonthYear }}</h1>
  {{#each Employees}}
  <div>{{ FullName }} - {{ Salary }}</div>
  {{/each}}
</body>
</html>";

        var adapter = new HandlebarsHtmlAdapter();
        var report = adapter.Convert(hbsTemplate);

        report.Metadata.Title.Should().Be("Payroll Summary");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("Employees");
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("MonthYear");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().NotBeEmpty();
    }

    [Fact]
    public void UblInvoiceAdapter_ShouldConvertUbl21XmlCorrectly()
    {
        var ublXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Invoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"">
  <cbc:ID>INV-2026-9901</cbc:ID>
  <cbc:IssueDate>2026-09-13</cbc:IssueDate>
  <cac:AccountingSupplierParty>
    <cac:Party>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName>Acme Global Corp</cbc:RegistrationName>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingSupplierParty>
  <cac:AccountingCustomerParty>
    <cac:Party>
      <cac:PartyLegalEntity>
        <cbc:RegistrationName>Bangplanix Client Ltd</cbc:RegistrationName>
      </cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingCustomerParty>
  <cac:LegalMonetaryTotal>
    <cbc:PayableAmount currencyID=""USD"">15400.00</cbc:PayableAmount>
  </cac:LegalMonetaryTotal>
  <cac:InvoiceLine>
    <cbc:ID>1</cbc:ID>
    <cbc:InvoicedQuantity unitCode=""EA"">5</cbc:InvoicedQuantity>
    <cbc:LineExtensionAmount currencyID=""USD"">5000.00</cbc:LineExtensionAmount>
  </cac:InvoiceLine>
</Invoice>";

        var adapter = new UblInvoiceAdapter();
        var report = adapter.Convert(ublXml);

        report.Metadata.Title.Should().Be("Invoice INV-2026-9901");
        report.Bands.ReportHeader.Should().NotBeNull();
        report.Bands.ReportHeader!.Elements.Should().Contain(e => e.Text != null && e.Text.Contains("Acme Global Corp"));
        report.Bands.ReportFooter.Should().NotBeNull();
        report.Bands.ReportFooter!.Elements.Should().Contain(e => e.Text == "15400.00");
    }

    [Fact]
    public void ZebraAndEplAdapter_ShouldConvertZplAndEplCorrectly()
    {
        var zpl = @"^XA
^FO50,50^ADN,36,20^FDSHIPPING LABEL^FS
^FO50,120^BCN,100,Y,N,N^FD123456789012^FS
^XZ";

        var adapter = new ZebraAndEplAdapter();
        var zplReport = adapter.Convert(zpl);

        zplReport.Metadata.Title.Should().Be("Zebra ZPL Label");
        zplReport.Bands.Detail.Should().NotBeNull();
        zplReport.Bands.Detail!.Elements.Should().Contain(e => e.Type == ElementType.Barcode && e.Text == "123456789012");
        zplReport.Bands.Detail.Elements.Should().Contain(e => e.Type == ElementType.Text && e.Text == "SHIPPING LABEL");

        var epl = @"N
A50,50,0,4,1,1,N,""PACKAGE RECEIPT""
B50,120,0,1,2,4,60,B,""PKG-98765""
P1";

        var eplReport = adapter.Convert(epl);
        eplReport.Metadata.Title.Should().Be("Eltron EPL Label");
        eplReport.Bands.Detail.Should().NotBeNull();
        eplReport.Bands.Detail!.Elements.Should().Contain(e => e.Type == ElementType.Barcode && e.Text == "PKG-98765");
        eplReport.Bands.Detail.Elements.Should().Contain(e => e.Type == ElementType.Text && e.Text == "PACKAGE RECEIPT");
    }

    [Fact]
    public void EscPosReceiptAdapter_ShouldConvertThermalReceiptBinaryCorrectly()
    {
        var adapter = new EscPosReceiptAdapter();
        var rawEscPos = "\x1b\x45\x01STORE RECEIPT\x0a\x1b\x45\x00Order #10045\x0aTotal: $45.00\x0a\x1d\x56\x00";

        var report = adapter.Convert(rawEscPos);

        report.Metadata.Title.Should().Be("POS Receipt");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(3);
        report.Bands.Detail.Elements[0].Text.Should().Be("STORE RECEIPT");
        report.Bands.Detail.Elements[0].Style?.FontWeight.Should().Be("Bold");
        report.Bands.Detail.Elements[1].Text.Should().Be("Order #10045");
        report.Bands.Detail.Elements[2].Text.Should().Be("Total: $45.00");
    }
}
