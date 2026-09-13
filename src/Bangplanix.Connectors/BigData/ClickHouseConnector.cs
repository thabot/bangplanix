using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.BigData;

/// <summary>
/// High-throughput ClickHouse Big Data connector executing SQL queries via native HTTP endpoint.
/// </summary>
public sealed class ClickHouseConnector : IDataConnector
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultDatabase;

    public ClickHouseConnector(HttpClient? httpClient = null, string defaultDatabase = "default")
    {
        _httpClient = httpClient ?? new HttpClient();
        _defaultDatabase = defaultDatabase;
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(
        DatasetDefinition dataset,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        // If static data is provided in testing/mock mode, return it
        if (dataset.StaticData is IReadOnlyList<IDictionary<string, object?>> list)
        {
            return list;
        }
        if (dataset.StaticData is List<Dictionary<string, object?>> dictList)
        {
            return dictList.Cast<IDictionary<string, object?>>().ToList().AsReadOnly();
        }

        var endpoint = dataset.ConnectionRef ?? "http://localhost:8123";
        var query = dataset.QueryOrUrl ?? "SELECT 1";

        // Append JSONEachRow format for efficient row-by-row JSON serialization
        if (!query.Contains("FORMAT", StringComparison.OrdinalIgnoreCase))
        {
            query += " FORMAT JSONEachRow";
        }

        var uriBuilder = new UriBuilder(endpoint);
        var queryParams = $"database={_defaultDatabase}";
        uriBuilder.Query = string.IsNullOrEmpty(uriBuilder.Query) ? queryParams : uriBuilder.Query.TrimStart('?') + "&" + queryParams;

        using var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri)
        {
            Content = new StringContent(query, Encoding.UTF8, "text/plain")
        };

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

        if (string.IsNullOrWhiteSpace(content))
        {
            return rows.AsReadOnly();
        }

        using var reader = new StringReader(content);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(line);
            if (parsed != null)
            {
                rows.Add(parsed);
            }
        }

        return rows.AsReadOnly();
    }
}
