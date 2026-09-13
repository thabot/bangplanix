using System.Diagnostics;
using System.Text.Json;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai.Providers;

/// <summary>
/// Google Gemini LLM Provider integration (Gemini 2.5 Flash / Pro).
/// </summary>
public sealed class GeminiLlmProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;

    public LlmProviderType ProviderType => LlmProviderType.Gemini;
    public LlmProviderConfig Config { get; }

    public GeminiLlmProvider(LlmProviderConfig config, HttpClient? httpClient = null)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds) };
    }

    public async Task<LlmResponse> GenerateCompletionAsync(LlmPrompt prompt, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // If offline or simulated/mock in unit tests when no API key provided
            if (string.IsNullOrWhiteSpace(Config.ApiKey) || Config.ApiKey == "mock_key")
            {
                await Task.Delay(10, cancellationToken);
                return CreateSimulatedResponse(prompt, sw.Elapsed);
            }

            string endpoint = Config.EndpointUrl ??
                $"https://generativelanguage.googleapis.com/v1beta/models/{Config.ModelName}:generateContent?key={Config.ApiKey}";

            var requestBody = new
            {
                contents = prompt.Messages.Select(m => new
                {
                    role = m.Role == LlmRole.Assistant ? "model" : "user",
                    parts = new[] { new { text = m.Content } }
                }).ToArray(),
                generationConfig = new
                {
                    temperature = prompt.Temperature,
                    maxOutputTokens = prompt.MaxTokens,
                    responseMimeType = prompt.ResponseFormat == "json" ? "application/json" : "text/plain"
                },
                systemInstruction = string.IsNullOrEmpty(prompt.SystemInstruction) ? null : new
                {
                    parts = new[] { new { text = prompt.SystemInstruction } }
                }
            };

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
                    ErrorMessage = $"Gemini HTTP {(int)httpResponse.StatusCode}: {err}",
                    Latency = sw.Elapsed
                };
            }

            string responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);
            string text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            int promptTokens = prompt.Messages.Sum(m => m.Content.Length) / 4;
            int completionTokens = text.Length / 4;
            double cost = ((promptTokens / 1000.0) * Config.CostPer1kInputTokens) +
                          ((completionTokens / 1000.0) * Config.CostPer1kOutputTokens);

            return new LlmResponse
            {
                Success = true,
                Content = text,
                ModelName = Config.ModelName,
                Provider = ProviderType,
                Usage = new LlmUsage(promptTokens, completionTokens, promptTokens + completionTokens, cost),
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
        string simulatedContent;

        if (lastMsg.Contains("Report") || lastMsg.Contains("รายงาน") || prompt.SystemInstruction?.Contains(".bpx") == true)
        {
            simulatedContent = @"{
  ""version"": ""1.0"",
  ""pageSetup"": { ""paperSize"": ""A4"", ""orientation"": ""Portrait"", ""unit"": ""Pt"" },
  ""bands"": [
    { ""bandType"": ""Header"", ""height"": 60, ""elements"": [ { ""type"": ""Label"", ""text"": ""Sales Summary Report"", ""fontSize"": 18 } ] },
    { ""bandType"": ""Detail"", ""height"": 30, ""elements"": [ { ""type"": ""Text"", ""binding"": ""=Fields[\""Amount\""]"" } ] },
    { ""bandType"": ""Footer"", ""height"": 40, ""elements"": [ { ""type"": ""Label"", ""text"": ""Generated by Bangplanix AI"", ""fontSize"": 9 } ] }
  ]
}";
        }
        else if (lastMsg.Contains("Expression") || lastMsg.Contains("สูตร") || prompt.SystemInstruction?.Contains("Roslyn") == true)
        {
            simulatedContent = @"{ ""csharpExpression"": ""(Fields[\""Amount\""] > 10000) ? (Fields[\""Amount\""] * 0.05) : 0"", ""returnType"": ""decimal"" }";
        }
        else
        {
            simulatedContent = @"{ ""message"": ""Simulated response from Gemini"", ""status"": ""OK"" }";
        }

        int promptTok = Math.Max(10, lastMsg.Length / 4);
        int compTok = Math.Max(10, simulatedContent.Length / 4);
        double cost = ((promptTok / 1000.0) * Config.CostPer1kInputTokens) + ((compTok / 1000.0) * Config.CostPer1kOutputTokens);

        return new LlmResponse
        {
            Success = true,
            Content = simulatedContent,
            ModelName = Config.ModelName,
            Provider = ProviderType,
            Usage = new LlmUsage(promptTok, compTok, promptTok + compTok, cost),
            Latency = latency
        };
    }
}
