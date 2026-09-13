using System.Diagnostics;
using System.Text.Json;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai.Providers;

/// <summary>
/// Microsoft Azure OpenAI / OpenAI LLM Provider integration (GPT-4o / GPT-4o-mini).
/// </summary>
public sealed class AzureOpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;

    public LlmProviderType ProviderType => LlmProviderType.AzureOpenAi;
    public LlmProviderConfig Config { get; }

    public AzureOpenAiProvider(LlmProviderConfig config, HttpClient? httpClient = null)
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

            string endpoint = Config.EndpointUrl ?? "https://api.openai.com/v1/chat/completions";

            var messages = new List<object>();
            if (!string.IsNullOrEmpty(prompt.SystemInstruction))
            {
                messages.Add(new { role = "system", content = prompt.SystemInstruction });
            }
            foreach (var m in prompt.Messages)
            {
                messages.Add(new { role = m.Role.ToString().ToLowerInvariant(), content = m.Content });
            }

            var requestBody = new
            {
                model = Config.ModelName,
                messages = messages,
                temperature = prompt.Temperature,
                max_tokens = prompt.MaxTokens,
                response_format = prompt.ResponseFormat == "json" ? new { type = "json_object" } : null
            };

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };

            if (endpoint.Contains("azure.com"))
            {
                request.Headers.Add("api-key", Config.ApiKey);
            }
            else
            {
                request.Headers.Add("Authorization", $"Bearer {Config.ApiKey}");
            }

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
                    ErrorMessage = $"Azure/OpenAI HTTP {(int)httpResponse.StatusCode}: {err}",
                    Latency = sw.Elapsed
                };
            }

            string responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            string text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            int promptTokens = doc.RootElement.GetProperty("usage").GetProperty("prompt_tokens").GetInt32();
            int compTokens = doc.RootElement.GetProperty("usage").GetProperty("completion_tokens").GetInt32();
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
        string content = @"{ ""message"": ""Simulated Azure OpenAI output"", ""status"": ""OK"" }";
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
