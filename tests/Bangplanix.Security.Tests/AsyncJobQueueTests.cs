using System.Threading.Tasks;
using Bangplanix.Core.Jobs;
using Xunit;

#pragma warning disable CA2007

namespace Bangplanix.Security.Tests;

public class AsyncJobQueueTests
{
    [Fact]
    public async Task AsyncJobQueueShouldProcessBatchJobsConcurrently()
    {
        await using var queue = new AsyncJobQueue(workerCount: 2);

        var job1 = await queue.EnqueueJobAsync("""{"title":"Invoice 1"}""");
        var job2 = await queue.EnqueueJobAsync("""{"title":"Invoice 2"}""");

        // Wait brief moment for worker pool to finish (up to 3 seconds)
        var retries = 0;
        while ((job1.Status != JobStatus.Completed || job2.Status != JobStatus.Completed) && retries < 100)
        {
            await Task.Delay(30);
            retries++;
        }

        Assert.Equal(JobStatus.Completed, job1.Status);
        Assert.Equal(100, job1.ProgressPercentage);
        Assert.NotNull(job1.OutputData);
        Assert.NotNull(job1.CompletedAtUtc);

        Assert.Equal(JobStatus.Completed, job2.Status);
        Assert.NotNull(queue.GetJobStatus(job1.JobId));
    }
}
