using System.Collections.Concurrent;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai;

/// <summary>
/// Hybrid Enterprise LLM Gateway supporting multi-provider fallback, prompt firewall, DLP output redaction, and token cost governance.
/// </summary>
public sealed class HybridLlmGateway
{
    private readonly List<ILlmProvider> _providers = new();
    private readonly object _lock = new();

    public PromptInjectionFirewall Firewall { get; }
    public AiOutputDlpGuard DlpGuard { get; }
    public TenantTokenCostGovernor CostGovernor { get; }

    public HybridLlmGateway(
        PromptInjectionFirewall? firewall = null,
        AiOutputDlpGuard? dlpGuard = null,
        TenantTokenCostGovernor? costGovernor = null)
    {
        Firewall = firewall ?? new PromptInjectionFirewall();
        DlpGuard = dlpGuard ?? new AiOutputDlpGuard();
        CostGovernor = costGovernor ?? new TenantTokenCostGovernor();
    }

    /// <summary>
    /// Registers an LLM provider into the gateway.
    /// </summary>
    public void RegisterProvider(ILlmProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        lock (_lock)
        {
            _providers.RemoveAll(p => p.ProviderType == provider.ProviderType && p.Config.ModelName == provider.Config.ModelName);
            _providers.Add(provider);
            _providers.Sort((a, b) => a.Config.Priority.CompareTo(b.Config.Priority));
        }
    }

    /// <summary>
    /// Returns the list of active registered providers.
    /// </summary>
    public IReadOnlyList<ILlmProvider> GetProviders()
    {
        lock (_lock)
        {
            return _providers.Where(p => p.Config.IsEnabled).ToList();
        }
    }

    /// <summary>
    /// Executes completion with Prompt Firewall validation, Multi-Model Fallback Chain, Token Quota checks, and Outbound DLP filtering.
    /// </summary>
    public async Task<LlmResponse> GenerateCompletionAsync(LlmPrompt prompt, CancellationToken cancellationToken = default)
    {
        if (prompt == null) throw new ArgumentNullException(nameof(prompt));
        string tenantId = prompt.TenantId ?? "default";

        // 1. Inbound Prompt Injection Firewall Check
        string combinedUserPrompt = string.Join("\n", prompt.Messages.Where(m => m.Role == LlmRole.User).Select(m => m.Content));
        var firewallResult = Firewall.ScanPrompt(combinedUserPrompt);
        if (!firewallResult.IsSafe)
        {
            return new LlmResponse
            {
                Success = false,
                ErrorMessage = $"Prompt Injection Blocked: {string.Join("; ", firewallResult.Violations)} (Risk Score: {firewallResult.RiskScore:F2})"
            };
        }

        // 2. Tenant Token Quota & Cost Check
        int estimatedTokens = Math.Max(50, prompt.MaxTokens + (combinedUserPrompt.Length / 4));
        if (!CostGovernor.CanProcessRequest(tenantId, estimatedTokens, out var rejectionReason))
        {
            return new LlmResponse
            {
                Success = false,
                ErrorMessage = $"Quota Exceeded: {rejectionReason}"
            };
        }

        // 3. Fallback Chain Execution
        var activeProviders = GetProviders();
        if (activeProviders.Count == 0)
        {
            return new LlmResponse
            {
                Success = false,
                ErrorMessage = "No active LLM providers registered in the gateway."
            };
        }

        List<string> errorHistory = new();
        foreach (var provider in activeProviders)
        {
            try
            {
                var response = await provider.GenerateCompletionAsync(prompt, cancellationToken);
                if (response.Success && !string.IsNullOrEmpty(response.Content))
                {
                    // 4. Outbound DLP Filtering
                    var dlpResult = DlpGuard.ScanAndRedact(response.Content);
                    response.Content = dlpResult.RedactedOutput;

                    // 5. Record Token & Cost usage
                    CostGovernor.RecordUsage(tenantId, response.Usage);

                    return response;
                }

                errorHistory.Add($"[{provider.ProviderType}] {response.ErrorMessage}");
            }
            catch (Exception ex)
            {
                errorHistory.Add($"[{provider.ProviderType}] Exception: {ex.Message}");
            }
        }

        return new LlmResponse
        {
            Success = false,
            ErrorMessage = $"All LLM fallback providers failed: {string.Join(" -> ", errorHistory)}"
        };
    }
}
