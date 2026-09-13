using System.Diagnostics;
using Bangplanix.Core.Distributed;

namespace Bangplanix.Engine.Distributed;

/// <summary>
/// Worker pool orchestrator executing distributed render jobs across background task loops.
/// </summary>
public sealed class DistributedWorkerOrchestrator : IAsyncDisposable
{
    private readonly IDistributedQueueProvider _queueProvider;
    private readonly string _consumerGroup;
    private readonly string _workerNodeId;
    private readonly int _concurrency;
    private readonly Func<DistributedRenderJob, CancellationToken, Task<byte[]>> _renderHandler;
    private readonly List<Task> _workerTasks = new();
    private readonly CancellationTokenSource _cts = new();
    private long _totalProcessed;
    private long _totalFailed;
    private bool _isRunning;

    public DistributedWorkerOrchestrator(
        IDistributedQueueProvider queueProvider,
        Func<DistributedRenderJob, CancellationToken, Task<byte[]>> renderHandler,
        int concurrency = 4,
        string consumerGroup = "bangplanix-workers",
        string? workerNodeId = null)
    {
        _queueProvider = queueProvider ?? throw new ArgumentNullException(nameof(queueProvider));
        _renderHandler = renderHandler ?? throw new ArgumentNullException(nameof(renderHandler));
        _concurrency = Math.Max(1, concurrency);
        _consumerGroup = consumerGroup;
        _workerNodeId = workerNodeId ?? $"worker-{Environment.MachineName}-{Guid.NewGuid():N}"[..24];
    }

    public string WorkerNodeId => _workerNodeId;
    public string ConsumerGroup => _consumerGroup;
    public int Concurrency => _concurrency;
    public long TotalProcessed => Interlocked.Read(ref _totalProcessed);
    public long TotalFailed => Interlocked.Read(ref _totalFailed);
    public bool IsRunning => _isRunning;

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;

        for (int i = 0; i < _concurrency; i++)
        {
            string workerSubId = $"{_workerNodeId}-t{i}";
            _workerTasks.Add(Task.Run(() => WorkerLoopAsync(workerSubId, _cts.Token)));
        }
    }

    private async Task WorkerLoopAsync(string workerId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queueProvider.DequeueJobAsync(
                    _consumerGroup,
                    workerId,
                    TimeSpan.FromMilliseconds(500),
                    cancellationToken).ConfigureAwait(false);

                if (job == null)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var sw = Stopwatch.StartNew();
                try
                {
                    byte[] renderedBytes = await _renderHandler(job, cancellationToken).ConfigureAwait(false);
                    sw.Stop();

                    job.Status = DistributedJobStatus.Completed;
                    job.FinishedAtUtc = DateTime.UtcNow;

                    await _queueProvider.AcknowledgeJobAsync(_consumerGroup, job.JobId, cancellationToken).ConfigureAwait(false);
                    Interlocked.Increment(ref _totalProcessed);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    Interlocked.Increment(ref _totalFailed);
                    await _queueProvider.RejectOrRetryJobAsync(_consumerGroup, job, ex.Message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;
        _isRunning = false;

        _cts.Cancel();
        try
        {
            await Task.WhenAll(_workerTasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Worker tasks cancelled
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _cts.Dispose();
    }
}
