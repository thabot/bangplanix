using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.BigData;

/// <summary>
/// Google Cloud BigQuery REST API data connector.
/// </summary>
public sealed class BigQueryConnector : IDataConnector
{
    private readonly HttpClient _httpClient;
    private readonly string? _accessToken;
    private readonly string _projectId;

    public BigQueryConnector(string projectId = "my-gcp-project", string? accessToken = null, HttpClient? httpClient = null)
    {
        _projectId = projectId;
        _accessToken = accessToken;
        _httpClient = httpClient ?? new HttpClient();
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

        var endpoint = dataset.ConnectionRef ?? $"https://bigquery.googleapis.com/bigquery/v2/projects/{_projectId}/queries";
        var query = dataset.QueryOrUrl ?? "SELECT 1";

        var payload = new
        {
            query = query,
            useLegacySql = false,
            maxResults = 10000
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
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

        // Extract Schema fields
        var schemaFields = new List<string>();
        if (root.TryGetProperty("schema", out var schemaElem) &&
            schemaElem.TryGetProperty("fields", out var fieldsElem) &&
            fieldsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var f in fieldsElem.EnumerateArray())
            {
                if (f.TryGetProperty("name", out var fName))
                {
                    schemaFields.Add(fName.GetString() ?? $"Field_{schemaFields.Count}");
                }
            }
        }

        // Extract Rows
        if (root.TryGetProperty("rows", out var rowsElem) && rowsElem.ValueKind == JsonValueKind.Array)
        {
            foreach (var rowElem in rowsElem.EnumerateArray())
            {
                if (rowElem.TryGetProperty("f", out var fArray) && fArray.ValueKind == JsonValueKind.Array)
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    int idx = 0;
                    foreach (var cell in fArray.EnumerateArray())
                    {
                        var key = idx < schemaFields.Count ? schemaFields[idx] : $"Field_{idx}";
                        if (cell.TryGetProperty("v", out var val))
                        {
                            row[key] = val.ValueKind == JsonValueKind.Null ? null : val.ToString();
                        }
                        idx++;
                    }
                    rows.Add(row);
                }
            }
        }

        return rows.AsReadOnly();
    }
}
