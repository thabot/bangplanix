using System.IO.Compression;
using System.Text;
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

namespace Bangplanix.Adapters.Tests;

public class FormatAdaptersTests
{
    [Fact]
    public void SsrsRdlAdapter_ShouldConvertRdlCorrectly()
    {
        var rdlXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition"">
  <Description>Sales Summary 2026</Description>
  <Author>Enterprise BI</Author>
  <Page>
    <PageWidth>8.5in</PageWidth>
    <PageHeight>11in</PageHeight>
    <PageHeader>
      <Height>1in</Height>
      <ReportItems>
        <Textbox Name=""HeaderTitle"">
          <Top>0.2in</Top>
          <Left>0.5in</Left>
          <Width>7.5in</Width>
          <Height>0.5in</Height>
          <Value>Annual Performance Report</Value>
        </Textbox>
      </ReportItems>
    </PageHeader>
  </Page>
  <ReportParameters>
    <ReportParameter Name=""StartDate"">
      <DataType>DateTime</DataType>
      <Prompt>Select Start Date</Prompt>
    </ReportParameter>
  </ReportParameters>
  <DataSets>
    <DataSet Name=""SalesData"">
      <Query>
        <CommandText>SELECT * FROM Sales</CommandText>
      </Query>
    </DataSet>
  </DataSets>
  <Body>
    <Height>5in</Height>
    <ReportItems>
      <Textbox Name=""DetailRow"">
        <Value>=Fields!Revenue.Value</Value>
      </Textbox>
    </ReportItems>
  </Body>
</Report>";

        var adapter = new SsrsRdlAdapter();
        var report = adapter.Convert(rdlXml);

        report.Metadata.Title.Should().Be("Sales Summary 2026");
        report.Metadata.Author.Should().Be("Enterprise BI");
        report.PageSetup.Width.Should().BeApproximately(612.0, 0.1);
        report.PageSetup.Height.Should().BeApproximately(792.0, 0.1);
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("StartDate");
        report.Parameters[0].Type.Should().Be(ParameterType.DateTime);
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("SalesData");
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements.Should().HaveCount(1);
        report.Bands.PageHeader.Elements[0].Text.Should().Be("Annual Performance Report");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.Revenue");
    }

    [Fact]
    public void JaspersoftJrxmlAdapter_ShouldConvertJrxmlCorrectly()
    {
        var jrxml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<jasperReport name=""EmployeeList"" pageWidth=""595"" pageHeight=""842"" leftMargin=""20"" rightMargin=""20"" topMargin=""30"" bottomMargin=""30"">
  <queryString><![CDATA[SELECT * FROM Employees WHERE dept = $P{DeptId}]]></queryString>
  <parameter name=""DeptId"" class=""java.lang.Integer"">
    <defaultValueExpression><![CDATA[101]]></defaultValueExpression>
  </parameter>
  <title>
    <band height=""50"">
      <staticText>
        <reportElement x=""0"" y=""10"" width=""555"" height=""30""/>
        <textElement textAlignment=""Center"">
          <font fontName=""Helvetica"" size=""16"" isBold=""true""/>
        </textElement>
        <text><![CDATA[Employee Directory]]></text>
      </staticText>
    </band>
  </title>
  <detail>
    <band height=""25"">
      <textField>
        <reportElement x=""10"" y=""0"" width=""200"" height=""20""/>
        <textFieldExpression><![CDATA[$F{FullName}]]></textFieldExpression>
      </textField>
    </band>
  </detail>
</jasperReport>";

        var adapter = new JaspersoftJrxmlAdapter();
        var report = adapter.Convert(jrxml);

        report.Metadata.Title.Should().Be("EmployeeList");
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("DeptId");
        report.Parameters[0].Type.Should().Be(ParameterType.Number);
        report.Parameters[0].DefaultValue.Should().Be("101");
        report.Datasets.Should().HaveCount(1);
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Employee Directory");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.FullName");
    }

    [Fact]
    public void FastReportFrxAdapter_ShouldConvertFrxCorrectly()
    {
        var frx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report ReportName=""InvoiceReport"">
  <ReportPage Name=""Page1"" PaperWidth=""210"" PaperHeight=""297"" LeftMargin=""10"" RightMargin=""10"" TopMargin=""10"" BottomMargin=""10"">
    <ReportTitleBand Name=""ReportTitle1"" Height=""30"">
      <TextObject Name=""Text1"" Left=""0"" Top=""5"" Width=""190"" Height=""20"" Text=""Tax Invoice"" Font=""Arial, 14pt, style=Bold"" HorzAlign=""Center""/>
    </ReportTitleBand>
    <DataBand Name=""Data1"" Height=""20"" DataSource=""OrderItems"">
      <TextObject Name=""Text2"" Left=""10"" Top=""0"" Width=""80"" Height=""18"" Text=""[OrderItems.ItemName]""/>
      <BarcodeObject Name=""Barcode1"" Left=""100"" Top=""0"" Width=""50"" Height=""18"" Text=""[OrderItems.SKU]""/>
    </DataBand>
  </ReportPage>
</Report>";

        var adapter = new FastReportFrxAdapter();
        var report = adapter.Convert(frx);

        report.Metadata.Title.Should().Be("InvoiceReport");
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Tax Invoice");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().HaveCount(2);
        report.Bands.Detail.Elements[0].Expression.Should().Be("=Fields.OrderItems.ItemName");
        report.Bands.Detail.Elements[1].Type.Should().Be(ElementType.Barcode);
    }

    [Fact]
    public void StimulsoftMrtAdapter_ShouldConvertJsonMrtCorrectly()
    {
        var jsonMrt = @"{
  ""ReportVersion"": ""2024.1.1"",
  ""ReportName"": ""ExecutiveDashboard"",
  ""Pages"": {
    ""0"": {
      ""Ident"": ""StiPage"",
      ""Name"": ""Page1"",
      ""PageWidth"": 21.0,
      ""PageHeight"": 29.7,
      ""Components"": {
        ""0"": {
          ""Ident"": ""StiHeaderBand"",
          ""Name"": ""HeaderBand1"",
          ""ClientRectangle"": ""0,0.4,19,1.0"",
          ""Components"": {
            ""0"": {
              ""Ident"": ""StiText"",
              ""Name"": ""TextHeader"",
              ""ClientRectangle"": ""0,0,19,0.8"",
              ""Text"": { ""Value"": ""Executive Financial Overview"" }
            }
          }
        },
        ""1"": {
          ""Ident"": ""StiDataBand"",
          ""Name"": ""DataBand1"",
          ""ClientRectangle"": ""0,1.8,19,0.8"",
          ""Components"": {
            ""0"": {
              ""Ident"": ""StiText"",
              ""Name"": ""TextRow"",
              ""ClientRectangle"": ""0,0,10,0.6"",
              ""Text"": { ""Value"": ""{Financials.Revenue}"" }
            }
          }
        }
      }
    }
  },
  ""Dictionary"": {
    ""DataSources"": {
      ""0"": {
        ""Ident"": ""StiSqlSource"",
        ""Name"": ""Financials"",
        ""SqlCommand"": ""SELECT * FROM Financials""
      }
    }
  }
}";

        var adapter = new StimulsoftMrtAdapter();
        var report = adapter.Convert(jsonMrt);

        report.Metadata.Title.Should().Be("ExecutiveDashboard");
        report.Datasets.Should().HaveCount(1);
        report.Datasets[0].Name.Should().Be("Financials");
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Executive Financial Overview");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.Financials.Revenue");
    }

    [Fact]
    public void TelerikTrdxAdapter_ShouldConvertTrdxXmlAndTrdpZipCorrectly()
    {
        var trdx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Report xmlns=""http://schemas.telerik.com/reporting/2021/1.0"" Name=""InventorySummary"">
  <PageSettings>
    <PageSize Width=""8.5in"" Height=""11in""/>
  </PageSettings>
  <ReportParameters>
    <ReportParameter Name=""WarehouseCode"" Type=""String"" Text=""Select Warehouse""/>
  </ReportParameters>
  <Items>
    <PageHeaderSection Height=""0.8in"" Name=""pageHeaderSection1"">
      <Items>
        <TextBox Location=""0in, 0.1in"" Size=""8.5in, 0.4in"" Value=""Warehouse Inventory"" Name=""titleBox""/>
      </Items>
    </PageHeaderSection>
    <DetailSection Height=""0.5in"" Name=""detailSection1"">
      <Items>
        <TextBox Location=""0in, 0in"" Size=""4in, 0.3in"" Value=""=Fields.ItemName"" Name=""itemBox""/>
      </Items>
    </DetailSection>
  </Items>
</Report>";

        var adapter = new TelerikTrdxAdapter();
        var report = adapter.Convert(trdx);

        report.Metadata.Title.Should().Be("InventorySummary");
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("WarehouseCode");
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Warehouse Inventory");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.ItemName");

        using var memStream = new MemoryStream();
        using (var zip = new ZipArchive(memStream, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("report.trdx");
            using var entryWriter = new StreamWriter(entry.Open());
            entryWriter.Write(trdx);
        }
        memStream.Position = 0;

        var trdpReport = adapter.Convert(memStream);
        trdpReport.Metadata.Title.Should().Be("InventorySummary");
    }

    [Fact]
    public void DevExpressRepxAdapter_ShouldConvertRepxCorrectly()
    {
        var repx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<XtraReportsLayoutSerializer SerializerVersion=""23.2.3.0"" Name=""SalesByRegion"">
  <Parameters>
    <Item1 Name=""Region"" Description=""Sales Region"" Value=""APAC""/>
  </Parameters>
  <Bands>
    <Item1 ControlType=""TopMarginBand"" Name=""TopMargin"" HeightF=""50"">
      <Controls>
        <Item1 ControlType=""XRLabel"" Name=""xrLabel1"" Text=""Sales by Region Report"" LocationFloat=""0, 10"" SizeF=""500, 30""/>
      </Controls>
    </Item1>
    <Item2 ControlType=""DetailBand"" Name=""Detail"" HeightF=""30"">
      <Controls>
        <Item1 ControlType=""XRLabel"" Name=""xrLabel2"" LocationFloat=""0, 0"" SizeF=""300, 20"">
          <ExpressionBindings>
            <Item1 PropertyName=""Text"" Expression=""[Region] + ' - ' + [TotalSales]""/>
          </ExpressionBindings>
        </Item1>
      </Controls>
    </Item2>
  </Bands>
</XtraReportsLayoutSerializer>";

        var adapter = new DevExpressRepxAdapter();
        var report = adapter.Convert(repx);

        report.Metadata.Title.Should().Be("SalesByRegion");
        report.Parameters.Should().HaveCount(1);
        report.Parameters[0].Name.Should().Be("Region");
        report.Parameters[0].DefaultValue.Should().Be("APAC");
        report.Bands.PageHeader.Should().NotBeNull();
        report.Bands.PageHeader!.Elements[0].Text.Should().Be("Sales by Region Report");
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements[0].Expression.Should().Be("=Fields.Region + ' - ' + Fields.TotalSales");
    }

    [Fact]
    public void OfficeAndHtmlAdapter_ShouldConvertHtmlAndLiquidTemplatesCorrectly()
    {
        var html = @"<!DOCTYPE html>
<html>
<head>
  <title>Customer Statement</title>
</head>
<body>
  <h1>Monthly Statement for {{ CustomerName }}</h1>
  <p>Account Balance: {{ balance }}</p>
  <p>Thank you for your business!</p>
</body>
</html>";

        var adapter = new OfficeAndHtmlAdapter();
        var report = adapter.Convert(html);

        report.Metadata.Title.Should().Be("Customer Statement");
        report.Parameters.Should().HaveCount(2);
        report.Bands.Detail.Should().NotBeNull();
        report.Bands.Detail!.Elements.Should().NotBeEmpty();
        report.Bands.Detail.Elements[0].Style?.FontSize.Should().Be(18);
        report.Bands.Detail.Elements[2].Text.Should().Be("Thank you for your business!");
    }
}
