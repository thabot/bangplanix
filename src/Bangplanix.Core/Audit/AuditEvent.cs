using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bangplanix.Core.Audit;

public enum AuditEventType
{
    ReportRendered,
    DataExported,
    ReportAccessDenied,
    RedactionApplied,
    MaskingApplied,
    DigitalSignatureCreated,
    TamperDetected,
    UserAuthentication,
    ConfigurationChanged,
    DataQueryExecuted,
    PluginLoaded,
    PluginUnloaded,
    StreamSpilled
}

public enum AuditSeverity
{
    Informational = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

public class AuditEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("eventType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuditEventType EventType { get; set; }

    [JsonPropertyName("severity")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuditSeverity Severity { get; set; } = AuditSeverity.Informational;

    [JsonPropertyName("complianceTags")]
    public List<string> ComplianceTags { get; set; } = new();

    [JsonPropertyName("tenantId")]
    public string? TenantId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("userRoles")]
    public List<string> UserRoles { get; set; } = new();

    [JsonPropertyName("clientIp")]
    public string? ClientIp { get; set; }

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    [JsonPropertyName("resourceName")]
    public string? ResourceName { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    [JsonPropertyName("executionDurationMs")]
    public double ExecutionDurationMs { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, object?> Details { get; set; } = new();

    [JsonPropertyName("previousEventHash")]
    public string? PreviousEventHash { get; set; }

    [JsonPropertyName("currentEventHash")]
    public string? CurrentEventHash { get; set; }

    public string ComputeHash(string? previousHash = null)
    {
        var rawData = $"{EventId}|{Timestamp:O}|{EventType}|{Severity}|{TenantId}|{UserId}|{ClientIp}|{ResourceName}|{Action}|{StatusCode}|{previousHash ?? PreviousEventHash ?? "GENESIS"}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
