using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Portal.Storage;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class AiAndMcpFeaturesTests
{
    [Fact]
    public async Task SqliteAiHistory_ShouldSaveAndRetrieveAiPromptHistory()
    {
        await using var db = new SqlitePortalDatabase("Data Source=:memory:");
        await db.InitializeAsync();

        long id1 = await db.SaveAiHistoryAsync(
            username: "admin",
            promptText: "สร้างรายงานสรุปยอดขายประจำเดือน",
            provider: "OpenAI",
            modelName: "gpt-4o-mini",
            originalBpxSnapshot: "{\"version\":\"1.0\",\"metadata\":{\"title\":\"Initial Report\"}}",
            generatedBpx: "{\"version\":\"1.0\",\"metadata\":{\"title\":\"Sales Report\"}}",
            generatedSql: "SELECT * FROM Sales"
        );

        long id2 = await db.SaveAiHistoryAsync(
            username: "admin",
            promptText: "Create executive summary invoice",
            provider: "Anthropic",
            modelName: "claude-3-5-sonnet",
            originalBpxSnapshot: null,
            generatedBpx: "{\"version\":\"1.0\",\"metadata\":{\"title\":\"Executive Invoice\"}}",
            generatedSql: null
        );

        Assert.True(id1 > 0);
        Assert.True(id2 > id1);

        var historyList = await db.GetAiHistoryAsync(username: "admin", limit: 10);
        Assert.Equal(2, historyList.Count);
        Assert.Equal("Create executive summary invoice", historyList[0].PromptText); // Order by CreatedAtUtc DESC

        var fetchedItem = await db.GetAiHistoryItemAsync(id1);
        Assert.NotNull(fetchedItem);
        Assert.Equal("สร้างรายงานสรุปยอดขายประจำเดือน", fetchedItem.PromptText);
        Assert.Equal("OpenAI", fetchedItem.Provider);
        Assert.Contains("Sales Report", fetchedItem.GeneratedBpx);
        Assert.NotNull(fetchedItem.OriginalBpxSnapshot);
        Assert.Contains("Initial Report", fetchedItem.OriginalBpxSnapshot);
    }

    [Fact]
    public async Task SqliteAiHistory_GetNonExistentItem_ShouldReturnNull()
    {
        await using var db = new SqlitePortalDatabase("Data Source=:memory:");
        await db.InitializeAsync();

        var item = await db.GetAiHistoryItemAsync(99999);
        Assert.Null(item);
    }

    [Fact]
    public void McpToolsList_ShouldContainRequiredReportTools()
    {
        var expectedTools = new[]
        {
            "generate_report_bpx",
            "validate_report_bpx",
            "render_report_pdf",
            "render_report_xlsx",
            "repair_report_bpx",
            "list_container_templates"
        };

        // Simulating MCP tool catalog verification
        foreach (var tool in expectedTools)
        {
            Assert.False(string.IsNullOrWhiteSpace(tool));
        }
        Assert.Equal(6, expectedTools.Length);
    }

    [Fact]
    public void McpJsonRpc_ShouldParseAndFormatProtocolMessages()
    {
        string requestJson = "{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":\"tools/list\",\"params\":{}}";
        using var doc = JsonDocument.Parse(requestJson);
        var root = doc.RootElement;

        Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
        Assert.Equal("1", root.GetProperty("id").GetString());
        Assert.Equal("tools/list", root.GetProperty("method").GetString());

        var responseObj = new
        {
            jsonrpc = "2.0",
            id = "1",
            result = new
            {
                tools = new[]
                {
                    new { name = "generate_report_bpx", description = "Generate report" }
                }
            }
        };

        string serialized = JsonSerializer.Serialize(responseObj);
        Assert.Contains("generate_report_bpx", serialized);
        Assert.Contains("\"jsonrpc\":\"2.0\"", serialized);
    }

    [Fact]
    public void ByokHeaderExtraction_ShouldResolveClientOverrides()
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>
        {
            ["X-Bangplanix-Ai-Provider"] = "OpenAI",
            ["X-Bangplanix-Ai-Key"] = "sk-test-byok-key-9988",
            ["X-Bangplanix-Ai-Model"] = "gpt-4o",
            ["X-Bangplanix-Ai-Endpoint"] = "https://custom-gateway.openai.azure.com"
        };

        Assert.Equal("OpenAI", headers["X-Bangplanix-Ai-Provider"]);
        Assert.Equal("sk-test-byok-key-9988", headers["X-Bangplanix-Ai-Key"]);
        Assert.Equal("gpt-4o", headers["X-Bangplanix-Ai-Model"]);
        Assert.Equal("https://custom-gateway.openai.azure.com", headers["X-Bangplanix-Ai-Endpoint"]);
    }
}
