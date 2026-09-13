using System.Globalization;
using System.Text.RegularExpressions;
using Bangplanix.Core.Models;

namespace Bangplanix.Core.Parameters;

public static class ParameterValidator
{
    public static Dictionary<string, object?> ResolveAndValidate(
        ReportDefinition report,
        IDictionary<string, object?>? userInputs,
        out List<string> validationErrors)
    {
        ArgumentNullException.ThrowIfNull(report);
        validationErrors = [];
        var resolved = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var inputs = userInputs != null
            ? new Dictionary<string, object?>(userInputs, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var paramDef in report.Parameters)
        {
            var hasInput = inputs.TryGetValue(paramDef.Name, out var rawVal) && rawVal != null;
            object? finalVal = null;

            if (hasInput)
            {
                finalVal = rawVal;
            }
            else if (paramDef.DefaultValue != null)
            {
                finalVal = paramDef.DefaultValue;
            }

            // 1. Required Check
            if (paramDef.IsRequired && (finalVal == null || (finalVal is string s && string.IsNullOrWhiteSpace(s))))
            {
                validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' is required.");
                continue;
            }

            if (finalVal == null)
            {
                resolved[paramDef.Name] = null;
                continue;
            }

            // 2. Type Conversion & Validation
            try
            {
                switch (paramDef.Type)
                {
                    case ParameterType.Number:
                        if (decimal.TryParse(finalVal.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var decVal))
                        {
                            // Min / Max Check
                            if (paramDef.MinValue != null && decimal.TryParse(paramDef.MinValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var minVal) && decVal < minVal)
                            {
                                validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be >= {minVal}.");
                            }
                            if (paramDef.MaxValue != null && decimal.TryParse(paramDef.MaxValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var maxVal) && decVal > maxVal)
                            {
                                validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be <= {maxVal}.");
                            }
                            finalVal = decVal;
                        }
                        else
                        {
                            validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be a valid number.");
                        }
                        break;

                    case ParameterType.DateTime:
                        if (DateTime.TryParse(finalVal.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtVal))
                        {
                            if (paramDef.MinValue != null && DateTime.TryParse(paramDef.MinValue.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var minDt) && dtVal < minDt)
                            {
                                validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be after {minDt:yyyy-MM-dd}.");
                            }
                            if (paramDef.MaxValue != null && DateTime.TryParse(paramDef.MaxValue.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var maxDt) && dtVal > maxDt)
                            {
                                validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be before {maxDt:yyyy-MM-dd}.");
                            }
                            finalVal = dtVal;
                        }
                        else
                        {
                            validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be a valid date/time.");
                        }
                        break;

                    case ParameterType.Boolean:
                        if (bool.TryParse(finalVal.ToString(), out var bVal))
                        {
                            finalVal = bVal;
                        }
                        else
                        {
                            validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' must be a boolean (true/false).");
                        }
                        break;

                    case ParameterType.String:
                        var strVal = finalVal.ToString()!;
                        if (!string.IsNullOrEmpty(paramDef.ValidationPattern))
                        {
                            if (!Regex.IsMatch(strVal, paramDef.ValidationPattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)))
                            {
                                validationErrors.Add($"Parameter '{paramDef.Label ?? paramDef.Name}' format is invalid.");
                            }
                        }
                        finalVal = strVal;
                        break;
                }
            }
            catch (Exception ex)
            {
                validationErrors.Add($"Error evaluating parameter '{paramDef.Name}': {ex.Message}");
            }

            resolved[paramDef.Name] = finalVal;
        }

        return resolved;
    }

    public static Dictionary<string, object?> GetDefaultValues(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in report.Parameters)
        {
            dict[p.Name] = p.DefaultValue;
        }
        return dict;
    }
}