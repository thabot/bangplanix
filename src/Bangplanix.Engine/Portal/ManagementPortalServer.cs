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
        return """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Bangplanix — Cloud Management Portal</title>
  <style>
    :root {
      --bg: #0d1117;
      --card-bg: #161b22;
      --border: #30363d;
      --text: #c9d1d9;
      --accent: #58a6ff;
      --success: #3fb950;
      --warning: #d29922;
      --danger: #f85149;
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif; background: var(--bg); color: var(--text); padding: 24px; }
    .header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border); padding-bottom: 16px; margin-bottom: 24px; }
    .brand { font-size: 22px; font-weight: 700; color: #fff; display: flex; align-items: center; gap: 10px; }
    .badge { font-size: 12px; background: rgba(88, 166, 255, 0.2); color: var(--accent); padding: 3px 8px; border-radius: 12px; }
    .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 20px; margin-bottom: 24px; }
    .card { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 20px; }
    .card h3 { font-size: 13px; text-transform: uppercase; color: #8b949e; letter-spacing: 0.5px; margin-bottom: 8px; }
    .card .value { font-size: 28px; font-weight: 700; color: #fff; }
    .card .subtext { font-size: 12px; color: #8b949e; margin-top: 6px; }
    .status-ok { color: var(--success); }
    .panel { background: var(--card-bg); border: 1px solid var(--border); border-radius: 8px; padding: 20px; }
    .panel-title { font-size: 16px; font-weight: 600; color: #fff; margin-bottom: 16px; }
    table { width: 100%; border-collapse: collapse; text-align: left; font-size: 14px; }
    th, td { padding: 12px; border-bottom: 1px solid var(--border); }
    th { color: #8b949e; font-weight: 500; }
    .btn { background: #238636; color: #fff; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer; font-weight: 600; }
    .btn:hover { background: #2ea043; }
  </style>
</head>
<body>
  <div class="header">
    <div class="brand">
      <span>🚀 Bangplanix Cloud Management Portal</span>
      <span class="badge">Native AOT v1.0.0</span>
    </div>
    <div>
      <button class="btn" onclick="fetchMetrics()">Refresh Metrics</button>
    </div>
  </div>

  <div class="grid">
    <div class="card">
      <h3>Active Queue Depth</h3>
      <div class="value" id="queueDepth">0</div>
      <div class="subtext">Pending render jobs in broker</div>
    </div>
    <div class="card">
      <h3>Total Processed</h3>
      <div class="value status-ok" id="totalProcessed">0</div>
      <div class="subtext">Successfully generated documents</div>
    </div>
    <div class="card">
      <h3>Dead-Letter Queue (DLQ)</h3>
      <div class="value" id="dlqCount" style="color: var(--danger)">0</div>
      <div class="subtext">Failed slices requiring replay</div>
    </div>
    <div class="card">
      <h3>RAM Consumption</h3>
      <div class="value" id="memoryUsage">0 MB</div>
      <div class="subtext">Managed heap ceiling: &lt; 128MB</div>
    </div>
  </div>

  <div class="panel">
    <div class="panel-title">Cluster Node & Licensing Status</div>
    <table>
      <thead>
        <tr>
          <th>Node Identifier</th>
          <th>Engine Runtime</th>
          <th>Worker Threads</th>
          <th>Licensing Tier</th>
          <th>Health Status</th>
        </tr>
      </thead>
      <tbody>
        <tr>
          <td id="nodeId">worker-local-01</td>
          <td>.NET 10 (C# 14 / Native AOT)</td>
          <td id="concurrency">4 Cores</td>
          <td><span class="badge" style="color: var(--success); background: rgba(63, 185, 80, 0.2)">Enterprise Sovereign</span></td>
          <td><span class="status-ok">● Online</span></td>
        </tr>
      </tbody>
    </table>
  </div>

  <script>
    async function fetchMetrics() {
      try {
        const res = await fetch('/api/metrics');
        if (!res.ok) return;
        const data = await res.json();
        document.getElementById('queueDepth').innerText = data.queueDepth;
        document.getElementById('totalProcessed').innerText = data.totalProcessed;
        document.getElementById('dlqCount').innerText = data.deadLetterCount;
        document.getElementById('memoryUsage').innerText = data.memoryAllocatedMb.toFixed(2) + ' MB';
        document.getElementById('nodeId').innerText = data.nodeId;
        document.getElementById('concurrency').innerText = data.workerConcurrency + ' Cores';
      } catch (e) {
        console.error('Failed to load metrics', e);
      }
    }
    setInterval(fetchMetrics, 3000);
    fetchMetrics();
  </script>
</body>
</html>
""";
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
