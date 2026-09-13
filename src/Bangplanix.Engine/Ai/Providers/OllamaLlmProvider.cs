using System.Diagnostics;
using System.Text.Json;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai.Providers;

/// <summary>
/// Private On-Premise Ollama / vLLM local LLM Provider integration (Llama 3, Mistral, Qwen, DeepSeek).
/// </summary>
public sealed class OllamaLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;

    public LlmProviderType ProviderType => LlmProviderType.Ollama;
    public LlmProviderConfig Config { get; }

    public OllamaLlmProvider(LlmProviderConfig config, HttpClient? httpClient = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds) };
    }

    public async Task<LlmResponse> GenerateCompletionAsync(LlmPrompt prompt, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            string endpoint = Config.EndpointUrl ?? "http://localhost:11434/api/generate";

            var messages = new List<object>();
            string combinedPrompt = "";
            if (!string.IsNullOrEmpty(prompt.SystemInstruction))
            {
                combinedPrompt += $"[System: {prompt.SystemInstruction}]\n";
            }
            foreach (var m in prompt.Messages)
            {
                combinedPrompt += $"{m.Role}: {m.Content}\n";
            }

            var requestBody = new
            {
                model = Config.ModelName,
                prompt = combinedPrompt,
                stream = false,
                format = prompt.ResponseFormat == "json" ? "json" : null,
                options = new
                {
                    temperature = prompt.Temperature,
                    num_predict = prompt.MaxTokens
                }
            };

            // If offline/mock endpoint
            if (endpoint.Contains("mock") || endpoint.Contains("offline"))
            {
                await Task.Delay(5, cancellationToken);
                return CreateSimulatedResponse(prompt, sw.Elapsed);
            }

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            sw.Stop();

            if (!httpResponse.IsSuccessStatusCode)
            {
                string err = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                return new LlmResponse
                {
                    Success = false,
                    Provider = ProviderType,
                    ModelName = Config.ModelName,
                    ErrorMessage = $"Ollama HTTP {(int)httpResponse.StatusCode}: {err}",
                    Latency = sw.Elapsed
                };
            }

            string responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            string responseText = doc.RootElement.GetProperty("response").GetString() ?? string.Empty;

            int promptTok = prompt.Messages.Sum(m => m.Content.Length) / 4;
            int compTok = responseText.Length / 4;

            // Local Ollama has $0.00 cloud token cost
            return new LlmResponse
            {
                Success = true,
                Content = responseText,
                ModelName = Config.ModelName,
                Provider = ProviderType,
                Usage = new LlmUsage(promptTok, compTok, promptTok + compTok, 0.0),
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
        return Config.IsEnabled;
    }

    private LlmResponse CreateSimulatedResponse(LlmPrompt prompt, TimeSpan latency)
    {
        string content = @"{ ""message"": ""Simulated local Ollama output"", ""status"": ""OK"" }";
        int promptTok = 20;
        int compTok = 10;

        return new LlmResponse
        {
            Success = true,
            Content = content,
            ModelName = Config.ModelName,
            Provider = ProviderType,
            Usage = new LlmUsage(promptTok, compTok, promptTok + compTok, 0.0),
            Latency = latency
        };
    }
}
