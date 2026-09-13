using System.Net.Http.Headers;
using Bangplanix.Connectors.Json;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;

namespace Bangplanix.Connectors.Rest;

public sealed class RestApiConnector : IDataConnector
{
    private readonly TimeSpan _timeout;
    private readonly string? _bearerToken;
    private readonly IDictionary<string, string>? _customHeaders;

    public RestApiConnector(
        string? bearerToken = null,
        IDictionary<string, string>? customHeaders = null,
        TimeSpan? timeout = null)
    {
        _bearerToken = bearerToken;
        _customHeaders = customHeaders;
        _timeout = timeout ?? TimeSpan.FromSeconds(10);
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> FetchDataAsync(
        DatasetDefinition dataset,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        var url = dataset.QueryOrUrl ?? dataset.ConnectionRef;
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException($"Dataset '{dataset.Name}' is missing QueryOrUrl REST API endpoint.");
        }

        // Validate Anti-SSRF URL
        if (!AntiSsrfValidator.IsSafeUrl(url, out var violationReason))
        {
            throw new InvalidOperationException($"REST API endpoint blocked by Anti-SSRF: {violationReason}");
        }

        // Append query parameters if provided
        if (parameters != null && parameters.Count > 0)
        {
            var queryBuilder = new List<string>();
            foreach (var (k, v) in parameters)
            {
                if (v != null)
                {
                    queryBuilder.Add($"{Uri.EscapeDataString(k)}={Uri.EscapeDataString(v.ToString() ?? string.Empty)}");
                }
            }

            if (queryBuilder.Count > 0)
            {
                var delimiter = url.Contains('?') ? "&" : "?";
                url += delimiter + string.Join("&", queryBuilder);
            }
        }

        using var client = AntiSsrfValidator.CreateSafeHttpClient(_timeout);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(_bearerToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
        }

        if (_customHeaders != null)
        {
            foreach (var (headerKey, headerVal) in _customHeaders)
            {
                request.Headers.TryAddWithoutValidation(headerKey, headerVal);
            }
        }

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return JsonPushStreamConnector.ParseStreamToRows(responseStream);
    }
}
