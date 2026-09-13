using System.Text.RegularExpressions;

namespace Bangplanix.Core.Ai;

/// <summary>
/// Result of a Data Loss Prevention (DLP) scan on LLM outputs.
/// </summary>
public sealed class DlpScanResult
{
    public bool IsClean { get; set; } = true;
    public string RedactedOutput { get; set; } = string.Empty;
    public List<string> Violations { get; set; } = new();
}

/// <summary>
/// Enterprise Data Loss Prevention (DLP) Guard for LLM outputs.
/// </summary>
public sealed class AiOutputDlpGuard
{
    private static readonly (Regex Pattern, string Replacement, string RuleName)[] DlpRules =
    {
        // OpenAI / API Key format
        (new Regex(@"\bsk-[a-zA-Z0-9]{20,}\b", RegexOptions.Compiled), "[REDACTED_API_KEY]", "OpenAI API Key Leak"),

        // Google API Key format
        (new Regex(@"\bAIzaSy[a-zA-Z0-9_-]{33}\b", RegexOptions.Compiled), "[REDACTED_API_KEY]", "Google Cloud API Key Leak"),

        // AWS Access Key
        (new Regex(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled), "[REDACTED_AWS_KEY]", "AWS Access Key Leak"),

        // Database Connection String Password
        (new Regex(@"(?i)(password|pwd|secret)\s*=\s*['""]?([^;'""\s]+)['""]?", RegexOptions.Compiled), "$1=********", "Database Password in Connection String"),

        // Credit Card Numbers (13 to 16 digits formatted)
        (new Regex(@"\b(?:\d{4}[ -]?){3}\d{4}\b", RegexOptions.Compiled), "[REDACTED_CREDIT_CARD]", "Credit Card Number"),

        // Thai National ID (13 digits)
        (new Regex(@"\b\d{1}[ -]?\d{4}[ -]?\d{5}[ -]?\d{2}[ -]?\d{1}\b", RegexOptions.Compiled), "[REDACTED_THAI_ID]", "Thai National ID Number"),

        // Private Key headers
        (new Regex(@"-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----[^-]+-----END (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----", RegexOptions.Compiled | RegexOptions.Singleline), "[REDACTED_PRIVATE_KEY]", "Private Key Leak"),

        // JWT Tokens
        (new Regex(@"\beyJ[a-zA-Z0-9_-]+\.eyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\b", RegexOptions.Compiled), "[REDACTED_JWT_TOKEN]", "JWT Bearer Token")
    };

    /// <summary>
    /// Scans and redacts sensitive credentials and PII from LLM generated outputs.
    /// </summary>
    public DlpScanResult ScanAndRedact(string? outputText)
    {
        if (string.IsNullOrEmpty(outputText))
        {
            return new DlpScanResult { IsClean = true, RedactedOutput = string.Empty };
        }

        string result = outputText;
        var violations = new List<string>();

        foreach (var (pattern, replacement, ruleName) in DlpRules)
        {
            if (pattern.IsMatch(result))
            {
                violations.Add(ruleName);
                result = pattern.Replace(result, replacement);
            }
        }

        return new DlpScanResult
        {
            IsClean = violations.Count == 0,
            RedactedOutput = result,
            Violations = violations
        };
    }
}
