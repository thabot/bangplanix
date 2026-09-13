using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;

namespace Bangplanix.Connectors.Sap;

public sealed class SapRaylightConnector : IDataConnector
{
    private readonly string? _serverBaseUrl;
    private readonly string? _logonToken;
    private readonly string? _userName;
    private readonly string? _password;
    private readonly string _authType;
    private readonly TimeSpan _timeout;
    private readonly HttpMessageHandler? _httpHandler;

    public SapRaylightConnector(
        string? serverBaseUrl = null,
        string? logonToken = null,
        string? userName = null,
        string? password = null,
        string authType = "secEnterprise",
        TimeSpan? timeout = null,
        HttpMessageHandler? httpHandler = null)
    {
        _serverBaseUrl = serverBaseUrl?.TrimEnd('/');
        _logonToken = logonToken;
        _userName = userName;
        _password = password;
        _authType = authType ?? "secEnterprise";
        _timeout = timeout ?? TimeSpan.FromSeconds(15);
        _httpHandler = httpHandler;
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(
        DatasetDefinition dataset,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var endpoint = dataset.QueryOrUrl ?? dataset.ConnectionRef;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException($"Dataset '{dataset.Name}' is missing SAP Raylight endpoint or connection ref.");
        }

        // Build full URL
        var fullUrl = endpoint;
        if (!fullUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !fullUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_serverBaseUrl))
            {
                throw new InvalidOperationException("SapRaylightConnector requires serverBaseUrl when relative endpoint is specified.");
            }
            fullUrl = $"{_serverBaseUrl}/{endpoint.TrimStart('/')}";
        }

        // Anti-SSRF Validation
        if (!AntiSsrfValidator.IsSafeUrl(fullUrl, out var violationReason))
        {
            throw new InvalidOperationException($"SAP Raylight endpoint blocked by Anti-SSRF: {violationReason}");
        }

        using var client = _httpHandler != null
            ? new HttpClient(_httpHandler, disposeHandler: false) { Timeout = _timeout }
            : AntiSsrfValidator.CreateSafeHttpClient(_timeout);

        // Resolve or acquire logon token
        var activeToken = _logonToken;
        if (string.IsNullOrWhiteSpace(activeToken) && !string.IsNullOrWhiteSpace(_userName) && !string.IsNullOrWhiteSpace(_password))
        {
            activeToken = await AcquireLogonTokenAsync(client, cancellationToken).ConfigureAwait(false);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

        if (!string.IsNullOrWhiteSpace(activeToken))
        {
            request.Headers.TryAddWithoutValidation("X-SAP-LogonToken", activeToken);
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
        var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        if (mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase) || (contentBytes.Length > 0 && contentBytes[0] == '<'))
        {
            return ParseRaylightXml(Encoding.UTF8.GetString(contentBytes));
        }

        return ParseRaylightJson(contentBytes);
    }

    private async Task<string> AcquireLogonTokenAsync(HttpClient client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_serverBaseUrl))
        {
            throw new InvalidOperationException("Cannot acquire SAP logon token without serverBaseUrl.");
        }

        var logonUrl = $"{_serverBaseUrl}/logon/long";
        if (!AntiSsrfValidator.IsSafeUrl(logonUrl, out var violationReason))
        {
            throw new InvalidOperationException($"SAP Logon URL blocked by Anti-SSRF: {violationReason}");
        }

        var logonXml = $@"<attrs xmlns=""http://www.sap.com/rws/bip"">
            <attr name=""userName"" type=""string"">{_userName}</attr>
            <attr name=""password"" type=""string"">{_password}</attr>
            <attr name=""auth"" type=""string"">{_authType}</attr>
        </attrs>";

        using var request = new HttpRequestMessage(HttpMethod.Post, logonUrl)
        {
            Content = new StringContent(logonXml, Encoding.UTF8, "application/xml")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        // Check header first
        if (response.Headers.TryGetValues("X-SAP-LogonToken", out var headerTokens))
        {
            foreach (var tok in headerTokens)
            {
                if (!string.IsNullOrWhiteSpace(tok)) return tok.Trim('"', ' ');
            }
        }

        // Extract from response body
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var doc = XDocument.Parse(responseBody);
        var tokenAttr = doc.Descendants().FirstOrDefault(e => e.Attribute("name")?.Value == "logonToken");
        if (tokenAttr != null && !string.IsNullOrWhiteSpace(tokenAttr.Value))
        {
            return tokenAttr.Value.Trim('"', ' ');
        }

        throw new InvalidOperationException("SAP Raylight logon succeeded but no logonToken returned.");
    }

    public static IReadOnlyList<IDictionary<string, object?>> ParseRaylightJson(byte[] jsonBytes)
    {
        if (jsonBytes.Length == 0) return Array.Empty<IDictionary<string, object?>>();

        using var doc = JsonDocument.Parse(jsonBytes);
        var root = doc.RootElement;

        // Pattern 1: { "flow": { "columns": ["ColA", "ColB"], "rows": [["1", "A"], ["2", "B"]] } }
        if (root.TryGetProperty("flow", out var flowEl) || root.TryGetProperty("dataFlow", out flowEl))
        {
            return ExtractFlowElement(flowEl);
        }

        // Pattern 2: Direct array of objects
        if (root.ValueKind == JsonValueKind.Array)
        {
            using var stream = new MemoryStream(jsonBytes);
            return JsonPushStreamConnector.ParseStreamToRows(stream);
        }

        // Pattern 3: Wrapped rows e.g. { "data": [...] } or { "rows": [...] }
        if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Array)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(dataEl.GetRawText()));
            return JsonPushStreamConnector.ParseStreamToRows(stream);
        }

        // Fallback standard JSON stream parser
        using var fallbackStream = new MemoryStream(jsonBytes);
        return JsonPushStreamConnector.ParseStreamToRows(fallbackStream);
    }

    private static IReadOnlyList<IDictionary<string, object?>> ExtractFlowElement(JsonElement flowEl)
    {
        var result = new List<IDictionary<string, object?>>();
        var columns = new List<string>();

        if (flowEl.TryGetProperty("columns", out var colsEl) && colsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var col in colsEl.EnumerateArray())
            {
                if (col.ValueKind == JsonValueKind.String)
                {
                    columns.Add(col.GetString() ?? $"Col_{columns.Count}");
                }
                else if (col.TryGetProperty("name", out var nameProp))
                {
                    columns.Add(nameProp.GetString() ?? $"Col_{columns.Count}");
                }
                else
                {
                    columns.Add($"Col_{columns.Count}");
                }
            }
        }

        if (flowEl.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var rowItem in rowsEl.EnumerateArray())
            {
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                if (rowItem.ValueKind == JsonValueKind.Array)
                {
                    var colIdx = 0;
                    foreach (var cell in rowItem.EnumerateArray())
                    {
                        var colName = colIdx < columns.Count ? columns[colIdx] : $"Col_{colIdx}";
                        dict[colName] = GetJsonValue(cell);
                        colIdx++;
                    }
                }
                else if (rowItem.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in rowItem.EnumerateObject())
                    {
                        dict[prop.Name] = GetJsonValue(prop.Value);
                    }
                }

                result.Add(dict);
            }
        }

        return result;
    }

    private static object? GetJsonValue(JsonElement cell)
    {
        return cell.ValueKind switch
        {
            JsonValueKind.String => cell.GetString(),
            JsonValueKind.Number => cell.TryGetInt64(out var l) ? l : cell.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => cell.GetRawText()
        };
    }

    public static IReadOnlyList<IDictionary<string, object?>> ParseRaylightXml(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent)) return Array.Empty<IDictionary<string, object?>>();

        var doc = XDocument.Parse(xmlContent);
        var result = new List<IDictionary<string, object?>>();

        var columns = new List<string>();
        foreach (var col in doc.Descendants().Where(e => e.Name.LocalName is "column" or "col" or "header"))
        {
            var name = col.Attribute("name")?.Value ?? col.Value.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                columns.Add(name);
            }
        }

        foreach (var rowEl in doc.Descendants().Where(e => e.Name.LocalName is "row" or "record" or "item"))
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            var colIdx = 0;

            foreach (var cellEl in rowEl.Elements())
            {
                var colName = cellEl.Attribute("name")?.Value
                              ?? (colIdx < columns.Count ? columns[colIdx] : cellEl.Name.LocalName);

                dict[colName] = cellEl.Value?.Trim();
                colIdx++;
            }

            if (dict.Count > 0)
            {
                result.Add(dict);
            }
        }

        return result;
    }
}
