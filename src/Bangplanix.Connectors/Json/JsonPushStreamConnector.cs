using System.Text.Json;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Json;

public sealed class JsonPushStreamConnector : IDataConnector
{
    public Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(
        DatasetDefinition dataset,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        if (dataset.StaticData is not null)
        {
            return Task.FromResult(ParseObjectToRows(dataset.StaticData));
        }

        if (!string.IsNullOrWhiteSpace(dataset.QueryOrUrl))
        {
            // If QueryOrUrl is a raw JSON string
            if (dataset.QueryOrUrl.TrimStart().StartsWith('[') || dataset.QueryOrUrl.TrimStart().StartsWith('{'))
            {
                return Task.FromResult(ParseJsonStringToRows(dataset.QueryOrUrl));
            }

            // If QueryOrUrl is a local file path
            if (File.Exists(dataset.QueryOrUrl))
            {
                using var stream = File.OpenRead(dataset.QueryOrUrl);
                return Task.FromResult(ParseStreamToRows(stream));
            }
        }

        return Task.FromResult<IReadOnlyList<IDictionary<string, object?>>>(Array.Empty<IDictionary<string, object?>>());
    }

    public static IReadOnlyList<IDictionary<string, object?>> ParseStreamToRows(Stream jsonStream, string? jsonPath = null)
    {
        using var doc = JsonDocument.Parse(jsonStream);
        var targetElement = string.IsNullOrWhiteSpace(jsonPath) ? doc.RootElement : NavigateJsonPath(doc.RootElement, jsonPath);
        return ParseJsonElementToRows(targetElement);
    }

    public static IReadOnlyList<IDictionary<string, object?>> ParseJsonStringToRows(string jsonString, string? jsonPath = null)
    {
        using var doc = JsonDocument.Parse(jsonString);
        var targetElement = string.IsNullOrWhiteSpace(jsonPath) ? doc.RootElement : NavigateJsonPath(doc.RootElement, jsonPath);
        return ParseJsonElementToRows(targetElement);
    }

    public static JsonElement NavigateJsonPath(JsonElement root, string path)
    {
        var cleanPath = path.Trim().TrimStart('$', '.');
        if (string.IsNullOrWhiteSpace(cleanPath)) return root;

        var segments = cleanPath.Split(new[] { '.', '/' }, StringSplitOptions.RemoveEmptyEntries);
        var current = root;

        foreach (var rawSeg in segments)
        {
            var segment = rawSeg.TrimEnd('[', ']', '*');
            if (string.IsNullOrEmpty(segment)) continue;

            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(segment, out var prop))
            {
                current = prop;
            }
            else if (current.ValueKind == JsonValueKind.Array && int.TryParse(segment, out var idx) && idx >= 0 && idx < current.GetArrayLength())
            {
                current = current[idx];
            }
            else
            {
                // Path not found, return empty/undefined
                return default;
            }
        }

        return current;
    }

    public static IReadOnlyList<IDictionary<string, object?>> ParseObjectToRows(object obj, string? jsonPath = null)
    {
        if (obj is JsonElement jsonElement)
        {
            var target = string.IsNullOrWhiteSpace(jsonPath) ? jsonElement : NavigateJsonPath(jsonElement, jsonPath);
            return ParseJsonElementToRows(target);
        }

        if (obj is IEnumerable<IDictionary<string, object?>> dictList && string.IsNullOrWhiteSpace(jsonPath))
        {
            return dictList.ToList();
        }

        var json = JsonSerializer.Serialize(obj);
        return ParseJsonStringToRows(json, jsonPath);
    }

    private static IReadOnlyList<IDictionary<string, object?>> ParseJsonElementToRows(JsonElement element)
    {
        var rows = new List<IDictionary<string, object?>>();

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    rows.Add(ConvertJsonObjectToDict(item));
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            // Check for common data wrapper properties: "data", "rows", "items", "records"
            if (element.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.Array)
            {
                return ParseJsonElementToRows(dataProp);
            }
            if (element.TryGetProperty("rows", out var rowsProp) && rowsProp.ValueKind == JsonValueKind.Array)
            {
                return ParseJsonElementToRows(rowsProp);
            }
            if (element.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
            {
                return ParseJsonElementToRows(itemsProp);
            }
            if (element.TryGetProperty("records", out var recordsProp) && recordsProp.ValueKind == JsonValueKind.Array)
            {
                return ParseJsonElementToRows(recordsProp);
            }

            // Single object converted to a single row
            rows.Add(ConvertJsonObjectToDict(element));
        }

        return rows;
    }

    private static IDictionary<string, object?> ConvertJsonObjectToDict(JsonElement obj)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in obj.EnumerateObject())
        {
            dict[prop.Name] = ExtractJsonValue(prop.Value);
        }
        return dict;
    }

    private static object? ExtractJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
            JsonValueKind.String => value.GetString(),
            _ => value.ToString()
        };
    }
}
