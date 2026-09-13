using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public enum PdfTagType
{
    Document,
    Section,
    H1,
    H2,
    H3,
    H4,
    H5,
    H6,
    Paragraph,
    Table,
    TableRow,
    TableHeader,
    TableData,
    Figure,
    Formula,
    List,
    ListItem,
    Artifact
}

public enum TableScopeType
{
    None,
    Column,
    Row,
    Both
}

public enum AccessibilityViolationSeverity
{
    Info,
    Warning,
    Error
}

public sealed class AccessibilityViolation
{
    [JsonPropertyName("ruleId")]
    public string RuleId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AccessibilityViolationSeverity Severity { get; set; } = AccessibilityViolationSeverity.Warning;

    [JsonPropertyName("elementId")]
    public string? ElementId { get; set; }

    [JsonPropertyName("suggestion")]
    public string? Suggestion { get; set; }
}

public sealed class AccessibilityAuditReport
{
    [JsonPropertyName("isCompliant")]
    public bool IsCompliant => ErrorCount == 0;

    [JsonPropertyName("score")]
    public double Score { get; set; } = 100.0;

    [JsonPropertyName("passedCount")]
    public int PassedCount { get; set; }

    [JsonPropertyName("warningCount")]
    public int WarningCount { get; set; }

    [JsonPropertyName("errorCount")]
    public int ErrorCount { get; set; }

    [JsonPropertyName("violations")]
    public List<AccessibilityViolation> Violations { get; set; } = [];
}

public sealed class ColorContrastResult
{
    [JsonPropertyName("ratio")]
    public double Ratio { get; set; }

    [JsonPropertyName("passesAaNormal")]
    public bool PassesAaNormal => Ratio >= 4.5;

    [JsonPropertyName("passesAaLarge")]
    public bool PassesAaLarge => Ratio >= 3.0;

    [JsonPropertyName("passesAaaNormal")]
    public bool PassesAaaNormal => Ratio >= 7.0;

    [JsonPropertyName("passesAaaLarge")]
    public bool PassesAaaLarge => Ratio >= 4.5;

    [JsonPropertyName("foregroundHex")]
    public string ForegroundHex { get; set; } = "#000000";

    [JsonPropertyName("backgroundHex")]
    public string BackgroundHex { get; set; } = "#FFFFFF";
}

public sealed class PdfStructElement
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("tagType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PdfTagType TagType { get; set; } = PdfTagType.Paragraph;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("altText")]
    public string? AltText { get; set; }

    [JsonPropertyName("actualText")]
    public string? ActualText { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("scope")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TableScopeType Scope { get; set; } = TableScopeType.None;

    [JsonPropertyName("headers")]
    public List<string> Headers { get; set; } = [];

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("children")]
    public List<PdfStructElement> Children { get; set; } = [];
}

public sealed class AccessibilityOptions
{
    [JsonPropertyName("enablePdfUa")]
    public bool EnablePdfUa { get; set; } = true;

    [JsonPropertyName("documentTitle")]
    public string? DocumentTitle { get; set; }

    [JsonPropertyName("primaryLanguage")]
    public string PrimaryLanguage { get; set; } = "th-TH";

    [JsonPropertyName("displayDocTitle")]
    public bool DisplayDocTitle { get; set; } = true;

    [JsonPropertyName("defaultImageAltText")]
    public string DefaultImageAltText { get; set; } = "ภาพประกอบรายงาน (Report Graphic)";

    [JsonPropertyName("defaultChartAltText")]
    public string DefaultChartAltText { get; set; } = "แผนภูมิแสดงผลข้อมูล (Data Visualization Chart)";

    [JsonPropertyName("enforceColorContrast")]
    public bool EnforceColorContrast { get; set; } = true;

    [JsonPropertyName("minContrastRatio")]
    public double MinContrastRatio { get; set; } = 4.5;
}
