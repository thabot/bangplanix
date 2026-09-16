using System.Text;
using Bangplanix.Core.Distributed;
using Bangplanix.Core.Licensing;
using Bangplanix.Engine.Distributed;
using Bangplanix.Engine.Licensing;
using Bangplanix.Engine.Portal;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class DistributedWorkerAndLicensingTests
{
    // --- 1. Distributed Worker Subsystem & Queue Tests (Task 4.4.2) ---
    [Fact]
    public async Task DistributedQueue_ShouldEnqueueDequeueAndAcknowledgeJobs()
    {
        var queue = new InMemoryDistributedQueueProvider(capacity: 100);

        var job = new DistributedRenderJob
        {
            JobId = "dist_job_001",
            TenantId = "tenant_acme",
            OutputFormat = "pdf",
            Priority = DistributedJobPriority.High,
            MaxRetryCount = 2
        };

        string enqueuedId = await queue.EnqueueJobAsync(job);
        Assert.Equal("dist_job_001", enqueuedId);
        Assert.Equal(1, await queue.GetQueueDepthAsync());

        // Worker claims job
        var claimed = await queue.DequeueJobAsync("group-1", "worker-alpha", TimeSpan.FromSeconds(1));
        Assert.NotNull(claimed);
        Assert.Equal("dist_job_001", claimed.JobId);
        Assert.Equal("worker-alpha", claimed.WorkerNodeId);
        Assert.Equal(DistributedJobStatus.Processing, claimed.Status);

        // Acknowledge
        await queue.AcknowledgeJobAsync("group-1", claimed.JobId);
        Assert.Equal(0, await queue.GetQueueDepthAsync());
        Assert.Equal(0, await queue.GetDeadLetterCountAsync());
    }

    [Fact]
    public async Task DistributedQueue_ShouldRetryAndDeadLetterAfterMaxRetries()
    {
        var queue = new InMemoryDistributedQueueProvider(capacity: 100);

        var job = new DistributedRenderJob
        {
            JobId = "failing_job_002",
            MaxRetryCount = 1
        };

        await queue.EnqueueJobAsync(job);

        // First attempt fails -> retry
        var claimed1 = await queue.DequeueJobAsync("group-1", "worker-1", TimeSpan.FromSeconds(1));
        Assert.NotNull(claimed1);
        await queue.RejectOrRetryJobAsync("group-1", claimed1, "Transient network timeout");
        Assert.Equal(1, claimed1.CurrentRetryCount);
        Assert.Equal(1, await queue.GetQueueDepthAsync());

        // Second attempt fails -> DLQ
        var claimed2 = await queue.DequeueJobAsync("group-1", "worker-1", TimeSpan.FromSeconds(1));
        Assert.NotNull(claimed2);
        await queue.RejectOrRetryJobAsync("group-1", claimed2, "Fatal error");
        Assert.Equal(2, claimed2.CurrentRetryCount);
        Assert.Equal(0, await queue.GetQueueDepthAsync());
        Assert.Equal(1, await queue.GetDeadLetterCountAsync());

        var deadLetters = queue.GetDeadLetterJobs();
        Assert.Single(deadLetters);
        Assert.Equal(DistributedJobStatus.DeadLettered, deadLetters[0].Status);
    }

    [Fact]
    public async Task DistributedWorkerOrchestrator_ShouldProcessJobsConcurrently()
    {
        var queue = new InMemoryDistributedQueueProvider();

        await using var orchestrator = new DistributedWorkerOrchestrator(
            queue,
            async (j, ct) =>
            {
                await Task.Delay(10, ct);
                return Encoding.UTF8.GetBytes($"Rendered {j.JobId}");
            },
            concurrency: 3,
            consumerGroup: "test-pool");

        orchestrator.Start();
        Assert.True(orchestrator.IsRunning);

        // Enqueue 6 jobs
        for (int i = 0; i < 6; i++)
        {
            await queue.EnqueueJobAsync(new DistributedRenderJob { JobId = $"batch_job_{i}" });
        }

        // Wait for workers to process
        for (int retry = 0; retry < 50; retry++)
        {
            if (orchestrator.TotalProcessed >= 6) break;
            await Task.Delay(50);
        }

        Assert.Equal(6, orchestrator.TotalProcessed);
        Assert.Equal(0, orchestrator.TotalFailed);
        Assert.Equal(0, await queue.GetQueueDepthAsync());

        await orchestrator.StopAsync();
        Assert.False(orchestrator.IsRunning);
    }

    // --- 2. Distributed Cloud Storage & Cache Provider (Task 4.4.3) ---
    [Fact]
    public async Task DistributedCloudStorageProvider_ShouldStoreCacheAndInvalidate()
    {
        var storage = new DistributedCloudStorageProvider(TimeSpan.FromMinutes(10));
        byte[] fontBytes = Encoding.UTF8.GetBytes("TTF Mock Font Data");

        await storage.PutFileAsync("fonts", "THSarabunNew.ttf", fontBytes);

        var retrieved = await storage.GetFileAsync("fonts", "THSarabunNew.ttf");
        Assert.Equal(fontBytes, retrieved);

        var files = await storage.ListFilesAsync("fonts");
        Assert.Contains("THSarabunNew.ttf", files);

        // Invalidate and delete
        storage.InvalidateCache("fonts", "THSarabunNew.ttf");
        await storage.DeleteFileAsync("fonts", "THSarabunNew.ttf");

        await Assert.ThrowsAsync<FileNotFoundException>(() => storage.GetFileAsync("fonts", "THSarabunNew.ttf"));
    }

    // --- 3. Quantum-Ready Commercial Licensing Enforcer (Task 4.4.5) ---
    [Fact]
    public void CommercialLicenseEnforcer_ShouldValidateSignedTokensAndEnforceEntitlements()
    {
        byte[] privateKey = "bangplanix_master_pub_key_2026_ed25519_dilithium"u8.ToArray();
        var enforcer = new CommercialLicenseEnforcer(publicKeyBytes: privateKey);

        // Community default
        var communityResult = enforcer.ApplyLicenseToken(null, currentHostCores: 4);
        Assert.True(communityResult.IsValid);
        Assert.Equal(LicenseTier.Community, communityResult.ActiveTier);
        Assert.True(enforcer.RequiresWatermark());
        Assert.False(enforcer.IsFeatureAllowed("AiSuite"));

        // Generate Signed Pro License (Max 8 cores)
        var proPayload = new LicensePayload
        {
            LicenseId = "LIC-PRO-001",
            CustomerName = "Fintech Corp",
            CustomerEmail = "dev@fintech.com",
            Tier = LicenseTier.Professional,
            MaxAllowedCores = 8,
            EnableWatermarking = false,
            EnableReportBursting = true,
            EnableAiSuite = false,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(1)
        };

        string signedProToken = CommercialLicenseEnforcer.GenerateSignedToken(proPayload, privateKey);
        var proResult = enforcer.ApplyLicenseToken(signedProToken, currentHostCores: 8);

        Assert.True(proResult.IsValid);
        Assert.Equal(LicenseTier.Professional, proResult.ActiveTier);
        Assert.False(enforcer.RequiresWatermark());
        Assert.True(enforcer.IsFeatureAllowed("Bursting"));
        Assert.False(enforcer.IsFeatureAllowed("AiSuite"));

        // Core limit exceeded for Pro on 16-core machine
        var coreExceededResult = enforcer.ApplyLicenseToken(signedProToken, currentHostCores: 16);
        Assert.False(coreExceededResult.IsValid);
        Assert.True(coreExceededResult.CoreLimitExceeded);

        // Generate Signed Enterprise License (Unlimited Cores + AI Suite)
        var entPayload = new LicensePayload
        {
            LicenseId = "LIC-ENT-999",
            CustomerName = "Sovereign Mega Bank",
            CustomerEmail = "ops@megabank.com",
            Tier = LicenseTier.Enterprise,
            MaxAllowedCores = 0, // Unlimited
            EnableWatermarking = false,
            EnableReportBursting = true,
            EnableAiSuite = true,
            EnableTrueVectorRedaction = true,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(1)
        };

        string signedEntToken = CommercialLicenseEnforcer.GenerateSignedToken(entPayload, privateKey);
        var entResult = enforcer.ApplyLicenseToken(signedEntToken, currentHostCores: 64);

        Assert.True(entResult.IsValid);
        Assert.Equal(LicenseTier.Enterprise, entResult.ActiveTier);
        Assert.False(enforcer.RequiresWatermark());
        Assert.True(enforcer.IsFeatureAllowed("AiSuite"));
        Assert.True(enforcer.IsFeatureAllowed("TrueVectorRedaction"));
    }

    [Fact]
    public void CommercialLicenseEnforcer_ShouldRejectTamperedAndExpiredTokens()
    {
        byte[] key = "bangplanix_master_pub_key_2026_ed25519_dilithium"u8.ToArray();
        var enforcer = new CommercialLicenseEnforcer(publicKeyBytes: key);

        var expiredPayload = new LicensePayload
        {
            LicenseId = "LIC-EXP-001",
            CustomerName = "Old Corp",
            Tier = LicenseTier.Professional,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        string expiredToken = CommercialLicenseEnforcer.GenerateSignedToken(expiredPayload, key);
        var result = enforcer.ApplyLicenseToken(expiredToken);

        Assert.False(result.IsValid);
        Assert.True(result.IsExpired);

        // Tampered token
        string tamperedToken = expiredToken[..^4] + "AAAA";
        var tamperedResult = enforcer.ApplyLicenseToken(tamperedToken);
        Assert.False(tamperedResult.IsValid);
    }

    [Fact]
    public void CommercialLicenseEnforcer_ShouldVerifyOfficialEcdsaAsymmetricKeys()
    {
        // 1. Official Private Key for signing (Server-side)
        const string testPrivateKeyPem = """
-----BEGIN EC PRIVATE KEY-----
MHcCAQEEICN10/9dT0eOWs3249mrTrCQujFEH99NvuZEyEtCw3L7oAoGCCqGSM49
AwEHoUQDQgAEifypBfJuRuE6r/q2tyBccAUvn+gE+zb+MXPSdh4GANAqfadgiqBU
3BAFa5olWD2DIZF88cBb8kgYskZuHjKkYg==
-----END EC PRIVATE KEY-----
""";

        // 2. Default Enforcer with embedded Official Public Key
        var enforcer = new CommercialLicenseEnforcer();

        var officialEnterprisePayload = new LicensePayload
        {
            LicenseId = "BPX-OFFICIAL-ENT-2026",
            CustomerName = "Enterprise Global Bank",
            CustomerEmail = "security@globalbank.com",
            Tier = LicenseTier.Enterprise,
            MaxAllowedCores = 0, // Unlimited
            EnableWatermarking = false,
            EnableReportBursting = true,
            EnableAiSuite = true,
            EnableTrueVectorRedaction = true,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(2)
        };

        // Sign with Private Key
        byte[] privateKeyBytes = System.Text.Encoding.UTF8.GetBytes(testPrivateKeyPem);
        string signedToken = CommercialLicenseEnforcer.GenerateSignedToken(officialEnterprisePayload, privateKeyBytes);

        // Verify with default enforcer (using embedded OfficialPublicKeyPem)
        var validationResult = enforcer.ApplyLicenseToken(signedToken, currentHostCores: 128);

        Assert.True(validationResult.IsValid);
        Assert.Equal(LicenseTier.Enterprise, validationResult.ActiveTier);
        Assert.False(enforcer.RequiresWatermark());
        Assert.True(enforcer.IsFeatureAllowed("AiSuite"));
        Assert.True(enforcer.IsFeatureAllowed("TrueVectorRedaction"));
        Assert.Contains("Enterprise Global Bank", validationResult.StatusMessage);

        // Tamper test: Alter payload string directly
        string[] tokenParts = signedToken.Split('.');
        byte[] tamperedPayloadBytes = Convert.FromBase64String(tokenParts[0]);
        string json = System.Text.Encoding.UTF8.GetString(tamperedPayloadBytes).Replace("Enterprise Global Bank", "Hacked Bank");
        string tamperedB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        string tamperedToken = $"{tamperedB64}.{tokenParts[1]}";

        var tamperedResult = enforcer.ApplyLicenseToken(tamperedToken);
        Assert.False(tamperedResult.IsValid);
        Assert.Equal(LicenseTier.Community, tamperedResult.ActiveTier);
    }

    // --- 4. Management Portal Telemetry & Health Checks (Task 4.4.4) ---
    [Fact]
    public void ManagementPortalServer_ShouldProvideEmbeddedHtmlDashboard()
    {
        string html = ManagementPortalServer.GetEmbeddedPortalHtml();
        Assert.NotNull(html);
        Assert.Contains("Bangplanix Cloud Management Portal", html);
        Assert.Contains("Active Queue Depth", html);
        Assert.Contains("Native AOT", html);
    }

    // --- 5. Distributed Leader Election & Lock Provider (Task 4.4.10 Add-on) ---
    [Fact]
    public async Task DistributedLock_ShouldEnforceMutualExclusionAndAutoRelease()
    {
        var lockProvider = new RedisDistributedLockProvider();
        string resourceKey = "cron_bursting_job_payroll";

        // Pod 1 acquires lock
        await using var handle1 = await lockProvider.TryAcquireLockAsync(
            resourceKey,
            leaseDuration: TimeSpan.FromSeconds(2),
            timeout: TimeSpan.FromMilliseconds(500));

        Assert.NotNull(handle1);
        Assert.True(handle1.IsAcquired);
        Assert.True(lockProvider.IsResourceLocked(resourceKey));

        // Pod 2 attempts to acquire same resource -> fails
        await using var handle2 = await lockProvider.TryAcquireLockAsync(
            resourceKey,
            leaseDuration: TimeSpan.FromSeconds(2),
            timeout: TimeSpan.FromMilliseconds(50));

        Assert.Null(handle2);

        // Pod 1 releases lock
        await handle1.DisposeAsync();
        Assert.False(lockProvider.IsResourceLocked(resourceKey));

        // Pod 2 can now acquire lock
        await using var handle3 = await lockProvider.TryAcquireLockAsync(
            resourceKey,
            leaseDuration: TimeSpan.FromSeconds(2),
            timeout: TimeSpan.FromMilliseconds(200));

        Assert.NotNull(handle3);
        Assert.True(handle3.IsAcquired);
    }

    // --- 6. Cross-Region Cloud Storage Replicator (Task 4.4.12 Add-on) ---
    [Fact]
    public async Task CrossRegionStorageReplicator_ShouldSyncFilesWithChecksumVerification()
    {
        var primaryStore = new DistributedCloudStorageProvider();
        var secondaryStore = new DistributedCloudStorageProvider();
        var replicator = new CrossRegionStorageReplicator(primaryStore, secondaryStore);

        byte[] templateContent = Encoding.UTF8.GetBytes("{\"report\": \"Corporate Invoice Template\"}");
        await primaryStore.PutFileAsync("templates", "invoice_v1.bpx", templateContent);

        // Replicate single file
        var result = await replicator.ReplicateFileAsync("templates", "invoice_v1.bpx");
        Assert.True(result.IsSynchronized);
        Assert.NotEmpty(result.PrimarySha256);
        Assert.Equal(result.PrimarySha256, result.SecondarySha256);

        // Verify secondary store has the file
        byte[] secondaryData = await secondaryStore.GetFileAsync("templates", "invoice_v1.bpx");
        Assert.Equal(templateContent, secondaryData);

        // Replicate category batch
        await primaryStore.PutFileAsync("templates", "statement_v2.bpx", Encoding.UTF8.GetBytes("{\"report\": \"Bank Statement\"}"));
        var categoryResults = await replicator.ReplicateCategoryAsync("templates");
        Assert.Equal(2, categoryResults.Count);
        Assert.All(categoryResults, r => Assert.True(r.IsSynchronized));
    }
}
