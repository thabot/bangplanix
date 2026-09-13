using System.Text;
using Bangplanix.Core.Bursting;
using Bangplanix.Engine.Bursting;
using Bangplanix.Engine.Bursting.Delivery;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class FinalReleaseBurstingScaleTests
{
    [Fact]
    public async Task ReportBurstingEngine_ShouldHandleHighVolumeBatchWithZeroDataLoss()
    {
        var dlq = new BurstingDeadLetterQueue();
        var rateLimiter = new DeliveryRateLimiter(maxDispatchesPerSecond: 10_000);
        var engine = new ReportBurstingEngine(dlq: dlq, rateLimiter: rateLimiter);

        // Generate 2,000 rows partitioned across 200 distinct employee groups (10 rows per employee)
        int totalEmployees = 200;
        int rowsPerEmployee = 10;
        var dataset = new List<Dictionary<string, object?>>(totalEmployees * rowsPerEmployee);

        for (int emp = 0; emp < totalEmployees; emp++)
        {
            string empId = $"EMP_{emp:D4}";
            for (int r = 0; r < rowsPerEmployee; r++)
            {
                dataset.Add(new Dictionary<string, object?>
                {
                    ["EmployeeId"] = empId,
                    ["EmployeeName"] = $"Staff {empId}",
                    ["Email"] = $"{empId}@enterprise.corp",
                    ["Year"] = "2026",
                    ["NationalId"] = $"11002003{emp:D5}",
                    ["Salary"] = 55000.00m + (emp * 10) + r
                });
            }
        }

        var securityPolicy = new RecipientSecurityPolicy
        {
            PasswordPattern = "TH-{NationalId:Last4}-{Year}",
            FallbackPassword = "Default@2026"
        };

        var job = new BurstingJobDefinition
        {
            JobId = "scale_burst_test_2026",
            JobName = "Enterprise Annual Payslips Burst",
            SplitKeyField = "EmployeeId",
            FileNamePattern = "Payslip_{EmployeeId}_{Year}.pdf",
            ConcurrencyLimit = 8,
            DeliveryTargets = new List<DeliveryTargetConfig>
            {
                new SmtpDeliveryConfig { RecipientEmailField = "Email" },
                new S3DeliveryConfig { BucketName = "hr-payslips-archive", KeyPrefix = "2026/annual/" }
            }
        };

        var summary = await engine.ExecuteBurstingAsync(
            job,
            dataset,
            (sliceKey, row) =>
            {
                return Task.FromResult(Encoding.UTF8.GetBytes($"%PDF-1.7 Payslip for {sliceKey}"));
            },
            securityPolicy: securityPolicy);

        Assert.Equal(totalEmployees, summary.TotalSlices);
        Assert.Equal(totalEmployees, summary.SucceededSlices);
        Assert.Equal(0, summary.FailedSlices);
        Assert.Equal(0, dlq.GetPendingCount(job.JobId));
        Assert.True(summary.ThroughputPerSecond > 0);

        // Verify first slice properties
        var first = summary.Slices.First();
        Assert.Equal(10, first.RowsInSlice);
        Assert.Equal(2, first.DeliveryResults.Count);
        Assert.All(first.DeliveryResults, d => Assert.True(d.Success));
    }

    [Fact]
    public async Task MultiChannelDeliveryDispatcher_ShouldHandleResilientRetriesAndThrottling()
    {
        var dispatcher = new MultiChannelDeliveryDispatcher();
        byte[] payload = Encoding.UTF8.GetBytes("High Concurrency Test Payload");

        var targets = new List<DeliveryTargetConfig>
        {
            new S3DeliveryConfig { BucketName = "production-dossier" },
            new AzureBlobDeliveryConfig { ContainerName = "dossiers" },
            new WebhookDeliveryConfig { WebhookUrl = "https://events.enterprise.internal/reports" }
        };

        var metadata = new Dictionary<string, object?> { ["ReportId"] = "REP-001" };

        var results = await dispatcher.DispatchAsync(payload, "Dossier_2026.pdf", targets, metadata);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }
}
