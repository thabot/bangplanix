namespace Bangplanix.Core.Distributed;

/// <summary>
/// Execution priority for distributed rendering jobs.
/// </summary>
public enum DistributedJobPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Status of a distributed rendering job.
/// </summary>
public enum DistributedJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    DeadLettered
}

/// <summary>
/// Payload definition for an asynchronous distributed report rendering task.
/// </summary>
public sealed class DistributedRenderJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = "default";
    public DistributedJobStatus Status { get; set; } = DistributedJobStatus.Queued;
    public string TemplateJsonOrPath { get; set; } = string.Empty;
    public string OutputFormat { get; set; } = "pdf"; // pdf, xlsx, svg, csv, etc.
    public DistributedJobPriority Priority { get; set; } = DistributedJobPriority.Normal;
    public Dictionary<string, object?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Dictionary<string, object?>> DatasetRows { get; set; } = new();
    public int MaxRetryCount { get; set; } = 3;
    public int CurrentRetryCount { get; set; } = 0;
    public DateTime QueuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string? WorkerNodeId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CallbackWebhookUrl { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Result of a completed distributed rendering job.
/// </summary>
public sealed class DistributedJobResult
{
    public string JobId { get; set; } = string.Empty;
    public DistributedJobStatus Status { get; set; }
    public byte[]? RenderedData { get; set; }
    public string? ContentType { get; set; }
    public long RenderDurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WorkerNodeId { get; set; }
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
}
