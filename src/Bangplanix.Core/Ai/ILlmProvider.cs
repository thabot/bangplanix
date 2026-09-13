namespace Bangplanix.Core.Ai;

/// <summary>
/// Interface for LLM provider adapters.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// The provider type identity.
    /// </summary>
    LlmProviderType ProviderType { get; }

    /// <summary>
    /// Configuration for this provider.
    /// </summary>
    LlmProviderConfig Config { get; }

    /// <summary>
    /// Executes a completion call against the underlying LLM model.
    /// </summary>
    Task<LlmResponse> GenerateCompletionAsync(LlmPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks connectivity/health of the LLM endpoint.
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}
