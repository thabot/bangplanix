using System.Collections.Concurrent;

namespace Bangplanix.Core.Bursting;

/// <summary>
/// Represents a failed bursting slice stored in the Dead-Letter Queue (DLQ).
/// </summary>
public sealed class DlqFailedSlice
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string JobId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "default";
    public string SliceKey { get; set; } = string.Empty;
    public IDictionary<string, object?> RowData { get; set; } = new Dictionary<string, object?>();
    public string ErrorMessage { get; set; } = string.Empty;
    public List<DeliveryChannelType> FailedChannels { get; set; } = new();
    public DateTime FailedAtUtc { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; } = 1;
    public bool IsResolved { get; set; }
}

/// <summary>
/// Thread-safe Dead-Letter Queue (DLQ) storing failed report slices for diagnostic inspection and 1-click replay.
/// </summary>
public sealed class BurstingDeadLetterQueue
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DlqFailedSlice>> _jobDlq = new();

    /// <summary>
    /// Enqueues a failed slice into the DLQ.
    /// </summary>
    public void Enqueue(DlqFailedSlice slice)
    {
        if (slice == null) return;
        var jobDict = _jobDlq.GetOrAdd(slice.JobId, _ => new ConcurrentDictionary<string, DlqFailedSlice>());
        jobDict[slice.SliceKey] = slice;
    }

    /// <summary>
    /// Retrieves all unresolved failed slices for a job.
    /// </summary>
    public IReadOnlyList<DlqFailedSlice> GetPendingSlices(string jobId)
    {
        if (_jobDlq.TryGetValue(jobId, out var jobDict))
        {
            return jobDict.Values.Where(s => !s.IsResolved).ToList();
        }
        return Array.Empty<DlqFailedSlice>();
    }

    /// <summary>
    /// Marks a failed slice as successfully replayed and resolved.
    /// </summary>
    public bool MarkResolved(string jobId, string sliceKey)
    {
        if (_jobDlq.TryGetValue(jobId, out var jobDict) && jobDict.TryGetValue(sliceKey, out var slice))
        {
            slice.IsResolved = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets total unresolved count in the DLQ for a job.
    /// </summary>
    public int GetPendingCount(string jobId)
    {
        if (_jobDlq.TryGetValue(jobId, out var jobDict))
        {
            return jobDict.Values.Count(s => !s.IsResolved);
        }
        return 0;
    }

    /// <summary>
    /// Clears all entries for a job.
    /// </summary>
    public void Clear(string jobId)
    {
        if (_jobDlq.TryGetValue(jobId, out var jobDict))
        {
            jobDict.Clear();
        }
    }
}
