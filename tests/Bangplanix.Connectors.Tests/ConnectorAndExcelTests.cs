using System.Text;
using Bangplanix.Connectors;
using Bangplanix.Connectors.Excel;
using Bangplanix.Connectors.Json;
using Bangplanix.Connectors.Sql;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;
using FluentAssertions;
using MiniExcelLibs;
using Xunit;

namespace Bangplanix.Connectors.Tests;

public class ConnectorAndExcelTests
{
    [Fact]
    public async Task JsonConnector_ParseRawJsonArray_ShouldReturnRows()
    {
        var json = @"[
            { ""id"": 1, ""name"": ""Bangkok Item A"", ""price"": 120.50 },
            { ""id"": 2, ""name"": ""Chiang Mai Item B"", ""price"": 450.00 }
        ]";

        var connector = new JsonPushStreamConnector();
        var dataset = new DatasetDefinition
        {
            Name = "Products",
            Type = DatasetType.Json,
            QueryOrUrl = json
        };

        var rows = await connector.FetchDataAsync(dataset);
        rows.Should().HaveCount(2);
        rows[0]["name"].Should().Be("Bangkok Item A");
        Convert.ToDouble(rows[1]["price"]).Should().Be(450.00);
    }

    [Fact]
    public async Task JsonConnector_ParseWrapperObject_ShouldExtractDataProperty()
    {
        var json = @"{
            ""status"": ""success"",
            ""data"": [
                { ""code"": ""TH-01"", ""description"": ""Standard License"" }
            ]
        }";

        var connector = new JsonPushStreamConnector();
        var dataset = new DatasetDefinition
        {
            Name = "Licenses",
            Type = DatasetType.Json,
            QueryOrUrl = json
        };

        var rows = await connector.FetchDataAsync(dataset);
        rows.Should().HaveCount(1);
        rows[0]["code"].Should().Be("TH-01");
    }

    [Fact]
    public void JsonConnector_ParseStream_ShouldExtractRows()
    {
        var jsonBytes = Encoding.UTF8.GetBytes(@"[
            { ""customer"": ""Alice"", ""amount"": 500 },
            { ""customer"": ""Bob"", ""amount"": 1500 }
        ]");

        using var ms = new MemoryStream(jsonBytes);
        var rows = JsonPushStreamConnector.ParseStreamToRows(ms);

        rows.Should().HaveCount(2);
        rows[1]["customer"].Should().Be("Bob");
    }

    [Fact]
    public void SqlConnector_DetectProvider_ShouldIdentifyCorrectEngines()
    {
        var pgConn = "Host=db.postgres.internal;Port=5432;Database=reports;Username=postgres;Password=pwd;";
        var mysqlConn = "Server=db.mysql.internal;Port=3306;Database=reports;Uid=root;Pwd=pwd;";
        var mssqlConn = "Server=tcp:sqlserver.internal,1433;Initial Catalog=reports;User ID=sa;Password=pwd;";

        SqlDatabaseConnector.DetectProvider(pgConn).Should().Be(DatabaseProviderType.PostgreSql);
        SqlDatabaseConnector.DetectProvider(mysqlConn).Should().Be(DatabaseProviderType.MySql);
        SqlDatabaseConnector.DetectProvider(mssqlConn).Should().Be(DatabaseProviderType.SqlServer);
    }

    [Fact]
    public void SqlConnector_EncryptedConnectionString_ShouldDecryptWithMasterKey()
    {
        var rawConn = "Server=db.internal;Database=Sales;User Id=app;Password=SuperSecret!";
        var masterKey = "Thabot_Enterprise_KMS_Key_2026!";
        var encrypted = AesGcmCrypto.Encrypt(rawConn, masterKey);

        var connector = new SqlDatabaseConnector(masterKey: masterKey);
        var resolved = connector.ResolveConnectionString(encrypted);

        resolved.Should().Be(rawConn);
    }

    [Fact]
    public async Task MiniExcelExporter_SingleSheetExport_ShouldGenerateValidXlsxStream()
    {
        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-2026-001", ["Customer"] = "Thabot Corp", ["Total"] = 15000.00 },
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-2026-002", ["Customer"] = "Bangplanix Ltd", ["Total"] = 42500.50 }
        };

        using var ms = new MemoryStream();
        await MiniExcelReportExporter.ExportToStreamAsync(ms, rows, sheetName: "Invoices");

        ms.Length.Should().BeGreaterThan(0);

        // Verify readable with MiniExcel
        ms.Position = 0;
        var queriedRows = (await ms.QueryAsync(useHeaderRow: true)).ToList();
        queriedRows.Should().HaveCount(2);
    }

    [Fact]
    public async Task MiniExcelExporter_MultiSheetExport_ShouldCreateMultipleSheets()
    {
        var sheet1 = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Category"] = "Software", ["Sales"] = 99000 }
        };
        var sheet2 = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Department"] = "Engineering", ["Headcount"] = 25 }
        };

        var sheets = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>
        {
            ["Revenue"] = sheet1,
            ["HR"] = sheet2
        };

        using var ms = new MemoryStream();
        await MiniExcelReportExporter.ExportMultiSheetToStreamAsync(ms, sheets);

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;
        var sheetNames = ms.GetSheetNames();
        sheetNames.Should().Contain("Revenue");
        sheetNames.Should().Contain("HR");
    }

    [Fact]
    public void DataConnectorFactory_ShouldResolveCorrectConnector()
    {
        var sqlConn = DataConnectorFactory.CreateConnector(DatasetType.Sql);
        sqlConn.Should().BeOfType<SqlDatabaseConnector>();

        var restConn = DataConnectorFactory.CreateConnector(DatasetType.Rest);
        restConn.Should().BeOfType<Bangplanix.Connectors.Rest.RestApiConnector>();

        var jsonConn = DataConnectorFactory.CreateConnector(DatasetType.Json);
        jsonConn.Should().BeOfType<JsonPushStreamConnector>();

        var staticConn = DataConnectorFactory.CreateConnector(DatasetType.Static);
        staticConn.Should().BeOfType<JsonPushStreamConnector>();
    }

    [Fact]
    public void JsonConnector_JsonPath_ShouldExtractNestedArrayData()
    {
        var nestedJson = @"{
            ""organization"": {
                ""division"": {
                    ""employees"": [
                        { ""id"": ""E001"", ""name"": ""John Doe"", ""department"": ""Dev"" },
                        { ""id"": ""E002"", ""name"": ""Jane Smith"", ""department"": ""QA"" }
                    ]
                }
            }
        }";

        var rows = JsonPushStreamConnector.ParseJsonStringToRows(nestedJson, "$.organization.division.employees");
        rows.Should().HaveCount(2);
        rows[0]["name"].Should().Be("John Doe");
        rows[1]["department"].Should().Be("QA");
    }

    [Fact]
    public void SqlConnector_InferDbType_ShouldMapCorrectAdoNetTypes()
    {
        SqlDatabaseConnector.InferDbType(100).Should().Be(System.Data.DbType.Int32);
        SqlDatabaseConnector.InferDbType(100L).Should().Be(System.Data.DbType.Int64);
        SqlDatabaseConnector.InferDbType(99.95m).Should().Be(System.Data.DbType.Decimal);
        SqlDatabaseConnector.InferDbType(DateTime.Now).Should().Be(System.Data.DbType.DateTime);
        SqlDatabaseConnector.InferDbType(true).Should().Be(System.Data.DbType.Boolean);
        SqlDatabaseConnector.InferDbType("Sample Text").Should().Be(System.Data.DbType.String);
    }

    [Fact]
    public async Task RestApiConnector_AntiSsrf_ShouldBlockPrivateIp()
    {
        var restConnector = new Bangplanix.Connectors.Rest.RestApiConnector();
        var dataset = new DatasetDefinition
        {
            Name = "InternalApi",
            Type = DatasetType.Rest,
            QueryOrUrl = "http://127.0.0.1:8080/api/private"
        };

        var act = () => restConnector.FetchDataAsync(dataset);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Anti-SSRF*");
    }

    [Fact]
    public async Task MiniExcelExporter_ExportReportToExcelAsync_ShouldMapBpxHeadersAndValues()
    {
        var report = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "SalesReport" },
            Bands = new BandsDefinition
            {
                PageHeader = new BandDefinition
                {
                    Elements = new List<ElementDefinition>
                    {
                        new ElementDefinition { Text = "Invoice Number" },
                        new ElementDefinition { Text = "Amount Due (THB)" }
                    }
                },
                Detail = new BandDefinition
                {
                    Elements = new List<ElementDefinition>
                    {
                        new ElementDefinition { Expression = "=Fields.InvoiceNo" },
                        new ElementDefinition { Expression = "=Fields.Amount" }
                    }
                }
            }
        };

        var dataRows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-001", ["Amount"] = 12000.00 },
            new Dictionary<string, object?> { ["InvoiceNo"] = "INV-002", ["Amount"] = 35000.00 }
        };

        using var ms = new MemoryStream();
        await MiniExcelReportExporter.ExportReportToExcelAsync(report, dataRows, ms);

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;
        var queried = (await ms.QueryAsync(useHeaderRow: true)).ToList();
        queried.Should().HaveCount(2);
    }
}

