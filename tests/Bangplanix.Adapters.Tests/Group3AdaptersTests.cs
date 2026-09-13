using System;
using System.IO;
using System.Threading.Tasks;
using Bangplanix.Adapters.ActiveReports;
using Bangplanix.Adapters.Birt;
using Bangplanix.Adapters.Common;
using Bangplanix.Adapters.Crystal;
using Bangplanix.Adapters.Oracle;
using Bangplanix.Core.Models;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Adapters.Tests;

public class Group3AdaptersTests
{
    [Fact]
    public void CrystalReportsXmlAdapterShouldConvertCrystalReportXmlCorrectly()
    {
        var crystalXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CrystalReport Title=""Monthly Sales Ledger"">
  <SummaryInfo>
    <Title>Monthly Sales Ledger</Title>
    <Author>SAP Crystal Reports</Author>
  </SummaryInfo>
  <PageSetup PaperWidth=""16840"" PaperHeight=""11900"" Orientation=""Landscape"" />
  <ParameterFields>
    <ParameterField Name=""?BranchCode"" ValueType=""String"">
      <PromptText>Enter Branch Code</PromptText>
    </ParameterField>
  </ParameterFields>
  <DataSources>
    <Table Name=""SalesLedger"">
      <SelectQuery>SELECT * FROM tbl_sales</SelectQuery>
    </Table>
  </DataSources>
  <Section Kind=""PageHeaderSection"" Height=""800"">
    <TextObject Name=""txtTitle"" Left=""200"" Top=""100"" Width=""4000"" Height=""400"">
      <Text>Monthly Sales Ledger 2026</Text>
      <Font Size=""14"" Bold=""true"" />
    </TextObject>
  </Section>
  <Section Kind=""DetailSection"" Height=""400"">
    <FieldObject Name=""fldCust"" Left=""200"" Top=""50"" Width=""2000"" Height=""300"">
      <Text>{SalesLedger.CustomerName}</Text>
    </FieldObject>
    <FieldObject Name=""fldAmount"" Left=""2300"" Top=""50"" Width=""1500"" Height=""300"">
      <Text>{@FormattedAmount}</Text>
    </FieldObject>
  </Section>
</CrystalReport>";

        var adapter = new CrystalReportsXmlAdapter();
        var report = adapter.Convert(crystalXml);

        report.Metadata.Title.Should().Be("Monthly Sales Ledger");
        report.PageSetup.Orientation.Should().Be(PageOrientation.Landscape);
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("BranchCode");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("SalesLedger");

        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements.Should().HaveCount(1);
        report.Bands.PageHeader.Elements[0].Text.Should().Be("Monthly Sales Ledger 2026");

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);
        report.Bands.Detail.Elements[0].Expression.Should().Be("=Fields.CustomerName");
        report.Bands.Detail.Elements[1].Expression.Should().Be("=Formulas.FormattedAmount");
    }

    [Fact]
    public async Task CrystalBridgeRunnerShouldBatchConvertDirectoryAsync()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "bpx_crystal_test_" + Guid.NewGuid().ToString("N"));
        string inDir = Path.Combine(tempDir, "input");
        string outDir = Path.Combine(tempDir, "output");

        Directory.CreateDirectory(inDir);

        try
        {
            string sampleXml = @"<CrystalReport Title=""Batch Test"">
  <Section Kind=""DetailSection"" Height=""400"">
    <TextObject Name=""t1"" Left=""0"" Top=""0"" Width=""100"" Height=""20"">
      <Text>Batch Line</Text>
    </TextObject>
  </Section>
</CrystalReport>";

            await File.WriteAllTextAsync(Path.Combine(inDir, "report1.rpt.xml"), sampleXml);
            await File.WriteAllTextAsync(Path.Combine(inDir, "report2.crystal.xml"), sampleXml);

            var batchResult = await CrystalBridgeRunner.ConvertDirectoryAsync(inDir, outDir);

            batchResult.TotalFound.Should().Be(2);
            batchResult.ConvertedCount.Should().Be(2);
            batchResult.FailedCount.Should().Be(0);
            batchResult.ConvertedFiles.Should().HaveCount(2);
            File.Exists(Path.Combine(outDir, "report1.bpx")).Should().BeTrue();
            File.Exists(Path.Combine(outDir, "report2.bpx")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void EclipseBirtAdapterShouldConvertBirtReportDesignCorrectly()
    {
        var birtXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<report xmlns=""http://www.eclipse.org/birt/2005/design"" version=""3.2.23"">
  <text-property name=""title"">Quarterly Financial Review</text-property>
  <page-setup>
    <simple-master-page name=""Simple MasterPage"" orientation=""portrait"" type=""a4"" />
  </page-setup>
  <parameters>
    <scalar-parameter name=""FiscalYear"" dataType=""integer"">
      <text-property name=""promptText"">Select Fiscal Year</text-property>
    </scalar-parameter>
  </parameters>
  <data-sets>
    <oda-data-set name=""FinancialData"">
      <property name=""queryText"">SELECT * FROM financial_records</property>
    </oda-data-set>
  </data-sets>
  <body>
    <table name=""MainTable"">
      <header>
        <row>
          <cell>
            <label name=""lblHeader"">
              <text-property name=""text"">Department Financials</text-property>
            </label>
          </cell>
        </row>
      </header>
      <detail>
        <row>
          <cell>
            <data name=""deptName"">
              <expression name=""resultSetColumn"">dataSetRow[""Department""]</expression>
            </data>
          </cell>
        </row>
      </detail>
    </table>
  </body>
</report>";

        var adapter = new EclipseBirtAdapter();
        var report = adapter.Convert(birtXml);

        report.Metadata.Title.Should().Be("Quarterly Financial Review");
        report.PageSetup.Orientation.Should().Be(PageOrientation.Portrait);
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("FiscalYear");
        report.Parameters[0].Type.Should().Be(ParameterType.Number);
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("FinancialData");

        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Department Financials");

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.Department");
    }

    [Fact]
    public void OracleReportsAdapterShouldConvertOracleReportsXmlCorrectly()
    {
        var oracleXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<dataTemplate name=""ORACLE_INVOICE_REPORT"">
  <parameters>
    <parameter name=""P_INVOICE_ID"" dataType=""character"" prompt=""Invoice ID"" />
  </parameters>
  <dataQuery>
    <sqlStatement name=""Q_INVOICE"">
      SELECT inv_no, customer_name, total_amount FROM ap_invoices_all
    </sqlStatement>
  </dataQuery>
  <layout>
    <section name=""MainSection"">
      <boilerplate name=""hdrTitle"">Invoice Summary Report</boilerplate>
      <field name=""f_inv_no"" source=""inv_no"" />
      <field name=""f_cust_name"" source=""customer_name"" />
    </section>
  </layout>
</dataTemplate>";

        var adapter = new OracleReportsAdapter();
        var report = adapter.Convert(oracleXml);

        report.Metadata.Title.Should().Be("ORACLE_INVOICE_REPORT");
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("P_INVOICE_ID");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("Q_INVOICE");

        report.Bands.ReportHeader.Should().NotBeNull();
        report.Bands.ReportHeader!.Elements[0].Text.Should().Be("Invoice Summary Report");

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);
        report.Bands.Detail.Elements[0].Expression.Should().Be("=Fields.inv_no");
        report.Bands.Detail.Elements[1].Expression.Should().Be("=Fields.customer_name");
    }

    [Fact]
    public void ActiveReportsAdapterShouldConvertRpxSectionReportCorrectly()
    {
        var rpxXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ActiveReportsLayout Version=""3"" ReportName=""CustomerStatement"">
  <PageSettings Orientation=""Portrait"" />
  <Parameters>
    <Parameter Key=""CustId"" Prompt=""Customer ID"" />
  </Parameters>
  <Sections>
    <Section Type=""PageHeader"" Height=""1.0in"">
      <Control Type=""AR.Label"" Name=""lblTitle"" Left=""0.5in"" Top=""0.2in"" Width=""5.0in"" Height=""0.5in"" Caption=""Customer Account Statement"" Font=""Arial, 14pt, style=Bold"" />
    </Section>
    <Section Type=""Detail"" Height=""0.5in"">
      <Control Type=""AR.Field"" Name=""txtBalance"" Left=""0.5in"" Top=""0.1in"" Width=""2.0in"" Height=""0.3in"" DataField=""[AccountBalance]"" />
    </Section>
  </Sections>
</ActiveReportsLayout>";

        var adapter = new ActiveReportsAdapter();
        var report = adapter.Convert(rpxXml);

        report.Metadata.Title.Should().Be("CustomerStatement");
        report.PageSetup.Orientation.Should().Be(PageOrientation.Portrait);
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("CustId");

        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Customer Account Statement");
        report.Bands.PageHeader.Elements[0].Style.FontWeight.Should().Be("bold");

        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.AccountBalance");
    }

    [Fact]
    public void LegacyAdapterFactoryShouldResolveAllGroup3Adapters()
    {
        LegacyAdapterFactory.GetAdapterByExtension("report.rpt.xml").Should().BeOfType<CrystalReportsXmlAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("sales.rptdesign").Should().BeOfType<EclipseBirtAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("oracle_doc.rex").Should().BeOfType<OracleReportsAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("invoice.rpx").Should().BeOfType<ActiveReportsAdapter>();
        LegacyAdapterFactory.GetAdapterByExtension("statement.rdlx").Should().BeOfType<ActiveReportsAdapter>();

        LegacyAdapterFactory.DetectAdapter("<CrystalReport Name=\"Test\" />").Should().BeOfType<CrystalReportsXmlAdapter>();
        LegacyAdapterFactory.DetectAdapter("<report xmlns=\"http://www.eclipse.org/birt/2005/design\"></report>").Should().BeOfType<EclipseBirtAdapter>();
        LegacyAdapterFactory.DetectAdapter("<dataTemplate name=\"OracleTest\"></dataTemplate>").Should().BeOfType<OracleReportsAdapter>();
        LegacyAdapterFactory.DetectAdapter("<ActiveReportsLayout Version=\"3\"></ActiveReportsLayout>").Should().BeOfType<ActiveReportsAdapter>();
    }
}