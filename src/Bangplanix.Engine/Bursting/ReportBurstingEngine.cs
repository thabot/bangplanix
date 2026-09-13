using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting;

/// <summary>
/// Data-Driven Report Bursting Engine partitioning large datasets into individual recipient documents in parallel,
/// with DLQ fault recovery, rate limiting, and confidential password encryption.
/// </summary>
public sealed class ReportBurstingEngine
{
    private readonly MultiChannelDeliveryDispatcher _dispatcher;
    public BurstingDeadLetterQueue DeadLetterQueue { get; }
    public DeliveryRateLimiter RateLimiter { get; }

    public ReportBurstingEngine(
        MultiChannelDeliveryDispatcher? dispatcher = null,
        BurstingDeadLetterQueue? dlq = null,
        DeliveryRateLimiter? rateLimiter = null)
    {
        _dispatcher = dispatcher ?? new MultiChannelDeliveryDispatcher();
        DeadLetterQueue = dlq ?? new BurstingDeadLetterQueue();
        RateLimiter = rateLimiter ?? new DeliveryRateLimiter();
    }

    /// <summary>
    /// Executes high-throughput data-driven bursting on a master dataset.
    /// </summary>
    public async Task<BurstingExecutionSummary> ExecuteBurstingAsync(
        BurstingJobDefinition job,
        IEnumerable<IDictionary<string, object?>> masterRows,
        Func<string, IDictionary<string, object?>, Task<byte[]>> renderReportCallback,
        RecipientSecurityPolicy? securityPolicy = null,
        CancellationToken cancellationToken = default)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));
        if (masterRows == null) throw new ArgumentNullException(nameof(masterRows));
        if (renderReportCallback == null) throw new ArgumentNullException(nameof(renderReportCallback));

        var summary = new BurstingExecutionSummary
        {
            JobId = job.JobId,
            TenantId = job.TenantId,
            StartTimeUtc = DateTime.UtcNow
        };

        // 1. Group / Partition rows by SplitKeyField
        var partitions = masterRows
            .GroupBy(row =>
            {
                if (row.TryGetValue(job.SplitKeyField, out var val) && val != null)
                {
                    return val.ToString()!;
                }
                return "Unknown";
            })
            .ToList();

        summary.TotalSlices = partitions.Count;

        // 2. Parallel execution using SemaphoreSlim concurrency ceiling
        using var semaphore = new SemaphoreSlim(Math.Max(1, job.ConcurrencyLimit));
        var sliceResults = new ConcurrentBag<BurstSliceResult>();

        var tasks = partitions.Select(async group =>
        {
            await semaphore.WaitAsync(cancellationToken);
            var sw = Stopwatch.StartNew();
            string sliceKey = group.Key;
            var representativeRow = group.First();

            // Evaluate dynamic file name
            string fileName = InterpolateFileName(job.FileNamePattern, representativeRow);

            var sliceResult = new BurstSliceResult
            {
                SliceKey = sliceKey,
                OutputFileName = fileName,
                RowsInSlice = group.Count()
            };

            try
            {
                // Render report slice
                byte[] documentBytes = await renderReportCallback(sliceKey, representativeRow);

                // Apply dynamic per-recipient password encryption if enabled
                if (securityPolicy != null && securityPolicy.IsEnabled && documentBytes != null)
                {
                    string password = RecipientPasswordEncryptionEngine.GenerateRecipientPassword(securityPolicy, representativeRow);
                    documentBytes = RecipientPasswordEncryptionEngine.EncryptDocumentBytes(documentBytes, password);
                }

                sw.Stop();
                sliceResult.GenerationLatency = sw.Elapsed;
                sliceResult.GenerationSuccess = documentBytes != null && documentBytes.Length > 0;

                if (sliceResult.GenerationSuccess && job.DeliveryTargets.Count > 0)
                {
                    // Throttle rate before delivery
                    await RateLimiter.AcquireAsync(cancellationToken);

                    // Dispatch to delivery channels
                    var deliveryResults = await _dispatcher.DispatchAsync(
                        documentBytes!,
                        fileName,
                        job.DeliveryTargets,
                        representativeRow,
                        job.MaxRetries,
                        cancellationToken);

                    sliceResult.DeliveryResults.AddRange(deliveryResults);

                    // If any delivery channel failed, enqueue to DLQ
                    var failedChannels = deliveryResults.Where(d => !d.Success).Select(d => d.ChannelType).ToList();
                    if (failedChannels.Count > 0)
                    {
                        DeadLetterQueue.Enqueue(new DlqFailedSlice
                        {
                            JobId = job.JobId,
                            TenantId = job.TenantId,
                            SliceKey = sliceKey,
                            RowData = representativeRow,
                            ErrorMessage = $"Failed delivery on {string.Join(", ", failedChannels)}",
                            FailedChannels = failedChannels
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                sliceResult.GenerationSuccess = false;
                sliceResult.GenerationLatency = sw.Elapsed;
                sliceResult.ErrorMessage = ex.Message;

                DeadLetterQueue.Enqueue(new DlqFailedSlice
                {
                    JobId = job.JobId,
                    TenantId = job.TenantId,
                    SliceKey = sliceKey,
                    RowData = representativeRow,
                    ErrorMessage = ex.Message
                });
            }
            finally
            {
                semaphore.Release();
                sliceResults.Add(sliceResult);
            }
        });

        await Task.WhenAll(tasks);

        summary.EndTimeUtc = DateTime.UtcNow;
        summary.Slices = sliceResults.OrderBy(s => s.SliceKey).ToList();
        return summary;
    }

    /// <summary>
    /// Replays only the failed slices from the Dead-Letter Queue (DLQ).
    /// </summary>
    public async Task<BurstingExecutionSummary> ReplayFailedSlicesAsync(
        BurstingJobDefinition job,
        Func<string, IDictionary<string, object?>, Task<byte[]>> renderReportCallback,
        RecipientSecurityPolicy? securityPolicy = null,
        CancellationToken cancellationToken = default)
    {
        var pendingSlices = DeadLetterQueue.GetPendingSlices(job.JobId);
        if (pendingSlices.Count == 0)
        {
            return new BurstingExecutionSummary { JobId = job.JobId, TotalSlices = 0 };
        }

        var rows = pendingSlices.Select(p => p.RowData).ToList();
        var summary = await ExecuteBurstingAsync(job, rows, renderReportCallback, securityPolicy, cancellationToken);

        foreach (var slice in summary.Slices.Where(s => s.OverallSuccess))
        {
            DeadLetterQueue.MarkResolved(job.JobId, slice.SliceKey);
        }

        return summary;
    }

    private static string InterpolateFileName(string pattern, IDictionary<string, object?> row)
    {
        string result = pattern;
        foreach (var (k, v) in row)
        {
            result = result.Replace($"{{{k}}}", v?.ToString() ?? "");
        }
        result = result.Replace("{Date}", DateTime.UtcNow.ToString("yyyyMMdd"));
        result = result.Replace("{Year}", DateTime.UtcNow.ToString("yyyy"));
        result = result.Replace("{Month}", DateTime.UtcNow.ToString("MM"));
        return result;
    }
}
