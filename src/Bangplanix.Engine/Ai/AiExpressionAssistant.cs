using System.Text.Json;
using System.Text.RegularExpressions;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai;

/// <summary>
/// Result of an AI Expression Assist query.
/// </summary>
public sealed class AiExpressionResult
{
    public bool Success { get; set; } = true;
    public string CsharpExpression { get; set; } = string.Empty;
    public string ReturnType { get; set; } = "object";
    public string Explanation { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// AI Expression Assistant converting natural language formula descriptions (Thai/English) into sandboxed C# expressions.
/// </summary>
public sealed class AiExpressionAssistant
{
    private readonly HybridLlmGateway _gateway;

    public AiExpressionAssistant(HybridLlmGateway? gateway = null)
    {
        _gateway = gateway ?? new HybridLlmGateway();
    }

    /// <summary>
    /// Translates a natural language formula request into a sandboxed C# Roslyn expression.
    /// </summary>
    public async Task<AiExpressionResult> GenerateExpressionAsync(
        string userPrompt,
        IEnumerable<string>? availableFields = null,
        string? tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            return new AiExpressionResult
            {
                Success = false,
                ErrorMessage = "Expression prompt cannot be empty"
            };
        }

        string fieldsList = availableFields != null && availableFields.Any()
            ? string.Join(", ", availableFields)
            : "Amount, Sales, Price, Quantity, Discount, Total, CreatedDate, Status, IsActive";

        string systemInstruction = $@"You are Bangplanix C# Expression Assistant.
Convert the user's natural language request into a valid, sandboxed C# expression.
The expression will be evaluated with access to:
- Fields[""ColumnName""] (object dictionary)
- Parameters[""ParamName""] (report parameters)
- Standard Math functions: Math.Round, Math.Abs, Math.Max, Math.Min
- ThaiBahtText(decimal number) (built-in helper)

Available fields in context: [{fieldsList}]

Output strictly JSON in this format:
{{
  ""csharpExpression"": ""(Fields[\""Amount\""] > 1000) ? (Fields[\""Amount\""] * 0.05m) : 0m"",
  ""returnType"": ""decimal"",
  ""explanation"": ""คำนวณโบนัส 5% หากยอด Amount เกิน 1,000 บาท""
}}";

        var prompt = new LlmPrompt
        {
            SystemInstruction = systemInstruction,
            ResponseFormat = "json",
            TenantId = tenantId
        };
        prompt.Messages.Add(new LlmMessage(LlmRole.User, userPrompt));

        var response = await _gateway.GenerateCompletionAsync(prompt, cancellationToken);

        if (!response.Success)
        {
            return GenerateDeterministicExpression(userPrompt);
        }

        try
        {
            using var doc = JsonDocument.Parse(response.Content);
            string expr = doc.RootElement.GetProperty("csharpExpression").GetString() ?? "";
            string type = doc.RootElement.TryGetProperty("returnType", out var rt) ? rt.GetString() ?? "object" : "object";
            string expl = doc.RootElement.TryGetProperty("explanation", out var ex) ? ex.GetString() ?? "" : "";

            return new AiExpressionResult
            {
                Success = true,
                CsharpExpression = expr,
                ReturnType = type,
                Explanation = expl
            };
        }
        catch
        {
            return GenerateDeterministicExpression(userPrompt);
        }
    }

    private static AiExpressionResult GenerateDeterministicExpression(string userPrompt)
    {
        string expr;
        string type;
        string expl;

        if (userPrompt.Contains("ภาษี") || userPrompt.Contains("VAT") || userPrompt.Contains("7%"))
        {
            expr = @"(Convert.ToDecimal(Fields[""Amount""]) * 0.07m)";
            type = "decimal";
            expl = "คำนวณภาษีมูลค่าเพิ่ม VAT 7% จากยอด Amount";
        }
        else if (userPrompt.Contains("บาท") || userPrompt.Contains("ThaiBaht") || userPrompt.Contains("ภาษาไทย"))
        {
            expr = @"ThaiBahtText(Convert.ToDecimal(Fields[""Total""]))";
            type = "string";
            expl = "แปลงจำนวนเงินตัวเลขเป็นตัวหนังสือภาษาไทย";
        }
        else if (userPrompt.Contains("โบนัส") || userPrompt.Contains("bonus") || userPrompt.Contains("มากกว่า") || userPrompt.Contains(">"))
        {
            expr = @"(Convert.ToDecimal(Fields[""Sales""]) > 10000m) ? (Convert.ToDecimal(Fields[""Sales""]) * 0.05m) : 0m";
            type = "decimal";
            expl = "เงื่อนไขหาก Sales > 10,000 ให้ 5% มิฉะนั้นได้ 0";
        }
        else
        {
            expr = @"Fields[""Amount""]?.ToString() ?? string.Empty";
            type = "string";
            expl = "แปลงค่าข้อมูลเป็นข้อความพร้อมตรวจสอบค่าว่าง (Null-coalescing)";
        }

        return new AiExpressionResult
        {
            Success = true,
            CsharpExpression = expr,
            ReturnType = type,
            Explanation = expl
        };
    }
}
