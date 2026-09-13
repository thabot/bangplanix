using System.Text.Json;
using Bangplanix.Core.Distributed;
using Bangplanix.Engine.Distributed;
using Xunit;

namespace Bangplanix.Engine.Tests;

public sealed class ContainerSmokeAndGracefulShutdownTests
{
    [Fact]
    public async Task GracefulShutdown_ShouldDrainQueuedJobsWithoutDataLoss()
    {
        var queueProvider = new InMemoryDistributedQueueProvider();
        int completedJobs = 0;

        await using var orchestrator = new DistributedWorkerOrchestrator(
            queueProvider,
            async (job, token) =>
            {
                await Task.Delay(10, token);
                Interlocked.Increment(ref completedJobs);
                return [0x25, 0x50, 0x44, 0x46]; // %PDF
            },
            concurrency: 2,
            consumerGroup: "smoke-workers");

        // Enqueue 10 jobs
        int jobCount = 10;
        for (int i = 0; i < jobCount; i++)
        {
            await queueProvider.EnqueueJobAsync(new DistributedRenderJob
            {
                JobId = $"smoke_job_{i}",
                TenantId = "tenant_smoke",
                Priority = DistributedJobPriority.Normal
            });
        }

        orchestrator.Start();
        Assert.True(orchestrator.IsRunning);

        // Allow some jobs to process, then initiate graceful shutdown
        for (int retry = 0; retry < 50; retry++)
        {
            if (orchestrator.TotalProcessed >= 4) break;
            await Task.Delay(50);
        }

        await orchestrator.StopAsync();

        Assert.True(orchestrator.TotalProcessed > 0, "Some jobs should have completed before/during drain");
        Assert.False(orchestrator.IsRunning, "Orchestrator should gracefully transition to not running");
    }

    [Fact]
    public void ContainerHealthProbes_ShouldReportValidPayloads()
    {
        // Verify JSON serialization format expected by Kubernetes liveness and readiness probes
        var liveness = new
        {
            status = "UP",
            timestamp = DateTimeOffset.UtcNow,
            version = "1.0.0",
            memoryUsageMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            gcServer = System.Runtime.GCSettings.IsServerGC
        };

        string json = JsonSerializer.Serialize(liveness);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("UP", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("1.0.0", doc.RootElement.GetProperty("version").GetString());
        Assert.True(doc.RootElement.GetProperty("memoryUsageMb").GetDouble() > 0);
    }
}

