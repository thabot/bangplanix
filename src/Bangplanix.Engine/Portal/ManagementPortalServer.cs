using System.Net;
using System.Text;
using System.Text.Json;
using Bangplanix.Core.Distributed;
using Bangplanix.Engine.Distributed;

namespace Bangplanix.Engine.Portal;

/// <summary>
/// Embedded Web Management Portal HTTP Server serving telemetry dashboards, worker controls, and licensing info.
/// </summary>
public sealed class ManagementPortalServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly int _port;
    private readonly IDistributedQueueProvider? _queueProvider;
    private readonly DistributedWorkerOrchestrator? _orchestrator;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenerTask;
    private bool _isRunning;

    public ManagementPortalServer(
        int port = 8088,
        IDistributedQueueProvider? queueProvider = null,
        DistributedWorkerOrchestrator? orchestrator = null)
    {
        _port = port;
        _queueProvider = queueProvider;
        _orchestrator = orchestrator;
    }

    public int Port => _port;
    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _listener.Prefixes.Add($"http://*:{_port}/");
            _listener.Start();
            _isRunning = true;
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
        catch (HttpListenerException)
        {
            // Fallback for non-admin localhost binding
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            _isRunning = true;
            _listenerTask = Task.Run(() => ListenLoopAsync(_cts.Token));
        }
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // listener aborted or transient
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var req = context.Request;
        var res = context.Response;

        res.Headers.Add("Access-Control-Allow-Origin", "*");
        res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = (int)HttpStatusCode.NoContent;
            res.Close();
            return;
        }

        string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

        try
        {
            if (path is "/" or "/index.html" or "/portal")
            {
                await ServeHtmlAsync(res).ConfigureAwait(false);
            }
            else if (path == "/api/status" || path == "/api/metrics")
            {
                await ServeMetricsApiAsync(res).ConfigureAwait(false);
            }
            else if (path == "/api/health")
            {
                await ServeHealthApiAsync(res).ConfigureAwait(false);
            }
            else
            {
                res.StatusCode = (int)HttpStatusCode.NotFound;
                byte[] notFound = "{\"error\": \"Not Found\"}"u8.ToArray();
                res.ContentType = "application/json";
                await res.OutputStream.WriteAsync(notFound).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            res.StatusCode = (int)HttpStatusCode.InternalServerError;
            byte[] errBytes = Encoding.UTF8.GetBytes($"{{\"error\": \"{ex.Message}\"}}");
            res.ContentType = "application/json";
            await res.OutputStream.WriteAsync(errBytes).ConfigureAwait(false);
        }
        finally
        {
            res.Close();
        }
    }

    private static async Task ServeHtmlAsync(HttpListenerResponse res)
    {
        res.ContentType = "text/html; charset=utf-8";
        res.StatusCode = (int)HttpStatusCode.OK;
        string html = GetEmbeddedPortalHtml();
        byte[] bytes = Encoding.UTF8.GetBytes(html);
        await res.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
    }

    private async Task ServeMetricsApiAsync(HttpListenerResponse res)
    {
        long queueDepth = _queueProvider != null ? await _queueProvider.GetQueueDepthAsync().ConfigureAwait(false) : 0;
        long dlqCount = _queueProvider != null ? await _queueProvider.GetDeadLetterCountAsync().ConfigureAwait(false) : 0;

        var payload = new
        {
            engine = "Bangplanix Enterprise (.NET 10 / Native AOT)",
            version = "1.0.0",
            uptimeSeconds = (long)TimeSpan.FromTicks(Environment.TickCount64 * 10_000).TotalSeconds,
            nodeId = _orchestrator?.WorkerNodeId ?? Environment.MachineName,
            workerConcurrency = _orchestrator?.Concurrency ?? Environment.ProcessorCount,
            totalProcessed = _orchestrator?.TotalProcessed ?? 0,
            totalFailed = _orchestrator?.TotalFailed ?? 0,
            queueDepth,
            deadLetterCount = dlqCount,
            memoryAllocatedMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            gcGen0 = GC.CollectionCount(0),
            gcGen1 = GC.CollectionCount(1),
            gcGen2 = GC.CollectionCount(2),
            timestampUtc = DateTime.UtcNow
        };

        res.ContentType = "application/json";
        res.StatusCode = (int)HttpStatusCode.OK;
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await res.OutputStream.WriteAsync(jsonBytes).ConfigureAwait(false);
    }

    private static async Task ServeHealthApiAsync(HttpListenerResponse res)
    {
        res.ContentType = "application/json";
        res.StatusCode = (int)HttpStatusCode.OK;
        byte[] jsonBytes = "{\"status\": \"Healthy\", \"service\": \"Bangplanix Management Portal\"}"u8.ToArray();
        await res.OutputStream.WriteAsync(jsonBytes).ConfigureAwait(false);
    }

    public static string GetEmbeddedPortalHtml()
    {
        return PortalHtmlBuilder.BuildHtml();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            _cts.Cancel();
            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch (Exception)
            {
                // ignore
            }
            if (_listenerTask != null)
            {
                await _listenerTask.ConfigureAwait(false);
            }
            _cts.Dispose();
            _isRunning = false;
        }
    }
}
