using System.Data;
using System.Data.Common;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Bangplanix.Connectors.Sql;

public sealed class SqlDatabaseConnector : IDataConnector
{
    private readonly string? _masterKey;
    private readonly DatabaseProviderType? _forcedProvider;

    public SqlDatabaseConnector(DatabaseProviderType? forcedProvider = null, string? masterKey = null)
    {
        _forcedProvider = forcedProvider;
        _masterKey = masterKey;
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(
        DatasetDefinition dataset,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        if (string.IsNullOrWhiteSpace(dataset.ConnectionRef))
        {
            throw new InvalidOperationException($"Dataset '{dataset.Name}' is missing ConnectionRef connection string.");
        }

        if (string.IsNullOrWhiteSpace(dataset.QueryOrUrl))
        {
            throw new InvalidOperationException($"Dataset '{dataset.Name}' is missing QueryOrUrl SQL query.");
        }

        var connectionString = ResolveConnectionString(dataset.ConnectionRef);
        var provider = _forcedProvider ?? DetectProvider(connectionString);

        using var connection = CreateConnection(provider, connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = dataset.QueryOrUrl;
        command.CommandType = CommandType.Text;
        command.CommandTimeout = 30; // 30s default timeout protection

        if (parameters != null && parameters.Count > 0)
        {
            BindParameters(command, parameters, provider);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<IDictionary<string, object?>>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var colName = reader.GetName(i);
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[colName] = value;
            }
            rows.Add(row);
        }

        return rows;
    }

    public string ResolveConnectionString(string connectionRef)
    {
        if (connectionRef.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
        {
            return AesGcmCrypto.Decrypt(connectionRef, _masterKey);
        }
        return connectionRef;
    }

    public static DatabaseProviderType DetectProvider(string connectionString)
    {
        if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Port=5432", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Username=postgres", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("SearchPath=", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseProviderType.PostgreSql;
        }

        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) &&
            (connectionString.Contains("Port=3306", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("Uid=", StringComparison.OrdinalIgnoreCase) ||
             connectionString.Contains("AllowUserVariables=", StringComparison.OrdinalIgnoreCase)))
        {
            return DatabaseProviderType.MySql;
        }

        // Default to SqlServer for ADO.NET standard ConnectionStrings
        return DatabaseProviderType.SqlServer;
    }

    public static DbConnection CreateConnection(DatabaseProviderType provider, string connectionString)
    {
        return provider switch
        {
            DatabaseProviderType.PostgreSql => new NpgsqlConnection(connectionString),
            DatabaseProviderType.MySql => new MySqlConnection(connectionString),
            DatabaseProviderType.SqlServer => new SqlConnection(connectionString),
            _ => new SqlConnection(connectionString)
        };
    }

    public static void BindParameters(DbCommand command, IDictionary<string, object?> parameters, DatabaseProviderType provider)
    {
        foreach (var (name, value) in parameters)
        {
            var paramName = name.StartsWith('@') || name.StartsWith(':') ? name : "@" + name;
            
            var p = command.CreateParameter();
            p.ParameterName = paramName;
            
            if (value == null)
            {
                p.Value = DBNull.Value;
            }
            else
            {
                p.Value = value;
                p.DbType = InferDbType(value);
            }

            command.Parameters.Add(p);
        }
    }

    public static DbType InferDbType(object value)
    {
        return value switch
        {
            int => DbType.Int32,
            long => DbType.Int64,
            short => DbType.Int16,
            byte => DbType.Byte,
            bool => DbType.Boolean,
            decimal => DbType.Decimal,
            double => DbType.Double,
            float => DbType.Single,
            DateTime => DbType.DateTime,
            DateTimeOffset => DbType.DateTimeOffset,
            DateOnly => DbType.Date,
            TimeOnly => DbType.Time,
            Guid => DbType.Guid,
            byte[] => DbType.Binary,
            _ => DbType.String
        };
    }
}
