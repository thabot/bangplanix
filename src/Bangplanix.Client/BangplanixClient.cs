using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bangplanix.Client;

public sealed class BangplanixClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;
    private readonly string _serverUrl;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public BangplanixClient(
        string serverUrl = "http://localhost:9545",
        HttpClient? httpClient = null,
        string? apiKey = null,
        int maxRetries = 3,
        TimeSpan? retryDelay = null)
    {
        _serverUrl = (serverUrl ?? "http://localhost:9545").TrimEnd('/');
        _disposeClient = httpClient == null;
        _httpClient = httpClient ?? new HttpClient();
        _maxRetries = maxRetries;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(200);

        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<RenderResult> RenderReportAsync(RenderClientRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.TemplatePath) && string.IsNullOrEmpty(request.TemplateJson))
        {
            throw new ArgumentException("Either TemplatePath or TemplateJson must be provided.", nameof(request));
        }

        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("D");
        var endpoint = $"{_serverUrl}/api/v1/report/render";
        var payload = new RenderServerPayload
        {
            TemplatePath = request.TemplatePath,
            TemplateJson = request.TemplateJson,
            DataJson = request.DataJson ?? "{}",
            Parameters = request.Parameters ?? new Dictionary<string, object?>(),
            Format = (request.Format ?? "pdf").ToLowerInvariant()
        };

        var sw = Stopwatch.StartNew();
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= _maxRetries)
        {
            try
            {
                using var msg = new HttpRequestMessage(HttpMethod.Post, endpoint);
                msg.Headers.Add("X-Correlation-ID", correlationId);
                msg.Content = JsonContent.Create(payload);

                using var response = await _httpClient.SendAsync(msg, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var isTransient = response.StatusCode is HttpStatusCode.ServiceUnavailable
                                                          or HttpStatusCode.GatewayTimeout
                                                          or HttpStatusCode.TooManyRequests
                                                          or HttpStatusCode.BadGateway;

                    if (isTransient && attempt < _maxRetries)
                    {
                        attempt++;
                        var backoff = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                        await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    throw new HttpRequestException($"Bangplanix render error [HTTP {response.StatusCode}]: {err}");
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? (payload.Format == "xlsx" ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf");

                sw.Stop();
                return new RenderResult(bytes, payload.Format, contentType, correlationId, sw.ElapsedMilliseconds);
            }
            catch (Exception ex) when (attempt < _maxRetries && (ex is HttpRequestException or TaskCanceledException))
            {
                lastException = ex;
                attempt++;
                var backoff = TimeSpan.FromMilliseconds(_retryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Render request failed after retries.");
    }

    public async Task<RenderResult> RenderToFileAsync(RenderClientRequest request, string outputPath, CancellationToken cancellationToken = default)
    {
        var result = await RenderReportAsync(request, cancellationToken).ConfigureAwait(false);
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllBytesAsync(outputPath, result.Data, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async Task RenderToStreamAsync(RenderClientRequest request, Stream destinationStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destinationStream);
        var result = await RenderReportAsync(request, cancellationToken).ConfigureAwait(false);
        await destinationStream.WriteAsync(result.Data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BatchItemResult>> RenderBatchAsync(IEnumerable<RenderClientRequest> requests, int maxDegreeOfParallelism = 4, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var list = requests.ToList();
        var results = new BatchItemResult[list.Count];
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

        var tasks = list.Select(async (req, idx) =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var res = await RenderReportAsync(req, cancellationToken).ConfigureAwait(false);
                results[idx] = new BatchItemResult(true, res, null, req);
            }
            catch (Exception ex)
            {
                results[idx] = new BatchItemResult(false, null, ex.Message, req);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    public async Task<TemplateValidationResult> ValidateTemplateAsync(string templateJsonOrPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateJsonOrPath);

        var isJson = templateJsonOrPath.TrimStart().StartsWith('{');
        var payload = new TemplateValidationPayload
        {
            TemplateJson = isJson ? templateJsonOrPath : null,
            TemplatePath = !isJson ? templateJsonOrPath : null
        };

        using var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/v1/template/validate", payload, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new TemplateValidationResult { IsValid = false, Errors = [$"HTTP {response.StatusCode}"] };
        }

        var res = await response.Content.ReadFromJsonAsync<TemplateValidationResult>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return res ?? new TemplateValidationResult { IsValid = false };
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"{_serverUrl}/health", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposeClient)
        {
            _httpClient.Dispose();
        }
    }
}

public sealed class RenderServerPayload
{
    [JsonPropertyName("templatePath")]
    public string? TemplatePath { get; set; }

    [JsonPropertyName("templateJson")]
    public string? TemplateJson { get; set; }

    [JsonPropertyName("dataJson")]
    public string? DataJson { get; set; }

    [JsonPropertyName("parameters")]
    public IDictionary<string, object?>? Parameters { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; } = "pdf";
}

public sealed class TemplateValidationPayload
{
    [JsonPropertyName("templatePath")]
    public string? TemplatePath { get; set; }

    [JsonPropertyName("templateJson")]
    public string? TemplateJson { get; set; }
}

public sealed record RenderClientRequest
{
    public string? TemplatePath { get; init; }
    public string? TemplateJson { get; init; }
    public string? DataJson { get; init; }
    public IDictionary<string, object?>? Parameters { get; init; }
    public string Format { get; init; } = "pdf";
    public string? CorrelationId { get; init; }
}

public sealed record RenderResult(byte[] Data, string Format, string ContentType, string CorrelationId, long DurationMs)
{
    public int Length => Data.Length;

    public string ToBase64() => Convert.ToBase64String(Data);

    public MemoryStream ToStream() => new(Data);

    public Task SaveToFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return File.WriteAllBytesAsync(filePath, Data, cancellationToken);
    }
}

public sealed record BatchItemResult(bool IsSuccess, RenderResult? Result, string? ErrorMessage, RenderClientRequest Request);

public sealed class TemplateValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];
}