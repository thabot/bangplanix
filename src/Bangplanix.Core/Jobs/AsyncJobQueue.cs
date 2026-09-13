using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Bangplanix.Core.Jobs;

public enum JobStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}

public class RenderJob
{
    public string JobId { get; init; } = Guid.NewGuid().ToString("N");
    public string TemplateJson { get; init; } = string.Empty;
    public string? ParametersJson { get; init; }
    public string? DataJson { get; init; }
    public string Format { get; init; } = "pdf";
    public string? TenantId { get; init; }
    public string? WebhookUrl { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public byte[]? OutputData { get; set; }
    public string? ErrorMessage { get; set; }
    public double ProgressPercentage { get; set; }
}

public class AsyncJobQueue : IAsyncDisposable
{
    private readonly Channel<RenderJob> _channel;
    private readonly ConcurrentDictionary<string, RenderJob> _jobRegistry = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task[] _workers;

    public int ActiveWorkerCount { get; private set; }
    public int QueuedCount => _channel.Reader.Count;

    public AsyncJobQueue(int workerCount = 4, int maxQueueCapacity = 1000)
    {
        var options = new BoundedChannelOptions(maxQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<RenderJob>(options);

        _workers = new Task[workerCount];
        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = ProcessJobsAsync(_cts.Token);
        }
    }

    public async ValueTask<RenderJob> EnqueueJobAsync(
        string templateJson,
        string? parametersJson = null,
        string? dataJson = null,
        string format = "pdf",
        string? tenantId = null,
        string? webhookUrl = null,
        CancellationToken cancellationToken = default)
    {
        var job = new RenderJob
        {
            TemplateJson = templateJson,
            ParametersJson = parametersJson,
            DataJson = dataJson,
            Format = format,
            TenantId = tenantId,
            WebhookUrl = webhookUrl
        };

        _jobRegistry[job.JobId] = job;
        await _channel.Writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
        return job;
    }

    public RenderJob? GetJobStatus(string jobId)
    {
        return _jobRegistry.TryGetValue(jobId, out var job) ? job : null;
    }

    public Func<RenderJob, CancellationToken, ValueTask<byte[]>>? JobProcessor { get; set; }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var job = await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                job.Status = JobStatus.Processing;
                job.ProgressPercentage = 10;
                ActiveWorkerCount++;

                try
                {
                    if (JobProcessor != null)
                    {
                        job.OutputData = await JobProcessor(job, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Mock processing
                        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                        job.OutputData = "%PDF-1.4 Mock Batch Render"u8.ToArray();
                    }

                    job.Status = JobStatus.Completed;
                    job.ProgressPercentage = 100;
                    job.CompletedAtUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = ex.Message;
                }
                finally
                {
                    ActiveWorkerCount = Math.Max(0, ActiveWorkerCount - 1);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();
        try
        {
            await Task.WhenAll(_workers).ConfigureAwait(false);
        }
        catch
        {
            // Ignore cancellation on shutdown
        }
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
