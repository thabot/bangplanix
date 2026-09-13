using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using Bangplanix.Core.Audit;

namespace Bangplanix.Engine.Security;

public enum SiemDestinationType
{
    GenericWebhook,
    SplunkHec,
    ElasticsearchIngest,
    DatadogIntake
}

public class SiemEndpointConfig
{
    public required Uri EndpointUri { get; init; }
    public SiemDestinationType DestinationType { get; init; } = SiemDestinationType.GenericWebhook;
    public string? AuthToken { get; init; }
    public int MaxBatchSize { get; init; } = 50;
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromMilliseconds(500);
    public int MaxRetries { get; init; } = 3;
}

public class SiemNetworkDispatcher : IAuditSink, IAsyncDisposable, IDisposable
{
    private readonly SiemEndpointConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentQueue<AuditEvent> _queue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _workerTask;
    private long _totalDispatchedEvents;
    private long _failedDispatches;
    private bool _isDisposed;

    public long TotalDispatchedEvents => _totalDispatchedEvents;
    public long FailedDispatches => _failedDispatches;
    public int QueuedEventsCount => _queue.Count;

    public SiemNetworkDispatcher(SiemEndpointConfig config, HttpClient? httpClient = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _workerTask = Task.Run(ProcessQueueAsync);
    }

    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        _queue.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.FlushInterval, _cts.Token);
                await FlushBatchAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Resilient worker loop
            }
        }

        // Final flush on shutdown
        await FlushBatchAsync();
    }

    public async Task<int> FlushBatchAsync(CancellationToken cancellationToken = default)
    {
        var batch = new List<AuditEvent>();
        while (batch.Count < _config.MaxBatchSize && _queue.TryDequeue(out var ev))
        {
            batch.Add(ev);
        }

        if (batch.Count == 0)
        {
            return 0;
        }

        bool success = await SendBatchWithRetryAsync(batch, cancellationToken);
        if (success)
        {
            Interlocked.Add(ref _totalDispatchedEvents, batch.Count);
        }
        else
        {
            Interlocked.Add(ref _failedDispatches, batch.Count);
        }

        return batch.Count;
    }

    private async Task<bool> SendBatchWithRetryAsync(List<AuditEvent> batch, CancellationToken cancellationToken)
    {
        var payload = FormatBatchPayload(batch);

        for (int attempt = 1; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, _config.EndpointUri)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(_config.AuthToken))
                {
                    if (_config.DestinationType == SiemDestinationType.SplunkHec)
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Splunk", _config.AuthToken);
                    }
                    else if (_config.DestinationType == SiemDestinationType.DatadogIntake)
                    {
                        request.Headers.Add("DD-API-KEY", _config.AuthToken);
                    }
                    else
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.AuthToken);
                    }
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                if (attempt == _config.MaxRetries) return false;
                await Task.Delay(50 * attempt, cancellationToken);
            }
        }

        return false;
    }

    private string FormatBatchPayload(List<AuditEvent> batch)
    {
        return _config.DestinationType switch
        {
            SiemDestinationType.SplunkHec => string.Join("\n", batch.Select(ev =>
                $"{{\"event\":{AuditFormatter.ToEcsJson(ev)},\"sourcetype\":\"bangplanix:audit\"}}")),
            SiemDestinationType.DatadogIntake => "[" + string.Join(",", batch.Select(AuditFormatter.ToDatadogJson)) + "]",
            SiemDestinationType.ElasticsearchIngest => string.Join("\n", batch.SelectMany(ev => new[]
            {
                $"{{\"index\":{{\"_index\":\"bangplanix-audit-{DateTime.UtcNow:yyyy.MM.dd}\"}}}}",
                AuditFormatter.ToEcsJson(ev)
            })) + "\n",
            _ => "[" + string.Join(",", batch.Select(AuditFormatter.ToEcsJson)) + "]"
        };
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            _cts.Cancel();
            _workerTask.Wait(TimeSpan.FromSeconds(2));
            _cts.Dispose();
        }
        catch
        {
            // Suppress teardown errors
        }

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        try
        {
            await _cts.CancelAsync();
            await _workerTask;
            _cts.Dispose();
        }
        catch
        {
            // Suppress teardown errors
        }

        GC.SuppressFinalize(this);
    }
}
