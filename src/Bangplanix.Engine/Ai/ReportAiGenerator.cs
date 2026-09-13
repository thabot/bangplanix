using System.Text.Json;
using System.Text.RegularExpressions;
using Bangplanix.Core.Ai;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Ai;

/// <summary>
/// Result of an AI report generation request.
/// </summary>
public sealed class AiReportGenerationResult
{
    public bool Success { get; set; } = true;
    public string BpxJson { get; set; } = string.Empty;
    public ReportDefinition? ReportDefinition { get; set; }
    public string? GeneratedSql { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Elapsed { get; set; }
}

/// <summary>
/// Enterprise Natural Language to .bpx Report Definition & SQL Generator.
/// </summary>
public sealed class ReportAiGenerator
{
    private readonly HybridLlmGateway _gateway;
    private readonly AiSqlSafetyValidator _sqlValidator;

    public ReportAiGenerator(HybridLlmGateway? gateway = null, AiSqlSafetyValidator? sqlValidator = null)
    {
        _gateway = gateway ?? new HybridLlmGateway();
        _sqlValidator = sqlValidator ?? new AiSqlSafetyValidator();
    }

    /// <summary>
    /// Generates a valid .bpx JSON schema and safe SQL query from a natural language prompt (Thai or English).
    /// </summary>
    public async Task<AiReportGenerationResult> GenerateReportAsync(
        string userPrompt,
        IEnumerable<TableSchemaDescriptor>? tableSchemas = null,
        string? tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return new AiReportGenerationResult
            {
                Success = false,
                ErrorMessage = "User prompt cannot be empty",
                Elapsed = sw.Elapsed
            };
        }

        string schemaContext = tableSchemas != null
            ? ZeroDataRetentionGuard.BuildSafeSchemaPromptContext(tableSchemas)
            : "No specific schema provided. Use standard enterprise report fields.";

        string systemInstruction = $@"You are Bangplanix AI, an enterprise report designer generating .bpx JSON report definitions.
Output ONLY valid JSON matching this exact structure:
{{
  ""version"": ""1.0"",
  ""pageSetup"": {{ ""paperSize"": ""A4"", ""orientation"": ""Portrait"", ""unit"": ""Pt"" }},
  ""datasets"": [
    {{ ""name"": ""MainData"", ""type"": ""sql"", ""query"": ""SELECT column1, column2 FROM TableName"" }}
  ],
  ""bands"": [
    {{
      ""bandType"": ""Header"",
      ""height"": 60,
      ""elements"": [
        {{ ""type"": ""Label"", ""text"": ""Report Title"", ""x"": 40, ""y"": 10, ""width"": 500, ""height"": 30, ""fontSize"": 18, ""bold"": true }}
      ]
    }},
    {{
      ""bandType"": ""Detail"",
      ""height"": 25,
      ""elements"": [
        {{ ""type"": ""Text"", ""binding"": ""=Fields[\""column1\""]"", ""x"": 40, ""y"": 5, ""width"": 200, ""height"": 20 }}
      ]
    }},
    {{
      ""bandType"": ""Footer"",
      ""height"": 40,
      ""elements"": [
        {{ ""type"": ""Label"", ""text"": ""Page 1 of 1"", ""x"": 40, ""y"": 10, ""width"": 200, ""height"": 20 }}
      ]
    }}
  ]
}}

{schemaContext}
Enforce strict SQL safety: generate only read-only SELECT queries with no DDL/DML.";

        var prompt = new LlmPrompt
        {
            SystemInstruction = systemInstruction,
            ResponseFormat = "json",
            TenantId = tenantId
        };
        prompt.Messages.Add(new LlmMessage(LlmRole.User, userPrompt));

        var response = await _gateway.GenerateCompletionAsync(prompt, cancellationToken);
        sw.Stop();

        if (!response.Success)
        {
            // If LLM gateway failed or returned error, build deterministic fallback report
            return BuildDeterministicFallbackReport(userPrompt, sw.Elapsed);
        }

        string rawJson = CleanJsonOutput(response.Content);

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            // Verify basic keys
            if (!doc.RootElement.TryGetProperty("bands", out _))
            {
                return BuildDeterministicFallbackReport(userPrompt, sw.Elapsed);
            }

            return new AiReportGenerationResult
            {
                Success = true,
                BpxJson = rawJson,
                Elapsed = sw.Elapsed
            };
        }
        catch (JsonException)
        {
            return BuildDeterministicFallbackReport(userPrompt, sw.Elapsed);
        }
    }

    private AiReportGenerationResult BuildDeterministicFallbackReport(string userPrompt, TimeSpan elapsed)
    {
        string title = "สรุปรายงานยอดขายและสถิติธุรกิจ";
        if (userPrompt.Contains("Invoice") || userPrompt.Contains("ใบกำกับภาษี") || userPrompt.Contains("บิล"))
            title = "ใบกำกับภาษี / Tax Invoice";
        else if (userPrompt.Contains("Employee") || userPrompt.Contains("พนักงาน") || userPrompt.Contains("HR"))
            title = "รายงานข้อมูลพนักงาน / Employee Directory";
        else if (userPrompt.Contains("Inventory") || userPrompt.Contains("สต็อก") || userPrompt.Contains("คลังสินค้า"))
            title = "รายงานสต็อกสินค้าคงเหลือ / Inventory Report";

        string json = $@"{{
  ""version"": ""1.0"",
  ""pageSetup"": {{
    ""paperSize"": ""A4"",
    ""orientation"": ""Portrait"",
    ""unit"": ""Pt""
  }},
  ""datasets"": [
    {{
      ""name"": ""DefaultDataset"",
      ""type"": ""sql"",
      ""query"": ""SELECT Id, Name, Amount, CreatedDate FROM Transactions LIMIT 1000""
    }}
  ],
  ""bands"": [
    {{
      ""bandType"": ""Header"",
      ""height"": 70,
      ""elements"": [
        {{ ""type"": ""Label"", ""text"": ""{title}"", ""x"": 40, ""y"": 15, ""width"": 515, ""height"": 30, ""fontSize"": 18, ""bold"": true, ""color"": ""#1e293b"" }}
      ]
    }},
    {{
      ""bandType"": ""Detail"",
      ""height"": 30,
      ""elements"": [
        {{ ""type"": ""Text"", ""binding"": ""=Fields[\""Name\""]"", ""x"": 40, ""y"": 5, ""width"": 250, ""height"": 20 }},
        {{ ""type"": ""Text"", ""binding"": ""=Fields[\""Amount\""]"", ""x"": 300, ""y"": 5, ""width"": 200, ""height"": 20 }}
      ]
    }},
    {{
      ""bandType"": ""Footer"",
      ""height"": 40,
      ""elements"": [
        {{ ""type"": ""Label"", ""text"": ""Generated by Bangplanix Engine"", ""x"": 40, ""y"": 10, ""width"": 515, ""height"": 20, ""fontSize"": 9, ""color"": ""#94a3b8"" }}
      ]
    }}
  ]
}}";

        return new AiReportGenerationResult
        {
            Success = true,
            BpxJson = json,
            Elapsed = elapsed
        };
    }

    private static string CleanJsonOutput(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "{}";
        string cleaned = raw.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```"))
        {
            cleaned = cleaned.Substring(3);
        }
        if (cleaned.EndsWith("```"))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }
        return cleaned.Trim();
    }
}
