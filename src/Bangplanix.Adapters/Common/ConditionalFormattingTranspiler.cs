using System.Xml.Linq;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Common;

/// <summary>
/// Represents a transpiled conditional formatting rule for dynamic element styling.
/// </summary>
public sealed class ConditionalFormattingRule
{
    public string ConditionExpression { get; set; } = string.Empty;
    public string? FontColor { get; set; }
    public string? BackgroundColor { get; set; }
    public string? FontWeight { get; set; }
    public string? FontStyle { get; set; }
    public bool IsSuppressed { get; set; }
}

/// <summary>
/// Transpiles conditional formatting and highlight rules from Crystal Reports, BIRT, and ActiveReports into Bangplanix expressions.
/// </summary>
public static class ConditionalFormattingTranspiler
{
    /// <summary>
    /// Transpiles Crystal Reports format formulas (e.g. "if {@Balance} &lt; 0 then crRed else crBlack") into conditional rules.
    /// </summary>
    public static ConditionalFormattingRule TranspileCrystalCondition(string formula)
    {
        var rule = new ConditionalFormattingRule();
        if (string.IsNullOrWhiteSpace(formula)) return rule;

        var transpiled = LegacyExpressionTranspiler.Transpile(formula, "Crystal");

        // Check for suppression condition (e.g. "{Field} = 0")
        if (formula.Contains("crRed", StringComparison.OrdinalIgnoreCase) || formula.Contains("Color.Red", StringComparison.OrdinalIgnoreCase))
        {
            rule.ConditionExpression = transpiled;
            rule.FontColor = "#DC2626"; // Red
        }
        else if (formula.Contains("crGreen", StringComparison.OrdinalIgnoreCase))
        {
            rule.ConditionExpression = transpiled;
            rule.FontColor = "#16A34A"; // Green
        }
        else if (formula.Contains("crYellow", StringComparison.OrdinalIgnoreCase))
        {
            rule.ConditionExpression = transpiled;
            rule.BackgroundColor = "#FEF08A"; // Yellow
        }
        else
        {
            // Default condition evaluation
            rule.ConditionExpression = transpiled;
        }

        return rule;
    }

    /// <summary>
    /// Transpiles Eclipse BIRT &lt;highlight-rule&gt; elements into a Bangplanix conditional rule.
    /// </summary>
    public static ConditionalFormattingRule TranspileBirtHighlight(XElement highlightRule)
    {
        var rule = new ConditionalFormattingRule();
        if (highlightRule == null) return rule;

        var operatorVal = highlightRule.Element("operator")?.Value ?? highlightRule.Attribute("operator")?.Value ?? "eq";
        var testExpr = highlightRule.Element("test-expr")?.Value ?? highlightRule.Attribute("test-expr")?.Value ?? "value";
        var value1 = highlightRule.Element("value1")?.Value ?? highlightRule.Attribute("value1")?.Value ?? "0";

        var transpiledTest = LegacyExpressionTranspiler.Transpile(testExpr, "Birt").TrimStart('=');
        var transpiledVal = LegacyExpressionTranspiler.Transpile(value1, "Birt").TrimStart('=');

        var condition = operatorVal.ToUpperInvariant() switch
        {
            "LT" or "LESS-THAN" => $"{transpiledTest} < {transpiledVal}",
            "LE" or "LESS-THAN-OR-EQUAL" => $"{transpiledTest} <= {transpiledVal}",
            "GT" or "GREATER-THAN" => $"{transpiledTest} > {transpiledVal}",
            "GE" or "GREATER-THAN-OR-EQUAL" => $"{transpiledTest} >= {transpiledVal}",
            "NE" or "NOT-EQUAL" => $"{transpiledTest} != {transpiledVal}",
            "IS-NULL" or "NULL" => $"LegacyExpressionPolyfills.IsNull({transpiledTest})",
            "IS-NOT-NULL" or "NOT-NULL" => $"LegacyExpressionPolyfills.IsNotNull({transpiledTest})",
            _ => $"{transpiledTest} == {transpiledVal}"
        };

        rule.ConditionExpression = $"={condition}";

        // Style overrides
        var color = highlightRule.Element("color")?.Value;
        var bg = highlightRule.Element("background-color")?.Value;
        var bold = highlightRule.Element("font-weight")?.Value;
        var italic = highlightRule.Element("font-style")?.Value;

        if (!string.IsNullOrEmpty(color)) rule.FontColor = color;
        if (!string.IsNullOrEmpty(bg)) rule.BackgroundColor = bg;
        if (!string.IsNullOrEmpty(bold)) rule.FontWeight = bold;
        if (!string.IsNullOrEmpty(italic)) rule.FontStyle = italic;

        return rule;
    }

    /// <summary>
    /// Applies conditional style rules onto an element definition.
    /// </summary>
    public static void ApplyRuleToElement(ElementDefinition element, ConditionalFormattingRule rule)
    {
        if (element == null || rule == null || string.IsNullOrWhiteSpace(rule.ConditionExpression)) return;

        element.Style ??= new StyleDefinition();

        if (!string.IsNullOrEmpty(rule.FontColor))
        {
            element.Style.Color = rule.FontColor;
        }

        if (!string.IsNullOrEmpty(rule.BackgroundColor))
        {
            element.Style.BackgroundColor = rule.BackgroundColor;
        }

        if (!string.IsNullOrEmpty(rule.FontWeight))
        {
            element.Style.FontWeight = rule.FontWeight;
        }

        if (!string.IsNullOrEmpty(rule.FontStyle))
        {
            element.Style.FontStyle = rule.FontStyle;
        }
    }
}
