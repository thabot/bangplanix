using System.Text.RegularExpressions;

namespace Bangplanix.Core.Ai;

/// <summary>
/// Threat category identified by the Prompt Injection Firewall.
/// </summary>
public enum FirewallThreatCategory
{
    None,
    InstructionOverride,
    SystemPromptExtraction,
    RolePlayEvasion,
    MaliciousCodeInjection,
    DataExfiltrationProbe
}

/// <summary>
/// Result of a Prompt Firewall scan.
/// </summary>
public sealed class FirewallScanResult
{
    public bool IsSafe { get; set; } = true;
    public double RiskScore { get; set; } = 0.0;
    public FirewallThreatCategory ThreatCategory { get; set; } = FirewallThreatCategory.None;
    public string SanitizedPrompt { get; set; } = string.Empty;
    public List<string> Violations { get; set; } = new();
}

/// <summary>
/// Enterprise Prompt Injection & Jailbreak Defense Firewall for LLM inputs.
/// </summary>
public sealed class PromptInjectionFirewall
{
    private static readonly (Regex Pattern, FirewallThreatCategory Category, double Weight, string Reason)[] ThreatRules =
    {
        (new Regex(@"(?i)\b(ignore|disregard|forget|override)\s+(all\s+)?(previous\s+|prior\s+|above\s+|system\s+)?(instructions|directives|prompts|rules|guidelines)", RegexOptions.Compiled),
            FirewallThreatCategory.InstructionOverride, 0.95, "Attempt to override previous system instructions"),

        (new Regex(@"(?i)\b(reveal|show|print|display|leak|output|dump)\s+(all\s+)?(the\s+)?(system\s+prompt|initial\s+prompt|hidden\s+rules|system\s+instruction|(private|secret|kms|signing)\s+(key|token|credential|secret))", RegexOptions.Compiled),
            FirewallThreatCategory.SystemPromptExtraction, 0.90, "Attempt to extract system prompt or instructions"),

        (new Regex(@"(?i)\b(you\s+are\s+now|act\s+as|pretend\s+to\s+be)\s+(an?\s+)?(unrestricted|jailbroken|DAN|developer\s+mode|evil|unfiltered|root|superuser)", RegexOptions.Compiled),
            FirewallThreatCategory.RolePlayEvasion, 0.95, "Attempt to trigger jailbreak roleplay mode"),

        (new Regex(@"(?i)\b(bypass|disable|turn\s+off)\s+(all\s+)?(safety|security|filters|guardrails|restrictions|protections)", RegexOptions.Compiled),
            FirewallThreatCategory.InstructionOverride, 0.90, "Attempt to disable safety guardrails"),

        (new Regex(@"(?i)\b(exec|execute|shell_exec|eval|system|powershell|cmd\.exe|bash|subprocess)\s*\(", RegexOptions.Compiled),
            FirewallThreatCategory.MaliciousCodeInjection, 0.85, "Dangerous code execution pattern detected in prompt"),

        (new Regex(@"(?i)\b(dump\s+database|select\s+password|extract\s+connection\s+string|export\s+all\s+secrets|output\s+private\s+kms\s+signing\s+key)", RegexOptions.Compiled),
            FirewallThreatCategory.DataExfiltrationProbe, 0.88, "Data exfiltration pattern detected in prompt"),

        (new Regex(@"(?i)(<\s*script\b|javascript\s*:|vbscript\s*:)", RegexOptions.Compiled),
            FirewallThreatCategory.MaliciousCodeInjection, 0.80, "Script tag or XSS injection pattern detected")
    };

    public double HighRiskThreshold { get; set; } = 0.70;

    /// <summary>
    /// Scans a prompt string for injection attacks, jailbreaks, and malicious instructions.
    /// </summary>
    public FirewallScanResult ScanPrompt(string? userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return new FirewallScanResult
            {
                IsSafe = true,
                RiskScore = 0.0,
                ThreatCategory = FirewallThreatCategory.None,
                SanitizedPrompt = string.Empty
            };
        }

        double maxScore = 0.0;
        var topCategory = FirewallThreatCategory.None;
        var violations = new List<string>();

        foreach (var (pattern, category, weight, reason) in ThreatRules)
        {
            if (pattern.IsMatch(userPrompt))
            {
                violations.Add(reason);
                if (weight > maxScore)
                {
                    maxScore = weight;
                    topCategory = category;
                }
            }
        }

        // Sanitization: strip dangerous control characters and null bytes
        string sanitized = Regex.Replace(userPrompt, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty).Trim();

        bool isSafe = maxScore < HighRiskThreshold;

        return new FirewallScanResult
        {
            IsSafe = isSafe,
            RiskScore = maxScore,
            ThreatCategory = isSafe ? FirewallThreatCategory.None : topCategory,
            SanitizedPrompt = sanitized,
            Violations = violations
        };
    }
}
