namespace Bangplanix.Core.Distributed;

/// <summary>
/// Interface abstraction for distributed broker queues (Redis Streams, RabbitMQ, In-Memory Channels).
/// </summary>
public interface IDistributedQueueProvider
{
    /// <summary>
    /// Enqueues a distributed rendering job.
    /// </summary>
    Task<string> EnqueueJobAsync(DistributedRenderJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues or claims the next available job for the worker group.
    /// </summary>
    Task<DistributedRenderJob?> DequeueJobAsync(string consumerGroup, string workerId, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges successful completion of a job.
    /// </summary>
    Task AcknowledgeJobAsync(string consumerGroup, string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-queues a failed job or forwards it to the dead-letter stream if retries are exhausted.
    /// </summary>
    Task RejectOrRetryJobAsync(string consumerGroup, DistributedRenderJob job, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current queue backlog depth.
    /// </summary>
    Task<long> GetQueueDepthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dead-letter backlog count.
    /// </summary>
    Task<long> GetDeadLetterCountAsync(CancellationToken cancellationToken = default);
}
