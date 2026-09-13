using System.Text.Json;
using Bangplanix.Core.Ai;

namespace Bangplanix.Engine.Ai;

/// <summary>
/// Executive summary structure ready for embedding into report dossiers.
/// </summary>
public sealed class ExecutiveSummaryReport
{
    public string Headline { get; set; } = string.Empty;
    public List<string> KeyHighlights { get; set; } = new();
    public List<string> AnomalyWarnings { get; set; } = new();
    public List<string> StrategicRecommendations { get; set; } = new();
    public Dictionary<string, object> MetricSnapshots { get; set; } = new();
    public string FormattedMarkdown { get; set; } = string.Empty;
}

/// <summary>
/// Engine synthesizing report metrics and statistical anomalies into professional Executive Summaries.
/// </summary>
public sealed class AiExecutiveSummaryEngine
{
    private readonly HybridLlmGateway _gateway;
    private readonly AnomalyDetectionEngine _anomalyDetector;

    public AiExecutiveSummaryEngine(HybridLlmGateway? gateway = null, AnomalyDetectionEngine? anomalyDetector = null)
    {
        _gateway = gateway ?? new HybridLlmGateway();
        _anomalyDetector = anomalyDetector ?? new AnomalyDetectionEngine();
    }

    /// <summary>
    /// Generates a comprehensive Executive Summary from a dataset and its numeric columns.
    /// </summary>
    public async Task<ExecutiveSummaryReport> GenerateSummaryAsync(
        string reportTitle,
        IDictionary<string, IReadOnlyList<double>> numericSeries,
        string language = "th",
        string? tenantId = "default",
        CancellationToken cancellationToken = default)
    {
        var summary = new ExecutiveSummaryReport();
        var anomalyList = new List<AnomalyAnalysisResult>();

        // 1. Run statistical anomaly detection across all numeric series
        foreach (var (colName, series) in numericSeries)
        {
            if (series.Count > 0)
            {
                var analysis = _anomalyDetector.AnalyzeColumn(colName, series);
                anomalyList.Add(analysis);

                summary.MetricSnapshots[colName] = new
                {
                    Count = analysis.TotalSamples,
                    Mean = analysis.Mean,
                    Median = analysis.Median,
                    Total = Math.Round(series.Sum(), 2),
                    Min = series.Min(),
                    Max = series.Max(),
                    AnomaliesCount = analysis.Anomalies.Count
                };

                foreach (var a in analysis.Anomalies.Where(a => a.Severity >= AnomalySeverity.High))
                {
                    summary.AnomalyWarnings.Add($"[{colName}] แถวที่ {a.RowIndex + 1}: {a.Description}");
                }
            }
        }

        // 2. Build prompt for AI synthesis
        string metricsSummaryJson = JsonSerializer.Serialize(summary.MetricSnapshots);
        string anomaliesJson = JsonSerializer.Serialize(anomalyList.Select(a => new
        {
            a.ColumnName,
            a.Mean,
            a.Median,
            AnomaliesCount = a.Anomalies.Count,
            TopAnomalies = a.Anomalies.Take(3).Select(x => x.Description)
        }));

        string systemInstruction = $@"You are Bangplanix Enterprise AI Executive Summary Writer.
Synthesize the report metrics and anomalies into a high-impact executive summary.
Language: {(language == "th" ? "Thai (ภาษาไทย)" : "English")}.

Output JSON matching this format:
{{
  ""headline"": ""สรุปภาพรวมผลการดำเนินงานและจุดสังเกตสำคัญ"",
  ""keyHighlights"": [
    ""ยอดรวมทั้งสิ้นเติบโตอย่างมีนัยสำคัญที่ ..."",
    ""ค่าเฉลี่ยต่อรายการอยู่ที่ ...""
  ],
  ""strategicRecommendations"": [
    ""ควรตรวจสอบรายการที่ผิดปกติ ..."",
    ""ปรับปรุงการควบคุมงบประมาณ ...""
  ]
}}";

        var prompt = new LlmPrompt
        {
            SystemInstruction = systemInstruction,
            ResponseFormat = "json",
            TenantId = tenantId
        };
        prompt.Messages.Add(new LlmMessage(LlmRole.User, $"Report: {reportTitle}\nMetrics: {metricsSummaryJson}\nAnomalies: {anomaliesJson}"));

        var response = await _gateway.GenerateCompletionAsync(prompt, cancellationToken);

        if (response.Success)
        {
            try
            {
                using var doc = JsonDocument.Parse(response.Content);
                summary.Headline = doc.RootElement.GetProperty("headline").GetString() ?? $"สรุปภาพรวม: {reportTitle}";

                if (doc.RootElement.TryGetProperty("keyHighlights", out var hl))
                {
                    foreach (var item in hl.EnumerateArray())
                    {
                        summary.KeyHighlights.Add(item.GetString() ?? "");
                    }
                }

                if (doc.RootElement.TryGetProperty("strategicRecommendations", out var sr))
                {
                    foreach (var item in sr.EnumerateArray())
                    {
                        summary.StrategicRecommendations.Add(item.GetString() ?? "");
                    }
                }
            }
            catch
            {
                PopulateDefaultHighlights(summary, reportTitle);
            }
        }
        else
        {
            PopulateDefaultHighlights(summary, reportTitle);
        }

        // 3. Format complete Markdown
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# 📊 {summary.Headline}");
        sb.AppendLine();
        sb.AppendLine("### 📌 สาระสำคัญ (Key Highlights)");
        foreach (var h in summary.KeyHighlights)
        {
            sb.AppendLine($"- {h}");
        }

        if (summary.AnomalyWarnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### ⚠️ การตรวจจับจุดผิดปกติทางสถิติ (Statistical Anomalies)");
            foreach (var w in summary.AnomalyWarnings)
            {
                sb.AppendLine($"- 🔴 {w}");
            }
        }

        if (summary.StrategicRecommendations.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("### 💡 ข้อเสนอแนะเชิงกลยุทธ์ (Recommendations)");
            foreach (var r in summary.StrategicRecommendations)
            {
                sb.AppendLine($"- 🟢 {r}");
            }
        }

        summary.FormattedMarkdown = sb.ToString();
        return summary;
    }

    private static void PopulateDefaultHighlights(ExecutiveSummaryReport summary, string reportTitle)
    {
        summary.Headline = $"สรุปผลการประมวลผลรายงาน: {reportTitle}";
        summary.KeyHighlights.Add("ประมวลผลชุดข้อมูลรายงานครบถ้วนและสถิติการทำงานอยู่ในเกณฑ์ปกติ");
        summary.KeyHighlights.Add($"มีตัวชี้วัดสำคัญทั้งหมด {summary.MetricSnapshots.Count} คอลัมน์");
        summary.StrategicRecommendations.Add("ติดตามความสม่ำเสมอของข้อมูลและตรวจทานรายการที่มีความผันผวนสูง");
    }
}
