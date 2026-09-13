using System.Text;
using System.Text.Json;

namespace Bangplanix.Core.Audit;

public static class AuditFormatter
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Formats as Common Event Format (CEF) standard used by Splunk, ArcSight, Microsoft Sentinel.
    /// CEF:0|Bangplanix|EnterpriseReporting|1.0|EventType|Action|Severity|ExtensionPairs...
    /// </summary>
    public static string ToCef(AuditEvent ev)
    {
        var cefSeverity = (int)ev.Severity * 2; // scale 1-5 to 2-10
        var sb = new StringBuilder();
        sb.Append("CEF:0|Bangplanix|EnterpriseReporting|1.0|");
        sb.Append(EscapeCefHeader(ev.EventType.ToString())).Append('|');
        sb.Append(EscapeCefHeader(ev.Action ?? "Execute")).Append('|');
        sb.Append(cefSeverity).Append('|');

        // Extensions
        sb.Append("externalId=").Append(ev.EventId);
        sb.Append(" rt=").Append(ev.Timestamp.ToUnixTimeMilliseconds());
        if (!string.IsNullOrEmpty(ev.TenantId)) sb.Append(" cs1=").Append(EscapeCefValue(ev.TenantId)).Append(" cs1Label=TenantId");
        if (!string.IsNullOrEmpty(ev.UserId)) sb.Append(" suser=").Append(EscapeCefValue(ev.UserId));
        if (!string.IsNullOrEmpty(ev.ClientIp)) sb.Append(" src=").Append(EscapeCefValue(ev.ClientIp));
        if (!string.IsNullOrEmpty(ev.ResourceName)) sb.Append(" request=").Append(EscapeCefValue(ev.ResourceName));
        if (!string.IsNullOrEmpty(ev.UserAgent)) sb.Append(" requestClientApplication=").Append(EscapeCefValue(ev.UserAgent));
        sb.Append(" outcome=").Append(ev.StatusCode >= 400 ? "Failure" : "Success");
        sb.Append(" cn1=").Append(ev.StatusCode).Append(" cn1Label=StatusCode");
        sb.Append(" cn2=").Append((long)ev.ExecutionDurationMs).Append(" cn2Label=DurationMs");

        if (ev.ComplianceTags.Count > 0)
        {
            sb.Append(" cs2=").Append(EscapeCefValue(string.Join(",", ev.ComplianceTags))).Append(" cs2Label=ComplianceTags");
        }

        if (!string.IsNullOrEmpty(ev.CurrentEventHash))
        {
            sb.Append(" cs3=").Append(EscapeCefValue(ev.CurrentEventHash)).Append(" cs3Label=EventHash");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats as Log Event Extended Format (LEEF 2.0) standard used by IBM QRadar.
    /// LEEF:2.0|Bangplanix|EnterpriseReporting|1.0|EventType|attr1=val\tattr2=val...
    /// </summary>
    public static string ToLeef(AuditEvent ev)
    {
        var sb = new StringBuilder();
        sb.Append("LEEF:2.0|Bangplanix|EnterpriseReporting|1.0|");
        sb.Append(ev.EventType.ToString()).Append('|');

        var attrs = new List<string>
        {
            $"eventId={ev.EventId}",
            $"devTime={ev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}",
            $"sev={((int)ev.Severity) * 2}",
            $"action={ev.Action ?? "Execute"}",
            $"usrName={ev.UserId ?? "anonymous"}",
            $"src={ev.ClientIp ?? "127.0.0.1"}",
            $"target={ev.ResourceName ?? "system"}",
            $"status={(ev.StatusCode >= 400 ? "FAILURE" : "SUCCESS")}",
            $"duration={ev.ExecutionDurationMs:F1}"
        };

        if (!string.IsNullOrEmpty(ev.TenantId)) attrs.Add($"tenantId={ev.TenantId}");
        if (ev.ComplianceTags.Count > 0) attrs.Add($"compliance={string.Join(";", ev.ComplianceTags)}");
        if (!string.IsNullOrEmpty(ev.CurrentEventHash)) attrs.Add($"hash={ev.CurrentEventHash}");

        sb.Append(string.Join("\t", attrs));
        return sb.ToString();
    }

    /// <summary>
    /// Formats as Elastic Common Schema (ECS) JSON format.
    /// </summary>
    public static string ToEcsJson(AuditEvent ev)
    {
        var ecsDoc = new Dictionary<string, object?>
        {
            ["@timestamp"] = ev.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["event.id"] = ev.EventId.ToString(),
            ["event.kind"] = "event",
            ["event.category"] = new[] { "audit", "security" },
            ["event.type"] = new[] { "access", "change" },
            ["event.action"] = ev.Action ?? ev.EventType.ToString(),
            ["event.duration"] = (long)(ev.ExecutionDurationMs * 1_000_000), // nanoseconds in ECS
            ["event.outcome"] = ev.StatusCode >= 400 ? "failure" : "success",
            ["log.level"] = ev.Severity.ToString().ToLowerInvariant(),
            ["service.name"] = "bangplanix",
            ["user.id"] = ev.UserId,
            ["user.roles"] = ev.UserRoles,
            ["source.ip"] = ev.ClientIp,
            ["user_agent.original"] = ev.UserAgent,
            ["file.name"] = ev.ResourceName,
            ["http.response.status_code"] = ev.StatusCode,
            ["tenant.id"] = ev.TenantId,
            ["compliance.tags"] = ev.ComplianceTags,
            ["audit.hash.current"] = ev.CurrentEventHash,
            ["audit.hash.previous"] = ev.PreviousEventHash,
            ["audit.details"] = ev.Details
        };

        return JsonSerializer.Serialize(ecsDoc, JsonOpts);
    }

    /// <summary>
    /// Formats as Datadog Logs JSON standard.
    /// </summary>
    public static string ToDatadogJson(AuditEvent ev)
    {
        var ddDoc = new Dictionary<string, object?>
        {
            ["timestamp"] = ev.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["ddsource"] = "bangplanix-audit",
            ["service"] = "bangplanix-reporting",
            ["status"] = ev.Severity switch
            {
                AuditSeverity.Critical => "CRITICAL",
                AuditSeverity.High => "ERROR",
                AuditSeverity.Medium => "WARN",
                _ => "INFO"
            },
            ["message"] = $"[{ev.EventType}] {ev.Action} on {ev.ResourceName} by {ev.UserId ?? "anonymous"} (Status: {ev.StatusCode})",
            ["usr"] = new Dictionary<string, object?>
            {
                ["id"] = ev.UserId,
                ["roles"] = ev.UserRoles
            },
            ["network"] = new Dictionary<string, object?>
            {
                ["client"] = new Dictionary<string, object?>
                {
                    ["ip"] = ev.ClientIp
                }
            },
            ["http"] = new Dictionary<string, object?>
            {
                ["status_code"] = ev.StatusCode,
                ["useragent"] = ev.UserAgent
            },
            ["bangplanix"] = new Dictionary<string, object?>
            {
                ["event_id"] = ev.EventId.ToString(),
                ["event_type"] = ev.EventType.ToString(),
                ["tenant_id"] = ev.TenantId,
                ["resource_name"] = ev.ResourceName,
                ["duration_ms"] = ev.ExecutionDurationMs,
                ["compliance_tags"] = ev.ComplianceTags,
                ["hash"] = ev.CurrentEventHash,
                ["details"] = ev.Details
            }
        };

        return JsonSerializer.Serialize(ddDoc, JsonOpts);
    }

    /// <summary>
    /// Formats as Syslog RFC 5424 structured syslog message.
    /// &lt;PRI&gt;VERSION TIMESTAMP HOSTNAME APP-NAME PROCID MSGID STRUCTURED-DATA MSG
    /// </summary>
    public static string ToSyslogRfc5424(AuditEvent ev, string hostname = "bangplanix-host", int processId = 1)
    {
        // Priority = Facility * 8 + Severity (Facility 13 = log audit)
        var syslogSeverity = ev.Severity switch
        {
            AuditSeverity.Critical => 2, // Critical
            AuditSeverity.High => 3,     // Error
            AuditSeverity.Medium => 4,   // Warning
            AuditSeverity.Low => 5,      // Notice
            _ => 6                       // Informational
        };
        var pri = 13 * 8 + syslogSeverity;

        var structuredData = $"[bangplanixAudit@42424 eventId=\"{ev.EventId}\" eventType=\"{ev.EventType}\" tenantId=\"{ev.TenantId ?? ""}\" userId=\"{ev.UserId ?? ""}\" status=\"{ev.StatusCode}\" hash=\"{ev.CurrentEventHash ?? ""}\"]";
        var msg = $"Action={ev.Action} Resource={ev.ResourceName} Duration={ev.ExecutionDurationMs:F1}ms";

        return $"<{pri}>1 {ev.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ} {hostname} bangplanix {processId} AUDIT {structuredData} {msg}";
    }

    private static string EscapeCefHeader(string str) => str.Replace("|", "\\|").Replace("\\", "\\\\");
    private static string EscapeCefValue(string str) => str.Replace("\\", "\\\\").Replace("=", "\\=").Replace("\n", "\\n").Replace("\r", "\\r");
}
