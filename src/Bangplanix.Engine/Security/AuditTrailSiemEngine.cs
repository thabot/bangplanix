using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Bangplanix.Core.Audit;

namespace Bangplanix.Engine.Security;

public enum AuditSinkType
{
    Memory,
    File,
    CefLog,
    LeefLog,
    EcsJson,
    DatadogJson,
    Syslog
}

public interface IAuditSink
{
    Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public class MemoryAuditSink : IAuditSink
{
    private readonly ConcurrentQueue<AuditEvent> _events = new();
    public IReadOnlyList<AuditEvent> Events => _events.ToArray();

    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events.Enqueue(auditEvent);
        return Task.CompletedTask;
    }

    public void Clear() => _events.Clear();
}

public class CallbackAuditSink : IAuditSink
{
    private readonly Action<string, AuditEvent> _callback;
    private readonly AuditSinkType _formatType;

    public CallbackAuditSink(AuditSinkType formatType, Action<string, AuditEvent> callback)
    {
        _formatType = formatType;
        _callback = callback;
    }

    public Task EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var formatted = _formatType switch
        {
            AuditSinkType.CefLog => AuditFormatter.ToCef(auditEvent),
            AuditSinkType.LeefLog => AuditFormatter.ToLeef(auditEvent),
            AuditSinkType.EcsJson => AuditFormatter.ToEcsJson(auditEvent),
            AuditSinkType.DatadogJson => AuditFormatter.ToDatadogJson(auditEvent),
            AuditSinkType.Syslog => AuditFormatter.ToSyslogRfc5424(auditEvent),
            _ => System.Text.Json.JsonSerializer.Serialize(auditEvent)
        };

        _callback(formatted, auditEvent);
        return Task.CompletedTask;
    }
}

public class AuditTrailSiemEngine
{
    private readonly List<IAuditSink> _sinks = new();
    private readonly object _chainLock = new();
    private string _lastHash = "GENESIS_HASH_00000000000000000000000000000000000000000000000000000000";
    private readonly List<AuditEvent> _auditLedger = new();

    private static readonly Regex SensitiveKeyRegex = new(
        @"(password|secret|token|apikey|privatekey|authorization|bearer|creditcard|ssn)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public AuditTrailSiemEngine(IEnumerable<IAuditSink>? sinks = null)
    {
        if (sinks != null)
        {
            _sinks.AddRange(sinks);
        }
    }

    public void RegisterSink(IAuditSink sink)
    {
        lock (_sinks)
        {
            _sinks.Add(sink);
        }
    }

    public async Task<AuditEvent> RecordEventAsync(
        AuditEventType eventType,
        AuditSeverity severity,
        string action,
        string? resourceName = null,
        string? tenantId = null,
        string? userId = null,
        IEnumerable<string>? userRoles = null,
        string? clientIp = null,
        string? userAgent = null,
        int statusCode = 200,
        double durationMs = 0,
        IEnumerable<string>? complianceTags = null,
        IDictionary<string, object?>? details = null,
        CancellationToken cancellationToken = default)
    {
        var sanitizedDetails = SanitizeDetails(details);

        var ev = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType,
            Severity = severity,
            Action = action,
            ResourceName = resourceName,
            TenantId = tenantId,
            UserId = userId,
            UserRoles = userRoles?.ToList() ?? new List<string>(),
            ClientIp = clientIp,
            UserAgent = userAgent,
            StatusCode = statusCode,
            ExecutionDurationMs = durationMs,
            ComplianceTags = complianceTags?.ToList() ?? GetDefaultComplianceTags(eventType),
            Details = sanitizedDetails
        };

        // Chaining cryptographic hash in thread-safe order
        lock (_chainLock)
        {
            ev.PreviousEventHash = _lastHash;
            ev.CurrentEventHash = ev.ComputeHash(_lastHash);
            _lastHash = ev.CurrentEventHash;
            _auditLedger.Add(ev);
        }

        // Dispatch to sinks
        List<IAuditSink> sinksCopy;
        lock (_sinks)
        {
            sinksCopy = new List<IAuditSink>(_sinks);
        }

        foreach (var sink in sinksCopy)
        {
            try
            {
                await sink.EmitAsync(ev, cancellationToken);
            }
            catch
            {
                // Resilient: Sink failure should never interrupt report execution flow
            }
        }

        return ev;
    }

    public bool VerifyLedgerIntegrity(out int corruptedIndex)
    {
        lock (_chainLock)
        {
            var expectedPreviousHash = "GENESIS_HASH_00000000000000000000000000000000000000000000000000000000";

            for (int i = 0; i < _auditLedger.Count; i++)
            {
                var entry = _auditLedger[i];
                if (entry.PreviousEventHash != expectedPreviousHash)
                {
                    corruptedIndex = i;
                    return false;
                }

                var calculatedCurrent = entry.ComputeHash(expectedPreviousHash);
                if (entry.CurrentEventHash != calculatedCurrent)
                {
                    corruptedIndex = i;
                    return false;
                }

                expectedPreviousHash = entry.CurrentEventHash;
            }

            corruptedIndex = -1;
            return true;
        }
    }

    public IReadOnlyList<AuditEvent> GetLedgerSnapshot()
    {
        lock (_chainLock)
        {
            return _auditLedger.ToList();
        }
    }

    private static Dictionary<string, object?> SanitizeDetails(IDictionary<string, object?>? details)
    {
        var result = new Dictionary<string, object?>();
        if (details == null) return result;

        foreach (var (k, v) in details)
        {
            if (SensitiveKeyRegex.IsMatch(k))
            {
                result[k] = "[SANATIZED_SECRET]";
            }
            else
            {
                result[k] = v;
            }
        }

        return result;
    }

    private static List<string> GetDefaultComplianceTags(AuditEventType eventType)
    {
        return eventType switch
        {
            AuditEventType.ReportRendered => new() { "ISO27001_A12_4", "SOC2_CC6_8", "HIPAA_164_312_b" },
            AuditEventType.DataExported => new() { "ISO27001_A12_4", "SOC2_CC6_8", "GDPR_Art30", "PDPA_Sec37" },
            AuditEventType.ReportAccessDenied => new() { "ISO27001_A9_4", "SOC2_CC6_1", "HIPAA_164_312_a" },
            AuditEventType.RedactionApplied => new() { "HIPAA_164_514", "GDPR_Art32", "PDPA_Sec37" },
            AuditEventType.MaskingApplied => new() { "PCI_DSS_3_4", "GDPR_Art32", "PDPA_Sec37" },
            AuditEventType.DigitalSignatureCreated => new() { "eIDAS", "ETDA_ETax", "ISO32000_2" },
            AuditEventType.TamperDetected => new() { "ISO27001_A12_4_2", "SOC2_CC6_8", "CRITICAL_SECURITY" },
            AuditEventType.PluginLoaded => new() { "ISO27001_A14_2", "SOC2_CC6_6" },
            _ => new() { "ISO27001_A12_4" }
        };
    }
}
