namespace Bangplanix.Core.Bursting;

/// <summary>
/// Delivery channel types supported by the Bursting Subsystem.
/// </summary>
public enum DeliveryChannelType
{
    SmtpEmail,
    AwsS3,
    AzureBlob,
    Sftp,
    Webhook
}

/// <summary>
/// Status of a scheduled job.
/// </summary>
public enum ScheduledJobStatus
{
    Active,
    Paused,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Base class for delivery target configurations.
/// </summary>
public abstract class DeliveryTargetConfig
{
    public abstract DeliveryChannelType ChannelType { get; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public sealed class SmtpDeliveryConfig : DeliveryTargetConfig
{
    public override DeliveryChannelType ChannelType => DeliveryChannelType.SmtpEmail;
    public string RecipientEmailField { get; set; } = "Email";
    public string Subject { get; set; } = "Your Bangplanix Report";
    public string BodyTemplate { get; set; } = "Please find attached your report.";
    public string SmtpHost { get; set; } = "smtp.example.com";
    public int SmtpPort { get; set; } = 587;
    public string? FromAddress { get; set; } = "reports@bangplanix.com";
}

public sealed class S3DeliveryConfig : DeliveryTargetConfig
{
    public override DeliveryChannelType ChannelType => DeliveryChannelType.AwsS3;
    public string BucketName { get; set; } = "bangplanix-bursting-bucket";
    public string KeyPrefix { get; set; } = "reports/{Period}/";
    public string? Region { get; set; } = "ap-southeast-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? EndpointUrl { get; set; } // For MinIO or S3 compatible
}

public sealed class AzureBlobDeliveryConfig : DeliveryTargetConfig
{
    public override DeliveryChannelType ChannelType => DeliveryChannelType.AzureBlob;
    public string ContainerName { get; set; } = "reports";
    public string BlobPrefix { get; set; } = "{Year}/{Month}/";
    public string? ConnectionString { get; set; }
}

public sealed class SftpDeliveryConfig : DeliveryTargetConfig
{
    public override DeliveryChannelType ChannelType => DeliveryChannelType.Sftp;
    public string Host { get; set; } = "sftp.example.com";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "report_user";
    public string RemoteDirectory { get; set; } = "/incoming/reports/";
}

public sealed class WebhookDeliveryConfig : DeliveryTargetConfig
{
    public override DeliveryChannelType ChannelType => DeliveryChannelType.Webhook;
    public string WebhookUrl { get; set; } = "https://api.example.com/webhooks/reports";
    public string? HmacSecretKey { get; set; }
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Result of a single delivery dispatch attempt.
/// </summary>
public sealed class DeliveryResult
{
    public bool Success { get; set; } = true;
    public DeliveryChannelType ChannelType { get; set; }
    public string Destination { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public TimeSpan Latency { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Definition of a Scheduled Report Bursting Job.
/// </summary>
public sealed class BurstingJobDefinition
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = "default";
    public string JobName { get; set; } = "Scheduled Bursting Job";
    public string CronExpression { get; set; } = "0 8 1 * *"; // Monthly on 1st at 08:00
    public string TemplateJson { get; set; } = "{}";
    public string SplitKeyField { get; set; } = "CustomerId";
    public string FilterParamName { get; set; } = "CustomerId";
    public string FileNamePattern { get; set; } = "Invoice_{CustomerId}_{Date}.pdf";
    public List<DeliveryTargetConfig> DeliveryTargets { get; set; } = new();
    public ScheduledJobStatus Status { get; set; } = ScheduledJobStatus.Active;
    public int ConcurrencyLimit { get; set; } = 8;
    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Result of a single burst slice generation & distribution.
/// </summary>
public sealed class BurstSliceResult
{
    public string SliceKey { get; set; } = string.Empty;
    public string OutputFileName { get; set; } = string.Empty;
    public int RowsInSlice { get; set; }
    public bool GenerationSuccess { get; set; } = true;
    public TimeSpan GenerationLatency { get; set; }
    public List<DeliveryResult> DeliveryResults { get; set; } = new();
    public bool OverallSuccess => GenerationSuccess && DeliveryResults.All(d => d.Success);
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Summary report of a complete Bursting batch run.
/// </summary>
public sealed class BurstingExecutionSummary
{
    public string JobId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "default";
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime EndTimeUtc { get; set; } = DateTime.UtcNow;
    public TimeSpan TotalDuration => EndTimeUtc - StartTimeUtc;
    public int TotalSlices { get; set; }
    public int SucceededSlices => Slices.Count(s => s.OverallSuccess);
    public int FailedSlices => Slices.Count(s => !s.OverallSuccess);
    public double ThroughputPerSecond => TotalDuration.TotalSeconds > 0 ? Math.Round(TotalSlices / TotalDuration.TotalSeconds, 2) : 0;
    public List<BurstSliceResult> Slices { get; set; } = new();
}

/// <summary>
/// Interface for delivery channel providers.
/// </summary>
public interface IDeliveryChannel
{
    DeliveryChannelType ChannelType { get; }
    Task<DeliveryResult> DeliverAsync(byte[] documentBytes, string fileName, DeliveryTargetConfig config, IDictionary<string, object?> sliceMetadata, CancellationToken cancellationToken = default);
}
