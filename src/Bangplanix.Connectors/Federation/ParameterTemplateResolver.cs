using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Bangplanix.Connectors.Federation;

public static class ParameterTemplateResolver
{
    private static readonly Regex ParameterTagRegex = new(@"\{\{(?:Parameters\.)?([a-zA-Z0-9_]+)\}\}|\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    public static string Resolve(string? template, IDictionary<string, object?>? parameters, bool urlEncode = false)
    {
        if (string.IsNullOrEmpty(template) || parameters == null || parameters.Count == 0)
        {
            return template ?? string.Empty;
        }

        return ParameterTagRegex.Replace(template, match =>
        {
            var paramName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

            if (DataJoinEngine.TryGetCaseInsensitive(parameters, paramName, out var val) && val != null && val is not DBNull)
            {
                var strVal = FormatParamValue(val);
                return urlEncode ? WebUtility.UrlEncode(strVal) : strVal;
            }

            return match.Value; // Keep original if not found
        });
    }

    public static Dictionary<string, string> ResolveHeaders(
        IDictionary<string, string>? headers,
        IDictionary<string, object?>? parameters)
    {
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (headers == null) return resolved;

        foreach (var (k, v) in headers)
        {
            resolved[k] = Resolve(v, parameters, urlEncode: false);
        }

        return resolved;
    }

    private static string FormatParamValue(object val)
    {
        if (val is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
        if (val is DateOnly d)
        {
            return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        if (val is double or float or decimal)
        {
            return Convert.ToString(val, CultureInfo.InvariantCulture) ?? string.Empty;
        }
        return val.ToString() ?? string.Empty;
    }
}
