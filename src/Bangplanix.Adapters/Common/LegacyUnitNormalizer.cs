using System.Globalization;
using System.Text.RegularExpressions;

namespace Bangplanix.Adapters.Common;

public static class LegacyUnitNormalizer
{
    private static readonly Regex UnitRegex = new(@"^\s*([+-]?\d+(?:\.\d+)?)\s*(in|cm|mm|pt|px)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public const double PointsPerInch = 72.0;
    public const double MmPerInch = 25.4;
    public const double PointsPerMm = PointsPerInch / MmPerInch; // ~2.83464567

    public static double ConvertToPoints(string? value, double defaultVal = 0.0, double dpi = 96.0)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultVal;

        var match = UnitRegex.Match(value.Trim());
        if (!match.Success)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }
            return defaultVal;
        }

        var num = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "in" => num * PointsPerInch,
            "cm" => (num * 10.0) * PointsPerMm,
            "mm" => num * PointsPerMm,
            "pt" => num,
            "px" => (num / dpi) * PointsPerInch,
            _ => num // default fallback
        };
    }

    public static double ConvertToMillimeters(string? value, double defaultVal = 0.0, double dpi = 96.0)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultVal;

        var pt = ConvertToPoints(value, defaultVal * PointsPerMm, dpi);
        return pt / PointsPerMm;
    }
}
