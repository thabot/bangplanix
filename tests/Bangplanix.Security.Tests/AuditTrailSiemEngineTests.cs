using System.Text.Json;
using Bangplanix.Core.Audit;
using Bangplanix.Engine.Security;
using Xunit;

namespace Bangplanix.Security.Tests;

public class AuditTrailSiemEngineTests
{
    [Fact]
    public async Task AuditTrailSiemEngine_ShouldRecordEventsAndMaintainCryptographicHashChain()
    {
        var memSink = new MemoryAuditSink();
        var engine = new AuditTrailSiemEngine(new[] { memSink });

        var ev1 = await engine.RecordEventAsync(
            AuditEventType.ReportRendered,
            AuditSeverity.Informational,
            action: "RenderPdf",
            resourceName: "InvoiceReport.bpx",
            tenantId: "tenant-thabot-001",
            userId: "user-somchai-42",
            userRoles: new[] { "BillingStaff", "User" },
            clientIp: "203.144.144.1",
            userAgent: "BangplanixClient/1.0",
            statusCode: 200,
            durationMs: 14.5,
            details: new Dictionary<string, object?> { ["PageCount"] = 3, ["Password"] = "super-secret-pass" });

        var ev2 = await engine.RecordEventAsync(
            AuditEventType.MaskingApplied,
            AuditSeverity.Medium,
            action: "MaskThaiIdAndSalary",
            resourceName: "SalaryReport.bpx",
            tenantId: "tenant-thabot-001",
            userId: "user-somchai-42",
            userRoles: new[] { "BillingStaff" },
            clientIp: "203.144.144.1",
            statusCode: 200,
            durationMs: 2.1);

        var ev3 = await engine.RecordEventAsync(
            AuditEventType.ReportAccessDenied,
            AuditSeverity.High,
            action: "UnauthorizedExport",
            resourceName: "ConfidentialFinancials.bpx",
            tenantId: "tenant-thabot-001",
            userId: "hacker-user",
            clientIp: "198.51.100.99",
            statusCode: 403,
            durationMs: 1.0);

        Assert.Equal(3, memSink.Events.Count);
        Assert.True(engine.VerifyLedgerIntegrity(out var corruptedIndex));
        Assert.Equal(-1, corruptedIndex);

        // Verify Hash Chain
        Assert.Equal("GENESIS_HASH_00000000000000000000000000000000000000000000000000000000", ev1.PreviousEventHash);
        Assert.Equal(ev1.CurrentEventHash, ev2.PreviousEventHash);
        Assert.Equal(ev2.CurrentEventHash, ev3.PreviousEventHash);

        // Verify PII Sanitization
        Assert.Equal("[SANATIZED_SECRET]", ev1.Details["Password"]?.ToString());
    }

    [Fact]
    public async Task AuditTrailSiemEngine_ShouldDetectTamperedLedger()
    {
        var engine = new AuditTrailSiemEngine();

        await engine.RecordEventAsync(AuditEventType.ReportRendered, AuditSeverity.Informational, "Render1");
        await engine.RecordEventAsync(AuditEventType.DataExported, AuditSeverity.Low, "Export1");
        await engine.RecordEventAsync(AuditEventType.DigitalSignatureCreated, AuditSeverity.Informational, "Sign1");

        Assert.True(engine.VerifyLedgerIntegrity(out _));

        var ledger = engine.GetLedgerSnapshot();
        // Tamper with second event
        ledger[1].Action = "MALICIOUS_TAMPERED_ACTION";

        // Verification should now fail on the tampered index
        Assert.False(engine.VerifyLedgerIntegrity(out var corruptedIndex));
        Assert.Equal(1, corruptedIndex);
    }

    [Fact]
    public void AuditFormatter_ShouldProduceValidCefAndLeefOutputs()
    {
        var ev = new AuditEvent
        {
            EventId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Timestamp = DateTimeOffset.Parse("2026-09-12T12:00:00Z"),
            EventType = AuditEventType.RedactionApplied,
            Severity = AuditSeverity.High,
            Action = "RedactPii",
            ResourceName = "CustomerStatement.bpx",
            TenantId = "tenant-007",
            UserId = "secops-officer",
            ClientIp = "10.0.0.50",
            StatusCode = 200,
            ExecutionDurationMs = 5.2,
            ComplianceTags = new List<string> { "ISO27001_A12_4", "GDPR_Art32" }
        };
        ev.CurrentEventHash = ev.ComputeHash();

        var cef = AuditFormatter.ToCef(ev);
        Assert.StartsWith("CEF:0|Bangplanix|EnterpriseReporting|1.0|RedactionApplied|RedactPii|8|", cef);
        Assert.Contains("suser=secops-officer", cef);
        Assert.Contains("src=10.0.0.50", cef);
        Assert.Contains("request=CustomerStatement.bpx", cef);
        Assert.Contains("cs2=ISO27001_A12_4,GDPR_Art32", cef);

        var leef = AuditFormatter.ToLeef(ev);
        Assert.StartsWith("LEEF:2.0|Bangplanix|EnterpriseReporting|1.0|RedactionApplied|", leef);
        Assert.Contains("usrName=secops-officer", leef);
        Assert.Contains("src=10.0.0.50", leef);
        Assert.Contains("compliance=ISO27001_A12_4;GDPR_Art32", leef);
    }

    [Fact]
    public void AuditFormatter_ShouldProduceValidEcsAndDatadogAndSyslogOutputs()
    {
        var ev = new AuditEvent
        {
            EventId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.DataExported,
            Severity = AuditSeverity.Informational,
            Action = "ExportExcel",
            ResourceName = "MonthlySales.bpx",
            TenantId = "tenant-prod",
            UserId = "accountant-01",
            ClientIp = "172.20.1.5",
            StatusCode = 200,
            ExecutionDurationMs = 120.5
        };
        ev.CurrentEventHash = ev.ComputeHash();

        // ECS JSON
        var ecsJson = AuditFormatter.ToEcsJson(ev);
        using var ecsDoc = JsonDocument.Parse(ecsJson);
        Assert.Equal("bangplanix", ecsDoc.RootElement.GetProperty("service.name").GetString());
        Assert.Equal("accountant-01", ecsDoc.RootElement.GetProperty("user.id").GetString());
        Assert.Equal(200, ecsDoc.RootElement.GetProperty("http.response.status_code").GetInt32());

        // Datadog JSON
        var ddJson = AuditFormatter.ToDatadogJson(ev);
        using var ddDoc = JsonDocument.Parse(ddJson);
        Assert.Equal("bangplanix-audit", ddDoc.RootElement.GetProperty("ddsource").GetString());
        Assert.Equal("INFO", ddDoc.RootElement.GetProperty("status").GetString());

        // Syslog RFC 5424
        var syslog = AuditFormatter.ToSyslogRfc5424(ev, "prod-server-01");
        Assert.Contains("<110>1", syslog); // Facility 13 * 8 + 6 = 110
        Assert.Contains("prod-server-01 bangplanix 1 AUDIT", syslog);
        Assert.Contains("Action=ExportExcel", syslog);
    }
}
