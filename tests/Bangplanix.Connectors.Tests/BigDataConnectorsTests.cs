using Bangplanix.Connectors.BigData;
using Bangplanix.Core.Models;
using Xunit;

namespace Bangplanix.Connectors.Tests;

public class BigDataConnectorsTests
{
    [Fact]
    public async Task ClickHouseConnectorShouldReturnStaticOrMockedRows()
    {
        var connector = new ClickHouseConnector();
        var dataset = new DatasetDefinition
        {
            Name = "ClickHouseAnalytics",
            Type = DatasetType.ClickHouse,
            QueryOrUrl = "SELECT event_name, count(*) AS total FROM events GROUP BY event_name",
            StaticData = new List<Dictionary<string, object?>>
            {
                new(StringComparer.OrdinalIgnoreCase) { ["event_name"] = "ReportGenerated", ["total"] = 14500L }
            }
        };

        var data = await connector.FetchDataAsync(dataset);
        Assert.Single(data);
        Assert.Equal("ReportGenerated", data[0]["event_name"]?.ToString());
    }

    [Fact]
    public async Task SnowflakeConnectorShouldReturnStaticOrMockedRows()
    {
        var connector = new SnowflakeConnector(bearerToken: "mock-snowflake-token");
        var dataset = new DatasetDefinition
        {
            Name = "SnowflakeFinancials",
            Type = DatasetType.Snowflake,
            QueryOrUrl = "SELECT account_id, balance FROM accounts WHERE balance > 10000",
            StaticData = new List<Dictionary<string, object?>>
            {
                new(StringComparer.OrdinalIgnoreCase) { ["account_id"] = "ACC-9988", ["balance"] = 250000m }
            }
        };

        var data = await connector.FetchDataAsync(dataset);
        Assert.Single(data);
        Assert.Equal("ACC-9988", data[0]["account_id"]?.ToString());
    }

    [Fact]
    public async Task BigQueryConnectorShouldReturnStaticOrMockedRows()
    {
        var connector = new BigQueryConnector(projectId: "bangplanix-gcp-prod", accessToken: "mock-gcp-token");
        var dataset = new DatasetDefinition
        {
            Name = "BigQueryOrders",
            Type = DatasetType.BigQuery,
            QueryOrUrl = "SELECT order_id, customer_name, total_amount FROM `bangplanix.orders` LIMIT 100",
            StaticData = new List<Dictionary<string, object?>>
            {
                new(StringComparer.OrdinalIgnoreCase) { ["order_id"] = "ORD-001", ["customer_name"] = "Acme Corp", ["total_amount"] = 89000m }
            }
        };

        var data = await connector.FetchDataAsync(dataset);
        Assert.Single(data);
        Assert.Equal("Acme Corp", data[0]["customer_name"]?.ToString());
    }

    [Fact]
    public void DataConnectorFactoryShouldInstantiateBigDataConnectors()
    {
        var clickhouse = DataConnectorFactory.CreateConnector(DatasetType.ClickHouse);
        Assert.IsType<ClickHouseConnector>(clickhouse);

        var snowflake = DataConnectorFactory.CreateConnector(DatasetType.Snowflake, bearerToken: "token");
        Assert.IsType<SnowflakeConnector>(snowflake);

        var bigquery = DataConnectorFactory.CreateConnector(DatasetType.BigQuery, bearerToken: "token");
        Assert.IsType<BigQueryConnector>(bigquery);
    }
}
