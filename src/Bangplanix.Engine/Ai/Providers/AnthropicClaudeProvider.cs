using System.Diagnostics;
using System.Text.Json;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai.Providers;

/// <summary>
/// Anthropic Claude LLM Provider integration (Claude 3.5 Sonnet / Haiku).
/// </summary>
public sealed class AnthropicClaudeProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;

    public LlmProviderType ProviderType => LlmProviderType.Anthropic;
    public LlmProviderConfig Config { get; }

    public AnthropicClaudeProvider(LlmProviderConfig config, HttpClient? httpClient = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds) };
    }

    public async Task<LlmResponse> GenerateCompletionAsync(LlmPrompt prompt, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrWhiteSpace(Config.ApiKey) || Config.ApiKey == "mock_key")
            {
                await Task.Delay(10, cancellationToken);
                return CreateSimulatedResponse(prompt, sw.Elapsed);
            }

            string endpoint = Config.EndpointUrl ?? "https://api.anthropic.com/v1/messages";

            var messages = prompt.Messages.Select(m => new
            {
                role = m.Role == LlmRole.Assistant ? "assistant" : "user",
                content = m.Content
            }).ToArray();

            var requestBody = new
            {
                model = Config.ModelName,
                messages = messages,
                max_tokens = prompt.MaxTokens,
                temperature = prompt.Temperature,
                system = prompt.SystemInstruction
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", Config.ApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");

            var httpResponse = await _httpClient.SendAsync(request, cancellationToken);
            sw.Stop();

            if (!httpResponse.IsSuccessStatusCode)
            {
                string err = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                return new LlmResponse
                {
                    Success = false,
                    Provider = ProviderType,
                    ModelName = Config.ModelName,
                    ErrorMessage = $"Anthropic HTTP {(int)httpResponse.StatusCode}: {err}",
                    Latency = sw.Elapsed
                };
            }

            string responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            string text = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            int promptTokens = doc.RootElement.GetProperty("usage").GetProperty("input_tokens").GetInt32();
            int compTokens = doc.RootElement.GetProperty("usage").GetProperty("output_tokens").GetInt32();
            double cost = ((promptTokens / 1000.0) * Config.CostPer1kInputTokens) +
                          ((compTokens / 1000.0) * Config.CostPer1kOutputTokens);

            return new LlmResponse
            {
                Success = true,
                Content = text,
                ModelName = Config.ModelName,
                Provider = ProviderType,
                Usage = new LlmUsage(promptTokens, compTokens, promptTokens + compTokens, cost),
                Latency = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new LlmResponse
            {
                Success = false,
                Provider = ProviderType,
                ModelName = Config.ModelName,
                ErrorMessage = ex.Message,
                Latency = sw.Elapsed
            };
        }
    }

    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return Config.IsEnabled && (!string.IsNullOrEmpty(Config.ApiKey) || Config.ApiKey == "mock_key");
    }

    private LlmResponse CreateSimulatedResponse(LlmPrompt prompt, TimeSpan latency)
    {
        string lastMsg = prompt.Messages.LastOrDefault()?.Content ?? string.Empty;
        string content = @"{ ""message"": ""Simulated Anthropic Claude output"", ""status"": ""OK"" }";
        int promptTok = Math.Max(10, lastMsg.Length / 4);
        int compTok = Math.Max(10, content.Length / 4);
        double cost = ((promptTok / 1000.0) * Config.CostPer1kInputTokens) + ((compTok / 1000.0) * Config.CostPer1kOutputTokens);

        return new LlmResponse
        {
            Success = true,
            Content = content,
            ModelName = Config.ModelName,
            Provider = ProviderType,
            Usage = new LlmUsage(promptTok, compTok, promptTok + compTok, cost),
            Latency = latency
        };
    }
}
