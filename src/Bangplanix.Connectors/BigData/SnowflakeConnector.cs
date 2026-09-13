using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.BigData;

/// <summary>
/// Snowflake Cloud Data Warehouse connector utilizing the Snowflake SQL REST API v2.
/// </summary>
public sealed class SnowflakeConnector : IDataConnector
{
    private readonly HttpClient _httpClient;
    private readonly string? _bearerToken;

    public SnowflakeConnector(HttpClient? httpClient = null, string? bearerToken = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _bearerToken = bearerToken;
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(
        DatasetDefinition dataset,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        if (dataset.StaticData is IReadOnlyList<IDictionary<string, object?>> list)
        {
            return list;
        }
        if (dataset.StaticData is List<Dictionary<string, object?>> dictList)
        {
            return dictList.Cast<IDictionary<string, object?>>().ToList().AsReadOnly();
        }

        var endpoint = dataset.ConnectionRef ?? "https://account.snowflakecomputing.com/api/v2/statements";
        var query = dataset.QueryOrUrl ?? "SELECT 1";

        var payload = new
        {
            statement = query,
            timeout = 60,
            resultSetMetaData = new { format = "json" }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(_bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        }

        if (dataset.Headers.Count > 0)
        {
            foreach (var (k, v) in dataset.Headers)
            {
                request.Headers.TryAddWithoutValidation(k, v);
            }
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var rows = new List<IDictionary<string, object?>>();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
        {
            // Extract column names from metadata if present
            var colNames = new List<string>();
            if (root.TryGetProperty("resultSetMetaData", out var meta) &&
                meta.TryGetProperty("rowType", out var rowType) &&
                rowType.ValueKind == JsonValueKind.Array)
            {
                foreach (var col in rowType.EnumerateArray())
                {
                    if (col.TryGetProperty("name", out var colName))
                    {
                        colNames.Add(colName.GetString() ?? $"Col_{colNames.Count}");
                    }
                }
            }

            foreach (var rowElem in dataArray.EnumerateArray())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                if (rowElem.ValueKind == JsonValueKind.Array)
                {
                    int idx = 0;
                    foreach (var cell in rowElem.EnumerateArray())
                    {
                        var key = idx < colNames.Count ? colNames[idx] : $"Col_{idx}";
                        row[key] = cell.ValueKind == JsonValueKind.Null ? null : cell.ToString();
                        idx++;
                    }
                }
                rows.Add(row);
            }
        }

        return rows.AsReadOnly();
    }
}
