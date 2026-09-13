using Bangplanix.Connectors.BigData;
using Bangplanix.Connectors.Json;
using Bangplanix.Connectors.Rest;
using Bangplanix.Connectors.Sap;
using Bangplanix.Connectors.Sql;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors;

public static class DataConnectorFactory
{
    public static IDataConnector CreateConnector(DatasetType type, string? masterKey = null, string? bearerToken = null)
    {
        return type switch
        {
            DatasetType.Sql => new SqlDatabaseConnector(masterKey: masterKey),
            DatasetType.Rest => new RestApiConnector(bearerToken: bearerToken),
            DatasetType.SapRaylight => new SapRaylightConnector(logonToken: bearerToken),
            DatasetType.ClickHouse => new ClickHouseConnector(),
            DatasetType.Snowflake => new SnowflakeConnector(bearerToken: bearerToken),
            DatasetType.BigQuery => new BigQueryConnector(accessToken: bearerToken),
            DatasetType.Json or DatasetType.Static => new JsonPushStreamConnector(),
            _ => new JsonPushStreamConnector()
        };
    }
}
