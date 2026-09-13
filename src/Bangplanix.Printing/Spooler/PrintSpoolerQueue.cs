using System.Collections.Concurrent;
using Bangplanix.Printing.Protocols;

namespace Bangplanix.Printing.Spooler;

public enum PrintJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
    Retrying
}

public sealed class PrintJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString("N");
    public string DocumentName { get; set; } = "Document";
    public byte[] Data { get; set; } = [];
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public string Protocol { get; set; } = "tcp";
    public int MaxRetries { get; set; } = 3;
    public int RetryCount { get; set; }
    public PrintJobStatus Status { get; set; } = PrintJobStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public sealed class PrintSpoolerQueue
{
    private readonly ConcurrentQueue<PrintJob> _queue = new();
    private readonly ConcurrentDictionary<string, PrintJob> _history = new();
    private readonly Dictionary<string, IPrinterProtocol> _protocols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tcp"] = new RawSocketPrinter(),
        ["raw"] = new RawSocketPrinter(),
        ["ipp"] = new IppPrinter(),
        ["lpr"] = new LprPrinter()
    };

    public string Enqueue(byte[] data, string host, int port = 9100, string protocol = "tcp", string documentName = "Report Job", int maxRetries = 3)
    {
        var job = new PrintJob
        {
            Data = data,
            Host = host,
            Port = port,
            Protocol = protocol,
            DocumentName = documentName,
            MaxRetries = maxRetries
        };

        _queue.Enqueue(job);
        _history[job.JobId] = job;
        return job.JobId;
    }

    public PrintJob? GetJob(string jobId)
    {
        _history.TryGetValue(jobId, out var job);
        return job;
    }

    public IReadOnlyList<PrintJob> GetAllJobs()
    {
        return _history.Values.OrderByDescending(j => j.CreatedAt).ToList();
    }

    public async Task<int> ProcessQueueAsync(CancellationToken cancellationToken = default)
    {
        int processedCount = 0;

        while (_queue.TryDequeue(out var job))
        {
            cancellationToken.ThrowIfCancellationRequested();
            job.Status = PrintJobStatus.Processing;

            try
            {
                var proto = _protocols.TryGetValue(job.Protocol, out var p) ? p : _protocols["tcp"];
                var success = await proto.PrintAsync(job.Data, job.Host, job.Port, cancellationToken).ConfigureAwait(false);

                if (success)
                {
                    job.Status = PrintJobStatus.Completed;
                    job.CompletedAt = DateTime.UtcNow;
                    processedCount++;
                }
                else
                {
                    HandleFailure(job, "Protocol returned false without exception");
                }
            }
            catch (Exception ex)
            {
                HandleFailure(job, ex.Message);
            }
        }

        return processedCount;
    }

    private void HandleFailure(PrintJob job, string error)
    {
        job.ErrorMessage = error;
        job.RetryCount++;

        if (job.RetryCount <= job.MaxRetries)
        {
            job.Status = PrintJobStatus.Retrying;
            _queue.Enqueue(job); // Re-queue
        }
        else
        {
            job.Status = PrintJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
        }
    }
}
