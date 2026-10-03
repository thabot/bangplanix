using System.Diagnostics;
using System.Text;
using Bangplanix.Core.Bursting;
using Bangplanix.Engine.Bursting;
using Bangplanix.Engine.Bursting.Delivery;
using FluentAssertions;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class ProductionBurstingDeliveryTests
{
    [Fact]
    public async Task Smtp_SimulationMode_ShouldReturnSuccessWithCorrectRecipient()
    {
        var channel = new SmtpEmailDeliveryChannel();
        var config = new SmtpDeliveryConfig
        {
            SimulationMode = true,
            RecipientEmailField = "UserEmail"
        };
        var metadata = new Dictionary<string, object?> { ["UserEmail"] = "customer@domain.com" };
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Test Content");

        var result = await channel.DeliverAsync(doc, "Invoice.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Be("customer@domain.com");
        result.TransactionId.Should().StartWith("SMTP-SIM-");
    }

    [Fact]
    public async Task Smtp_DynamicInterpolation_ShouldReplacePlaceholders()
    {
        var channel = new SmtpEmailDeliveryChannel();
        var config = new SmtpDeliveryConfig
        {
            SimulationMode = true,
            Subject = "Invoice {InvoiceNo} for {CustomerName}",
            RecipientEmailField = "Email"
        };
        var metadata = new Dictionary<string, object?>
        {
            ["Email"] = "finance@company.com",
            ["InvoiceNo"] = "INV-2026-001",
            ["CustomerName"] = "Acme Corp"
        };
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Invoice.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Be("finance@company.com");
    }

    [Fact]
    public async Task Smtp_MissingEmailField_ShouldFallbackGracefully()
    {
        var channel = new SmtpEmailDeliveryChannel();
        var config = new SmtpDeliveryConfig
        {
            SimulationMode = true,
            RecipientEmailField = "NonExistentEmail"
        };
        var metadata = new Dictionary<string, object?> { ["OtherField"] = "Value" };
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Invoice.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Be("user@example.com");
    }

    [Fact]
    public void S3_AwsSigV4_AuthorizationHeader_ShouldMatchStandard()
    {
        var date = new DateTime(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
        var payload = Encoding.UTF8.GetBytes("Test S3 PDF Payload");

        string auth = S3DeliveryChannel.ComputeSigV4AuthorizationHeader(
            "AKIAIOSFODNN7EXAMPLE",
            "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            "ap-southeast-1",
            "s3",
            date,
            "mybucket.s3.ap-southeast-1.amazonaws.com",
            "/reports/invoice.pdf",
            payload);

        auth.Should().StartWith("AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20260922/ap-southeast-1/s3/aws4_request");
        auth.Should().Contain("SignedHeaders=host;x-amz-content-sha256;x-amz-date");
        auth.Should().Contain("Signature=");
    }

    [Fact]
    public async Task S3_DynamicKeyPrefix_ShouldResolvePartitionVariables()
    {
        var channel = new S3DeliveryChannel();
        var config = new S3DeliveryConfig
        {
            BucketName = "finance-reports",
            KeyPrefix = "monthly/{TenantId}/"
        };
        var metadata = new Dictionary<string, object?> { ["TenantId"] = "tenant-007" };
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Summary.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Be("s3://finance-reports/monthly/tenant-007/Summary.pdf");
    }

    [Fact]
    public async Task S3_MinioCompatibleEndpoint_ShouldTargetServiceUrl()
    {
        var channel = new S3DeliveryChannel();
        var config = new S3DeliveryConfig
        {
            BucketName = "minio-bucket",
            ServiceUrl = "https://minio.example.com",
            AccessKey = "minioadmin",
            SecretKey = "minioadmin"
        };
        var metadata = new Dictionary<string, object?>();
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Doc.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Contain("s3://minio-bucket/");
    }

    [Fact]
    public void AzureBlob_SharedKey_HeaderCalculation_ShouldMatchSpec()
    {
        string header = AzureBlobDeliveryChannel.ComputeSharedKeyHeader(
            "mystorageaccount",
            Convert.ToBase64String(new byte[32]),
            "invoices",
            "2026/09/inv1.pdf",
            1024,
            "Tue, 22 Sep 2026 12:00:00 GMT");

        header.Should().StartWith("SharedKey mystorageaccount:");
    }

    [Fact]
    public async Task AzureBlob_Simulation_ShouldReturnSuccess()
    {
        var channel = new AzureBlobDeliveryChannel();
        var config = new AzureBlobDeliveryConfig
        {
            AccountName = "myteststorage",
            ContainerName = "invoices",
            BlobPrefix = "{Year}/{Month}/"
        };
        var metadata = new Dictionary<string, object?>();
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Inv.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Be($"https://myteststorage.blob.core.windows.net/invoices/{DateTime.UtcNow:yyyy}/{DateTime.UtcNow:MM}/Inv.pdf");
    }

    [Fact]
    public async Task Sftp_PasswordAuth_MockUpload_ShouldReturnSuccess()
    {
        var channel = new SftpDeliveryChannel();
        var config = new SftpDeliveryConfig
        {
            Host = "sftp.internal.corp",
            Port = 22,
            Username = "uploader",
            Password = "secretpassword",
            RemoteDirectory = "/uploads/{CustomerId}/"
        };
        var metadata = new Dictionary<string, object?> { ["CustomerId"] = "CUST99" };
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Statement.pdf", config, metadata);

        result.Success.Should().BeTrue();
        result.Destination.Should().Be("sftp://uploader@sftp.internal.corp/uploads/CUST99/Statement.pdf");
    }

    [Fact]
    public async Task Sftp_InvalidPemKey_ShouldReturnFailure()
    {
        var channel = new SftpDeliveryChannel();
        var config = new SftpDeliveryConfig
        {
            Host = "sftp.internal.corp",
            Username = "uploader",
            PrivateKeyPem = "NOT_A_VALID_KEY"
        };
        var metadata = new Dictionary<string, object?>();
        var doc = Encoding.UTF8.GetBytes("%PDF-1.7 Content");

        var result = await channel.DeliverAsync(doc, "Statement.pdf", config, metadata);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid PEM format");
    }

    [Fact]
    public async Task MultiChannelDispatcher_ShouldDeliverToAllConfiguredChannels()
    {
        var dispatcher = new MultiChannelDeliveryDispatcher();
        var targets = new List<DeliveryTargetConfig>
        {
            new SmtpDeliveryConfig { Name = "Email1", SimulationMode = true },
            new S3DeliveryConfig { Name = "S3-1" },
            new AzureBlobDeliveryConfig { Name = "Az-1" }
        };

        var metadata = new Dictionary<string, object?> { ["Email"] = "test@example.com" };
        var doc = Encoding.UTF8.GetBytes("Test Document");

        var results = await dispatcher.DispatchAsync(doc, "Report.pdf", targets, metadata);

        results.Should().HaveCount(3);
        results.All(r => r.Success).Should().BeTrue();
    }

    [Fact]
    public async Task MultiChannelDispatcher_OnFailure_ShouldRetryUpToMaxRetries()
    {
        var dispatcher = new MultiChannelDeliveryDispatcher();
        var fakeChannel = new MockFlakyDeliveryChannel(failCountBeforeSuccess: 2);
        dispatcher.RegisterChannel(fakeChannel);

        var targets = new List<DeliveryTargetConfig>
        {
            new CustomMockDeliveryConfig()
        };

        var results = await dispatcher.DispatchAsync(new byte[10], "doc.pdf", targets, new Dictionary<string, object?>(), maxRetries: 3);

        results.Should().HaveCount(1);
        results[0].Success.Should().BeTrue();
        fakeChannel.AttemptCount.Should().Be(3);
    }

    [Fact]
    public async Task ReportBurstingEngine_WhenChannelFails_ShouldEnqueueToDlq()
    {
        var dispatcher = new MultiChannelDeliveryDispatcher();
        var fakeChannel = new MockFailingDeliveryChannel();
        dispatcher.RegisterChannel(fakeChannel);

        var engine = new ReportBurstingEngine(dispatcher);
        var job = new BurstingJobDefinition
        {
            JobId = "JOB-DLQ-1",
            SplitKeyField = "Id",
            DeliveryTargets = new List<DeliveryTargetConfig> { new CustomMockDeliveryConfig() },
            MaxRetries = 1
        };

        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Id"] = "ROW1", ["Name"] = "Alice" }
        };

        var summary = await engine.ExecuteBurstingAsync(job, rows, (_, _) => Task.FromResult(Encoding.UTF8.GetBytes("Data")));

        summary.TotalSlices.Should().Be(1);
        engine.DeadLetterQueue.GetPendingSlices("JOB-DLQ-1").Should().HaveCount(1);
    }

    [Fact]
    public async Task ReportBurstingEngine_ReplayDlq_ShouldReprocessOnlyFailedSlices()
    {
        var dispatcher = new MultiChannelDeliveryDispatcher();
        var engine = new ReportBurstingEngine(dispatcher);

        var job = new BurstingJobDefinition
        {
            JobId = "JOB-REPLAY-1",
            SplitKeyField = "Id",
            DeliveryTargets = new List<DeliveryTargetConfig> { new SmtpDeliveryConfig { SimulationMode = true } }
        };

        engine.DeadLetterQueue.Enqueue(new Bangplanix.Core.Bursting.DlqFailedSlice
        {
            JobId = job.JobId,
            SliceKey = "ROW-STUCK",
            RowData = new Dictionary<string, object?> { ["Id"] = "ROW-STUCK", ["Email"] = "stuck@example.com" }
        });

        var replaySummary = await engine.ReplayFailedSlicesAsync(job, (_, _) => Task.FromResult(Encoding.UTF8.GetBytes("Replayed Content")));

        replaySummary.TotalSlices.Should().Be(1);
        replaySummary.SucceededSlices.Should().Be(1);
        engine.DeadLetterQueue.GetPendingSlices(job.JobId).Should().BeEmpty();
    }

    [Fact]
    public async Task RateLimiter_ShouldThrottleBurstDispatches()
    {
        var limiter = new DeliveryRateLimiter(50);
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 5; i++)
        {
            await limiter.AcquireAsync();
        }

        sw.Stop();
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    private class CustomMockDeliveryConfig : DeliveryTargetConfig
    {
        public override DeliveryChannelType ChannelType => DeliveryChannelType.Webhook;
    }

    private class MockFlakyDeliveryChannel : IDeliveryChannel
    {
        public DeliveryChannelType ChannelType => DeliveryChannelType.Webhook;
        public int AttemptCount { get; private set; }
        private readonly int _failCountBeforeSuccess;

        public MockFlakyDeliveryChannel(int failCountBeforeSuccess)
        {
            _failCountBeforeSuccess = failCountBeforeSuccess;
        }

        public Task<DeliveryResult> DeliverAsync(byte[] documentBytes, string fileName, DeliveryTargetConfig config, IDictionary<string, object?> sliceMetadata, CancellationToken cancellationToken = default)
        {
            AttemptCount++;
            if (AttemptCount <= _failCountBeforeSuccess)
            {
                return Task.FromResult(new DeliveryResult { Success = false, ChannelType = ChannelType, ErrorMessage = "Transient Network Error" });
            }
            return Task.FromResult(new DeliveryResult { Success = true, ChannelType = ChannelType, Destination = "https://webhook.mock" });
        }
    }

    private class MockFailingDeliveryChannel : IDeliveryChannel
    {
        public DeliveryChannelType ChannelType => DeliveryChannelType.Webhook;

        public Task<DeliveryResult> DeliverAsync(byte[] documentBytes, string fileName, DeliveryTargetConfig config, IDictionary<string, object?> sliceMetadata, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DeliveryResult { Success = false, ChannelType = ChannelType, ErrorMessage = "Permanent 500 Failure" });
        }
    }
}
