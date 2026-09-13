using System.Text;
using Bangplanix.Core.Bursting;
using Bangplanix.Engine.Bursting;
using Bangplanix.Engine.Bursting.Delivery;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class ReportBurstingAndSchedulingTests
{
    // --- 1. Cron Expression Parser Tests (Task 4.3.1) ---
    [Fact]
    public void CronExpressionParser_ShouldParseStandardAndMacroExpressions()
    {
        var daily = new CronExpressionParser("@daily");
        Assert.Equal("0 0 * * *", daily.Expression);

        var hourly = new CronExpressionParser("@hourly");
        Assert.Equal("0 * * * *", hourly.Expression);

        var custom = new CronExpressionParser("*/15 9-17 * * 1-5");
        Assert.Equal("*/15 9-17 * * 1-5", custom.Expression);

        DateTime refTime = new DateTime(2026, 10, 1, 8, 30, 0, DateTimeKind.Utc);
        var nextOccur = custom.GetNextOccurrenceUtc(refTime);

        Assert.NotNull(nextOccur);
        Assert.True(nextOccur.Value > refTime);
        Assert.Equal(9, nextOccur.Value.Hour);
        Assert.Equal(0, nextOccur.Value.Minute);
    }

    [Fact]
    public void CronExpressionParser_ShouldThrowOnMalformedExpression()
    {
        Assert.Throws<FormatException>(() => new CronExpressionParser("invalid cron string"));
        Assert.Throws<ArgumentException>(() => new CronExpressionParser(""));
    }

    // --- 2. Report Bursting Engine Tests (Task 4.3.2) ---
    [Fact]
    public async Task ReportBurstingEngine_ShouldPartitionAndRenderSlicesInParallel()
    {
        var engine = new ReportBurstingEngine();

        // 100 rows across 10 distinct customers (10 rows per customer)
        var masterData = new List<Dictionary<string, object?>>();
        for (int i = 0; i < 100; i++)
        {
            string custId = $"CUST_{(i % 10):D3}";
            masterData.Add(new Dictionary<string, object?>
            {
                ["CustomerId"] = custId,
                ["CustomerName"] = $"Customer {custId}",
                ["Email"] = $"{custId}@company.com",
                ["InvoiceNo"] = $"INV-2026-{i:D4}",
                ["Amount"] = 1500.00m + i
            });
        }

        var job = new BurstingJobDefinition
        {
            JobId = "job_burst_01",
            JobName = "Monthly Invoices Burst",
            SplitKeyField = "CustomerId",
            FileNamePattern = "Invoice_{CustomerId}_{Date}.pdf",
            ConcurrencyLimit = 4,
            DeliveryTargets = new List<DeliveryTargetConfig>
            {
                new SmtpDeliveryConfig { RecipientEmailField = "Email" },
                new S3DeliveryConfig { BucketName = "finance-invoices" }
            }
        };

        var summary = await engine.ExecuteBurstingAsync(
            job,
            masterData,
            async (sliceKey, row) =>
            {
                await Task.Delay(2); // simulate render
                return Encoding.UTF8.GetBytes($"%PDF-1.7 Rendered Invoice for {sliceKey}");
            });

        Assert.Equal("job_burst_01", summary.JobId);
        Assert.Equal(10, summary.TotalSlices);
        Assert.Equal(10, summary.SucceededSlices);
        Assert.Equal(0, summary.FailedSlices);
        Assert.True(summary.ThroughputPerSecond > 0);

        var firstSlice = summary.Slices.First();
        Assert.StartsWith("Invoice_CUST_000_", firstSlice.OutputFileName);
        Assert.Equal(10, firstSlice.RowsInSlice);
        Assert.Equal(2, firstSlice.DeliveryResults.Count);
        Assert.All(firstSlice.DeliveryResults, d => Assert.True(d.Success));
    }

    // --- 3. Multi-Channel Delivery Providers (Task 4.3.3) ---
    [Fact]
    public async Task MultiChannelDeliveryDispatcher_ShouldDispatchToAllChannels()
    {
        var dispatcher = new MultiChannelDeliveryDispatcher();
        byte[] docBytes = Encoding.UTF8.GetBytes("Test Document Content");
        string fileName = "Statement_202610.pdf";

        var sliceMeta = new Dictionary<string, object?>
        {
            ["Email"] = "billing@enterprise.com",
            ["Period"] = "2026-10",
            ["Year"] = "2026",
            ["Month"] = "10"
        };

        var targets = new List<DeliveryTargetConfig>
        {
            new SmtpDeliveryConfig { RecipientEmailField = "Email" },
            new S3DeliveryConfig { BucketName = "statements", KeyPrefix = "archive/{Period}/" },
            new AzureBlobDeliveryConfig { ContainerName = "reports", BlobPrefix = "{Year}/{Month}/" },
            new SftpDeliveryConfig { Host = "sftp.bank.com", Username = "fin_ops" },
            new WebhookDeliveryConfig { WebhookUrl = "https://mock.service.com/webhook", HmacSecretKey = "super_hmac_secret_key" }
        };

        var results = await dispatcher.DispatchAsync(docBytes, fileName, targets, sliceMeta);

        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
        Assert.Contains(results, r => r.ChannelType == DeliveryChannelType.SmtpEmail && r.Destination == "billing@enterprise.com");
        Assert.Contains(results, r => r.ChannelType == DeliveryChannelType.AwsS3 && r.Destination.Contains("s3://statements/archive/2026-10/"));
        Assert.Contains(results, r => r.ChannelType == DeliveryChannelType.AzureBlob && r.Destination.Contains("https://azureblob.storage/reports/2026/10/"));
        Assert.Contains(results, r => r.ChannelType == DeliveryChannelType.Sftp && r.Destination.Contains("sftp://fin_ops@sftp.bank.com"));
        Assert.Contains(results, r => r.ChannelType == DeliveryChannelType.Webhook);
    }

    // --- 4. Cron Job Scheduler Engine Lifecycle (Task 4.3.1) ---
    [Fact]
    public async Task CronJobSchedulerEngine_ShouldManageJobsAndProcessDueRuns()
    {
        var scheduler = new CronJobSchedulerEngine();

        var job = new BurstingJobDefinition
        {
            JobId = "job_scheduled_payroll",
            JobName = "Bi-weekly Payroll Burst",
            CronExpression = "0 8 1,15 * *", // 1st and 15th at 08:00
            SplitKeyField = "EmployeeId",
            FileNamePattern = "Payslip_{EmployeeId}.pdf"
        };

        scheduler.ScheduleJob(job);
        Assert.Single(scheduler.GetJobs());
        Assert.NotNull(job.NextRunUtc);
        Assert.Equal(ScheduledJobStatus.Active, job.Status);

        // Pause & Resume
        scheduler.PauseJob(job.JobId);
        Assert.Equal(ScheduledJobStatus.Paused, scheduler.GetJob(job.JobId)?.Status);

        scheduler.ResumeJob(job.JobId);
        Assert.Equal(ScheduledJobStatus.Active, scheduler.GetJob(job.JobId)?.Status);

        // Process Due Jobs
        DateTime simulatedTriggerTime = job.NextRunUtc.Value.AddMinutes(1);
        var summaries = await scheduler.ProcessDueJobsAsync(
            simulatedTriggerTime,
            jobDef => Task.FromResult<IEnumerable<IDictionary<string, object?>>>(new List<Dictionary<string, object?>>
            {
                new() { ["EmployeeId"] = "EMP001", ["Name"] = "Alice", ["Salary"] = 65000 },
                new() { ["EmployeeId"] = "EMP002", ["Name"] = "Bob", ["Salary"] = 72000 }
            }),
            (sliceKey, row) => Task.FromResult(Encoding.UTF8.GetBytes($"Payslip PDF for {sliceKey}")));

        Assert.Single(summaries);
        Assert.Equal(2, summaries[0].TotalSlices);
        Assert.Equal(2, summaries[0].SucceededSlices);

        // History check
        var history = scheduler.GetJobHistory(job.JobId);
        Assert.Single(history);
        Assert.Equal(2, history[0].TotalSlices);

        // Delete job
        scheduler.DeleteJob(job.JobId);
        Assert.Empty(scheduler.GetJobs());
    }

    // --- 5. Dead-Letter Queue (DLQ) & 1-Click Replay (Task 4.3.4) ---
    [Fact]
    public async Task BurstingDeadLetterQueue_ShouldEnqueueFailedSlicesAndReplaySuccessfully()
    {
        var dlq = new BurstingDeadLetterQueue();
        var engine = new ReportBurstingEngine(dlq: dlq);

        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["CustomerId"] = "OK_001", ["Email"] = "ok1@corp.com" },
            new() { ["CustomerId"] = "FAIL_002", ["Email"] = "fail2@corp.com" },
            new() { ["CustomerId"] = "OK_003", ["Email"] = "ok3@corp.com" }
        };

        var job = new BurstingJobDefinition
        {
            JobId = "job_dlq_test",
            SplitKeyField = "CustomerId",
            DeliveryTargets = new List<DeliveryTargetConfig>
            {
                new SmtpDeliveryConfig { RecipientEmailField = "Email" }
            }
        };

        // First run: FAIL_002 throws an exception during render
        var summary1 = await engine.ExecuteBurstingAsync(
            job,
            rows,
            (key, row) =>
            {
                if (key == "FAIL_002") throw new InvalidOperationException("Simulated corrupted template for FAIL_002");
                return Task.FromResult(Encoding.UTF8.GetBytes($"Report for {key}"));
            });

        Assert.Equal(3, summary1.TotalSlices);
        Assert.Equal(2, summary1.SucceededSlices);
        Assert.Equal(1, summary1.FailedSlices);

        // Verify DLQ state
        var pending = dlq.GetPendingSlices(job.JobId);
        Assert.Single(pending);
        Assert.Equal("FAIL_002", pending[0].SliceKey);
        Assert.Contains("Simulated corrupted template", pending[0].ErrorMessage);

        // Replay failed slices (now render succeeds)
        var replaySummary = await engine.ReplayFailedSlicesAsync(
            job,
            (key, row) => Task.FromResult(Encoding.UTF8.GetBytes($"Recovered Report for {key}")));

        Assert.Single(replaySummary.Slices);
        Assert.True(replaySummary.Slices[0].OverallSuccess);
        Assert.Equal(0, dlq.GetPendingCount(job.JobId));
    }

    // --- 6. Confidential Per-Recipient Dynamic Password Encryption (Task 4.3.5) ---
    [Fact]
    public void RecipientPasswordEncryption_ShouldGenerateDynamicPasswordsAndEncryptBytes()
    {
        var policy = new RecipientSecurityPolicy
        {
            PasswordPattern = "TH-{NationalId:Last4}-{Year}",
            FallbackPassword = "Default@2026"
        };

        var row = new Dictionary<string, object?>
        {
            ["NationalId"] = "1100200345678",
            ["Year"] = "2026",
            ["EmployeeName"] = "Somchai"
        };

        string pwd = RecipientPasswordEncryptionEngine.GenerateRecipientPassword(policy, row);
        Assert.Equal("TH-5678-2026", pwd);

        byte[] plainBytes = Encoding.UTF8.GetBytes("Confidential Salary Details: 120,000 THB");
        byte[] encryptedBytes = RecipientPasswordEncryptionEngine.EncryptDocumentBytes(plainBytes, pwd);

        Assert.NotEmpty(encryptedBytes);
        Assert.NotEqual(plainBytes, encryptedBytes);
        Assert.True(encryptedBytes.Length > plainBytes.Length); // Due to 16-byte salt + AES block padding
    }

    // --- 7. Egress Delivery Rate Limiter (Task 4.3.6) ---
    [Fact]
    public async Task DeliveryRateLimiter_ShouldThrottleDispatches()
    {
        var limiter = new DeliveryRateLimiter(maxDispatchesPerSecond: 100);

        // Rapidly acquire 10 tokens
        for (int i = 0; i < 10; i++)
        {
            await limiter.AcquireAsync();
        }

        Assert.True(limiter.IsEnabled);
    }

    // --- 8. Department Zip Bundler & Manifest Generator (Task 4.3.7) ---
    [Fact]
    public void DepartmentZipBundler_ShouldCreateValidZipWithManifest()
    {
        var slices = new List<(string SliceKey, string FileName, byte[] DocumentBytes)>
        {
            ("EMP_001", "Payslip_EMP_001.pdf", Encoding.UTF8.GetBytes("PDF Content for EMP_001")),
            ("EMP_002", "Payslip_EMP_002.pdf", Encoding.UTF8.GetBytes("PDF Content for EMP_002")),
            ("EMP_003", "Payslip_EMP_003.pdf", Encoding.UTF8.GetBytes("PDF Content for EMP_003"))
        };

        byte[] zipArchive = DepartmentZipBundler.CreateZipBundle("Finance_Dept", slices);

        Assert.NotNull(zipArchive);
        Assert.True(zipArchive.Length > 0);

        // Verify Zip header 'PK\x03\x04'
        Assert.Equal(0x50, zipArchive[0]); // 'P'
        Assert.Equal(0x4B, zipArchive[1]); // 'K'
        Assert.Equal(0x03, zipArchive[2]);
        Assert.Equal(0x04, zipArchive[3]);
    }
}

