using Bangplanix.Core.Formatting;
using Bangplanix.Core.Models;
using Bangplanix.Core.Payments;

namespace Bangplanix.Engine.Bands;

public sealed class BandContext
{
    public ReportDefinition Report { get; init; } = default!;
    public IDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<IDictionary<string, object?>> MainDataRows { get; init; } = [];
#pragma warning disable CA2227 // Collection properties should be read only (CurrentRow is updated per row in rendering loop)
    public IDictionary<string, object?>? CurrentRow { get; set; }
#pragma warning restore CA2227
    public int CurrentRowIndex { get; set; }
    public int CurrentPageNumber { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string? CurrentGroupKey { get; set; }

    public object? ResolveExpressionOrValue(string? text, string? expression)
    {
        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (string.IsNullOrEmpty(expression))
        {
            return null;
        }

        var expr = expression.Trim();
        if (expr.Equals("@Globals.PageNumber", StringComparison.OrdinalIgnoreCase))
        {
            return CurrentPageNumber;
        }
        if (expr.Equals("@Globals.TotalPages", StringComparison.OrdinalIgnoreCase))
        {
            return TotalPages;
        }
        if (expr.StartsWith("@Parameters.", StringComparison.OrdinalIgnoreCase))
        {
            var paramName = expr["@Parameters.".Length..];
            if (Parameters.TryGetValue(paramName, out var pVal))
            {
                return pVal;
            }
            var paramDef = Report.Parameters.FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));
            return paramDef?.DefaultValue;
        }
        if (expr.StartsWith("@Row.", StringComparison.OrdinalIgnoreCase) && CurrentRow != null)
        {
            var fieldName = expr["@Row.".Length..];
            if (CurrentRow.TryGetValue(fieldName, out var rowVal))
            {
                return rowVal;
            }
        }
        if (expr.StartsWith("PromptPayQr(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(')'))
        {
            var inner = expr[12..^1].Trim();
            var parts = inner.Split(',', 2);
            var targetId = parts[0].Trim(' ', '\'', '"');
            var resolvedTarget = ResolveExpressionOrValue(null, targetId)?.ToString() ?? targetId;
            decimal? amount = null;
            if (parts.Length > 1)
            {
                var resolvedAmt = ResolveExpressionOrValue(null, parts[1].Trim());
                if (resolvedAmt != null && decimal.TryParse(resolvedAmt.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amtVal))
                {
                    amount = amtVal;
                }
            }
            return PromptPayQrGenerator.GeneratePromptPayPayload(resolvedTarget, amount);
        }
        if (expr.StartsWith("BahtText(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(')'))
        {
            var inner = expr[9..^1].Trim();
            var resolvedVal = ResolveExpressionOrValue(null, inner);
            if (resolvedVal != null && decimal.TryParse(resolvedVal.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var decVal))
            {
                return BahtTextFormatter.ToBahtText(decVal);
            }
            return "ศูนย์บาทถ้วน";
        }
        if (expr.StartsWith("ThaiDate(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(')'))
        {
            var inner = expr[9..^1].Trim();
            var parts = inner.Split(',', 2);
            var dateVal = DateTime.Now;
            var fmt = "d MMMM yyyy";
            if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                var resolvedDate = ResolveExpressionOrValue(null, parts[0].Trim());
                if (resolvedDate != null && DateTime.TryParse(resolvedDate.ToString(), out var parsedDate))
                {
                    dateVal = parsedDate;
                }
            }
            if (parts.Length > 1)
            {
                fmt = parts[1].Trim(' ', '\'', '"');
            }
            return ThaiDateFormatter.FormatBuddhistDate(dateVal, fmt);
        }
        if (expr.StartsWith("Format(", StringComparison.OrdinalIgnoreCase) && expr.EndsWith(')'))
        {
            var inner = expr[7..^1];
            var parts = inner.Split(',', 2);
            if (parts.Length == 2)
            {
                var valExpr = parts[0].Trim();
                var fmt = parts[1].Trim(' ', '\'', '"');
                var resolvedVal = ResolveExpressionOrValue(null, valExpr);
                if (resolvedVal is IFormattable formattable)
                {
                    return formattable.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (double.TryParse(resolvedVal?.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var dVal))
                {
                    return dVal.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);
                }
                return resolvedVal?.ToString();
            }
        }
        if (expr.Contains('+', StringComparison.Ordinal))
        {
            var parts = expr.Split('+');
            var sb = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if ((trimmed.StartsWith('\'') && trimmed.EndsWith('\'')) || (trimmed.StartsWith('"') && trimmed.EndsWith('"')))
                {
                    sb.Append(trimmed[1..^1]);
                }
                else
                {
                    var resolved = ResolveExpressionOrValue(null, trimmed);
                    sb.Append(resolved?.ToString());
                }
            }
            return sb.ToString();
        }

        return expr;
    }
}
