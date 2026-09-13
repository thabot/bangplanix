namespace Bangplanix.Core.Ai;

/// <summary>
/// Supported LLM provider types.
/// </summary>
public enum LlmProviderType
{
    Gemini,
    AzureOpenAi,
    Anthropic,
    Ollama,
    Custom
}

/// <summary>
/// Role of an LLM conversation message.
/// </summary>
public enum LlmRole
{
    System,
    User,
    Assistant
}

/// <summary>
/// Represents a message in an LLM conversation prompt.
/// </summary>
public sealed record LlmMessage(LlmRole Role, string Content);

/// <summary>
/// Input prompt structure for LLM completion requests.
/// </summary>
public sealed class LlmPrompt
{
    public List<LlmMessage> Messages { get; set; } = new();
    public string? SystemInstruction { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public string? ResponseFormat { get; set; } = "json"; // "json" or "text"
    public string? TenantId { get; set; } = "default";
    public string? UserId { get; set; }

    public static LlmPrompt Create(string userPrompt, string? systemInstruction = null, string? responseFormat = "json")
    {
        var prompt = new LlmPrompt
        {
            SystemInstruction = systemInstruction,
            ResponseFormat = responseFormat
        };
        prompt.Messages.Add(new LlmMessage(LlmRole.User, userPrompt));
        return prompt;
    }
}

/// <summary>
/// Usage telemetry for LLM completion.
/// </summary>
public sealed record LlmUsage(int PromptTokens, int CompletionTokens, int TotalTokens, double EstimatedCostUsd);

/// <summary>
/// Output response structure from an LLM provider.
/// </summary>
public sealed class LlmResponse
{
    public bool Success { get; set; } = true;
    public string Content { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public LlmProviderType Provider { get; set; }
    public LlmUsage Usage { get; set; } = new(0, 0, 0, 0.0);
    public string? ErrorMessage { get; set; }
    public TimeSpan Latency { get; set; }
}

/// <summary>
/// Configuration for an LLM provider.
/// </summary>
public sealed class LlmProviderConfig
{
    public LlmProviderType ProviderType { get; set; }
    public string ModelName { get; set; } = "gemini-2.5-flash";
    public string? ApiKey { get; set; }
    public string? EndpointUrl { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int Priority { get; set; } = 1; // 1 = highest priority
    public bool IsEnabled { get; set; } = true;
    public double CostPer1kInputTokens { get; set; } = 0.00015;
    public double CostPer1kOutputTokens { get; set; } = 0.00060;
}
