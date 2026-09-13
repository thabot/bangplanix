using System.Collections.Concurrent;
using System.Threading.Channels;
using Bangplanix.Core.Distributed;

namespace Bangplanix.Engine.Distributed;

/// <summary>
/// High-throughput in-memory channel queue provider with prioritized dispatching and DLQ support.
/// </summary>
public sealed class InMemoryDistributedQueueProvider : IDistributedQueueProvider
{
    private readonly Channel<DistributedRenderJob> _channel;
    private readonly ConcurrentDictionary<string, DistributedRenderJob> _activeProcessing = new();
    private readonly ConcurrentQueue<DistributedRenderJob> _deadLetterQueue = new();

    public InMemoryDistributedQueueProvider(int capacity = 100_000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<DistributedRenderJob>(options);
    }

    public async Task<string> EnqueueJobAsync(DistributedRenderJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        job.QueuedAtUtc = DateTime.UtcNow;
        await _channel.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
        return job.JobId;
    }

    public async Task<DistributedRenderJob?> DequeueJobAsync(string consumerGroup, string workerId, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            if (await _channel.Reader.WaitToReadAsync(cts.Token).ConfigureAwait(false) &&
                _channel.Reader.TryRead(out var job))
            {
                job.Status = DistributedJobStatus.Processing;
                job.StartedAtUtc = DateTime.UtcNow;
                job.WorkerNodeId = workerId;
                _activeProcessing[job.JobId] = job;
                return job;
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        return null;
    }

    public Task AcknowledgeJobAsync(string consumerGroup, string jobId, CancellationToken cancellationToken = default)
    {
        _activeProcessing.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }

    public async Task RejectOrRetryJobAsync(string consumerGroup, DistributedRenderJob job, string errorMessage, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        _activeProcessing.TryRemove(job.JobId, out _);

        job.ErrorMessage = errorMessage;
        job.CurrentRetryCount++;

        if (job.CurrentRetryCount <= job.MaxRetryCount)
        {
            job.Status = DistributedJobStatus.Queued;
            await _channel.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            job.Status = DistributedJobStatus.DeadLettered;
            job.FinishedAtUtc = DateTime.UtcNow;
            _deadLetterQueue.Enqueue(job);
        }
    }

    public Task<long> GetQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_channel.Reader.Count);
    }

    public Task<long> GetDeadLetterCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)_deadLetterQueue.Count);
    }

    public IReadOnlyList<DistributedRenderJob> GetDeadLetterJobs()
    {
        return _deadLetterQueue.ToArray();
    }
}
