using System.Collections.Concurrent;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting;

/// <summary>
/// Built-in Cron Job Scheduler Engine managing recurrent report generation and bursting execution.
/// </summary>
public sealed class CronJobSchedulerEngine
{
    private readonly ConcurrentDictionary<string, BurstingJobDefinition> _jobs = new();
    private readonly ConcurrentDictionary<string, List<BurstingExecutionSummary>> _history = new();
    private readonly ReportBurstingEngine _burstingEngine;

    public CronJobSchedulerEngine(ReportBurstingEngine? burstingEngine = null)
    {
        _burstingEngine = burstingEngine ?? new ReportBurstingEngine();
    }

    /// <summary>
    /// Schedules or registers a new bursting job.
    /// </summary>
    public void ScheduleJob(BurstingJobDefinition job)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));

        var parser = new CronExpressionParser(job.CronExpression);
        job.NextRunUtc = parser.GetNextOccurrenceUtc(DateTime.UtcNow);
        job.Status = ScheduledJobStatus.Active;

        _jobs[job.JobId] = job;
    }

    /// <summary>
    /// Retrieves all active and registered scheduled jobs.
    /// </summary>
    public IReadOnlyList<BurstingJobDefinition> GetJobs() => _jobs.Values.ToList();

    public BurstingJobDefinition? GetJob(string jobId) => _jobs.TryGetValue(jobId, out var job) ? job : null;

    public bool PauseJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = ScheduledJobStatus.Paused;
            return true;
        }
        return false;
    }

    public bool ResumeJob(string jobId)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            var parser = new CronExpressionParser(job.CronExpression);
            job.NextRunUtc = parser.GetNextOccurrenceUtc(DateTime.UtcNow);
            job.Status = ScheduledJobStatus.Active;
            return true;
        }
        return false;
    }

    public bool DeleteJob(string jobId) => _jobs.TryRemove(jobId, out _);

    /// <summary>
    /// Checks and executes all jobs that are due for execution as of the given timestamp.
    /// </summary>
    public async Task<List<BurstingExecutionSummary>> ProcessDueJobsAsync(
        DateTime nowUtc,
        Func<BurstingJobDefinition, Task<IEnumerable<IDictionary<string, object?>>>> fetchDatasetCallback,
        Func<string, IDictionary<string, object?>, Task<byte[]>> renderReportCallback,
        CancellationToken cancellationToken = default)
    {
        var dueJobs = _jobs.Values
            .Where(j => j.Status == ScheduledJobStatus.Active && j.NextRunUtc.HasValue && j.NextRunUtc.Value <= nowUtc)
            .ToList();

        var summaries = new List<BurstingExecutionSummary>();

        foreach (var job in dueJobs)
        {
            job.Status = ScheduledJobStatus.Running;
            try
            {
                var masterRows = await fetchDatasetCallback(job);
                var summary = await _burstingEngine.ExecuteBurstingAsync(job, masterRows, renderReportCallback, cancellationToken: cancellationToken);

                job.LastRunUtc = nowUtc;
                var parser = new CronExpressionParser(job.CronExpression);
                job.NextRunUtc = parser.GetNextOccurrenceUtc(nowUtc);
                job.Status = ScheduledJobStatus.Active;

                var historyList = _history.GetOrAdd(job.JobId, _ => new List<BurstingExecutionSummary>());
                lock (historyList)
                {
                    historyList.Add(summary);
                    if (historyList.Count > 100) historyList.RemoveAt(0);
                }

                summaries.Add(summary);
            }
            catch (Exception)
            {
                job.Status = ScheduledJobStatus.Failed;
            }
        }

        return summaries;
    }

    /// <summary>
    /// Immediately triggers an ad-hoc run for a scheduled job.
    /// </summary>
    public async Task<BurstingExecutionSummary> TriggerJobNowAsync(
        string jobId,
        IEnumerable<IDictionary<string, object?>> masterRows,
        Func<string, IDictionary<string, object?>, Task<byte[]>> renderReportCallback,
        CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out var job))
        {
            throw new KeyNotFoundException($"Scheduled job '{jobId}' not found.");
        }

        var summary = await _burstingEngine.ExecuteBurstingAsync(job, masterRows, renderReportCallback, cancellationToken: cancellationToken);
        job.LastRunUtc = DateTime.UtcNow;

        var historyList = _history.GetOrAdd(job.JobId, _ => new List<BurstingExecutionSummary>());
        lock (historyList)
        {
            historyList.Add(summary);
        }

        return summary;
    }

    /// <summary>
    /// Retrieves execution run history for a job.
    /// </summary>
    public IReadOnlyList<BurstingExecutionSummary> GetJobHistory(string jobId)
    {
        return _history.TryGetValue(jobId, out var list) ? list : Array.Empty<BurstingExecutionSummary>();
    }
}
