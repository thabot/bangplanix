using System.Collections.Concurrent;

namespace Bangplanix.Core.Ai;

/// <summary>
/// Aggregated token and cost metrics for a tenant.
/// </summary>
public sealed class TenantTokenMetrics
{
    public string TenantId { get; set; } = "default";
    public long TotalPromptTokens { get; set; }
    public long TotalCompletionTokens { get; set; }
    public long TotalTokens => TotalPromptTokens + TotalCompletionTokens;
    public double TotalCostUsd { get; set; }
    public double TotalCostThb => Math.Round(TotalCostUsd * UsdToThbRate, 2);
    public long DailyTokenLimit { get; set; } = 1_000_000;
    public long MonthlyTokenLimit { get; set; } = 25_000_000;
    public double MonthlyCostBudgetUsd { get; set; } = 100.0;
    public DateTime LastResetUtc { get; set; } = DateTime.UtcNow;

    public const double UsdToThbRate = 35.50;
}

/// <summary>
/// Thread-safe Governor managing Token budgets, rate quotas, and real-time cost accounting per tenant.
/// </summary>
public sealed class TenantTokenCostGovernor
{
    private readonly ConcurrentDictionary<string, TenantTokenMetrics> _tenantMetrics = new();

    /// <summary>
    /// Configures or updates the quota limits for a tenant.
    /// </summary>
    public void SetTenantQuota(string tenantId, long dailyTokenLimit, long monthlyTokenLimit, double monthlyCostBudgetUsd)
    {
        var metrics = _tenantMetrics.GetOrAdd(tenantId, id => new TenantTokenMetrics { TenantId = id });
        lock (metrics)
        {
            metrics.DailyTokenLimit = dailyTokenLimit;
            metrics.MonthlyTokenLimit = monthlyTokenLimit;
            metrics.MonthlyCostBudgetUsd = monthlyCostBudgetUsd;
        }
    }

    /// <summary>
    /// Checks if a tenant has remaining budget to process the estimated token request.
    /// </summary>
    public bool CanProcessRequest(string tenantId, int estimatedTokens, out string? rejectionReason)
    {
        var metrics = _tenantMetrics.GetOrAdd(tenantId, id => new TenantTokenMetrics { TenantId = id });
        lock (metrics)
        {
            if (metrics.TotalTokens + estimatedTokens > metrics.MonthlyTokenLimit)
            {
                rejectionReason = $"Tenant '{tenantId}' exceeded monthly token limit ({metrics.MonthlyTokenLimit:N0} tokens).";
                return false;
            }

            if (metrics.TotalCostUsd >= metrics.MonthlyCostBudgetUsd)
            {
                rejectionReason = $"Tenant '{tenantId}' exceeded monthly cost budget (${metrics.MonthlyCostBudgetUsd:N2} USD).";
                return false;
            }

            rejectionReason = null;
            return true;
        }
    }

    /// <summary>
    /// Records actual token usage and updates cost telemetry atomically.
    /// </summary>
    public void RecordUsage(string tenantId, LlmUsage usage)
    {
        if (usage == null) return;

        var metrics = _tenantMetrics.GetOrAdd(tenantId, id => new TenantTokenMetrics { TenantId = id });
        lock (metrics)
        {
            metrics.TotalPromptTokens += usage.PromptTokens;
            metrics.TotalCompletionTokens += usage.CompletionTokens;
            metrics.TotalCostUsd += usage.EstimatedCostUsd;
        }
    }

    /// <summary>
    /// Retrieves current token and cost metrics for a tenant.
    /// </summary>
    public TenantTokenMetrics GetMetrics(string tenantId)
    {
        return _tenantMetrics.GetOrAdd(tenantId, id => new TenantTokenMetrics { TenantId = id });
    }

    /// <summary>
    /// Resets all counters for a tenant (e.g. at monthly billing rollover).
    /// </summary>
    public void ResetTenantUsage(string tenantId)
    {
        if (_tenantMetrics.TryGetValue(tenantId, out var metrics))
        {
            lock (metrics)
            {
                metrics.TotalPromptTokens = 0;
                metrics.TotalCompletionTokens = 0;
                metrics.TotalCostUsd = 0;
                metrics.LastResetUtc = DateTime.UtcNow;
            }
        }
    }
}
