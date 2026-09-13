using System.Globalization;

namespace Bangplanix.Expressions.Polyfills;

/// <summary>
/// Universal runtime polyfill functions for legacy report expressions (Crystal Reports, Eclipse BIRT, Oracle Reports, ActiveReports).
/// </summary>
public static class LegacyExpressionPolyfills
{
    // ==========================================
    // 1. Crystal Reports & VB Expressions
    // ==========================================

    public static bool IsNull(object? value)
    {
        return value is null || value is DBNull || (value is string s && string.IsNullOrEmpty(s));
    }

    public static bool IsNotNull(object? value) => !IsNull(value);

    public static string ToText(object? value, string? format = null)
    {
        if (IsNull(value)) return string.Empty;
        if (value is IFormattable formattable && !string.IsNullOrEmpty(format))
        {
            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }
        return value?.ToString() ?? string.Empty;
    }

    public static decimal ToNumber(object? value)
    {
        if (IsNull(value)) return 0m;
        if (value is decimal d) return d;
        if (value is double dbl) return (decimal)dbl;
        if (value is int i) return i;
        if (value is long l) return l;
        if (decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }
        return 0m;
    }

    public static string Left(string? value, int length)
    {
        if (string.IsNullOrEmpty(value) || length <= 0) return string.Empty;
        return value.Length <= length ? value : value[..length];
    }

    public static string Right(string? value, int length)
    {
        if (string.IsNullOrEmpty(value) || length <= 0) return string.Empty;
        return value.Length <= length ? value : value[^length..];
    }

    public static string Mid(string? value, int startIndex, int length = -1)
    {
        if (string.IsNullOrEmpty(value) || startIndex < 1) return string.Empty;
        var zeroIndex = startIndex - 1;
        if (zeroIndex >= value.Length) return string.Empty;

        if (length < 0 || zeroIndex + length > value.Length)
        {
            return value[zeroIndex..];
        }

        return value.Substring(zeroIndex, length);
    }

    public static object? IIF(bool condition, object? truePart, object? falsePart)
    {
        return condition ? truePart : falsePart;
    }

    public static DateTime CurrentDate() => DateTime.Today;

    public static DateTime CurrentDateTime() => DateTime.Now;

    public static DateTime DateAdd(string interval, int number, DateTime date)
    {
        if (string.IsNullOrEmpty(interval)) return date;
        var lower = interval.ToLowerInvariant();
        return lower switch
        {
            "yyyy" or "year" or "y" => date.AddYears(number),
            "m" or "month" => date.AddMonths(number),
            "d" or "day" => date.AddDays(number),
            "h" or "hour" => date.AddHours(number),
            "n" or "minute" => date.AddMinutes(number),
            "s" or "second" => date.AddSeconds(number),
            _ => date.AddDays(number)
        };
    }

    // ==========================================
    // 2. Oracle Reports & PL/SQL Functions
    // ==========================================

    public static object? NVL(object? value, object? replaceValue)
    {
        return IsNull(value) ? replaceValue : value;
    }

    public static object? NVL2(object? value, object? notNullValue, object? nullValue)
    {
        return IsNull(value) ? nullValue : notNullValue;
    }

    public static object? DECODE(params object?[] args)
    {
        if (args == null || args.Length < 3) return null;

        var target = args[0];
        var i = 1;
        while (i < args.Length - 1)
        {
            var matchValue = args[i];
            var resultValue = args[i + 1];

            if (Equals(target, matchValue) || (target != null && matchValue != null && target.ToString() == matchValue.ToString()))
            {
                return resultValue;
            }
            i += 2;
        }

        // If default value exists (odd number of args after target)
        if (i == args.Length - 1)
        {
            return args[^1];
        }

        return null;
    }

    public static string TO_CHAR(object? value, string? format = null) => ToText(value, format);

    public static decimal TO_NUMBER(object? value) => ToNumber(value);
}

/// <summary>
/// BIRT DateTime helper polyfill (BirtDateTime.now, BirtDateTime.year, etc.).
/// </summary>
public static class BirtDateTime
{
    public static DateTime Now() => DateTime.Now;
    public static DateTime Today() => DateTime.Today;
    public static int Year(DateTime date) => date.Year;
    public static int Month(DateTime date) => date.Month;
    public static int Day(DateTime date) => date.Day;

    public static int DiffDay(DateTime start, DateTime end) => (int)(end - start).TotalDays;
    public static int DiffMonth(DateTime start, DateTime end) => ((end.Year - start.Year) * 12) + end.Month - start.Month;
    public static int DiffYear(DateTime start, DateTime end) => end.Year - start.Year;
}

/// <summary>
/// BIRT Math helper polyfill (BirtMath.round, BirtMath.ceil, etc.).
/// </summary>
public static class BirtMath
{
    public static decimal Round(decimal value, int decimals = 2) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    public static double Round(double value, int decimals = 2) => Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    public static decimal Ceil(decimal value) => Math.Ceiling(value);
    public static decimal Floor(decimal value) => Math.Floor(value);
    public static decimal Abs(decimal value) => Math.Abs(value);
}

/// <summary>
/// BIRT String helper polyfill (BirtStr.concat, BirtStr.trim, etc.).
/// </summary>
public static class BirtStr
{
    public static string Concat(params string?[] parts)
    {
        if (parts == null || parts.Length == 0) return string.Empty;
        return string.Concat(parts);
    }

    public static string Trim(string? value) => value?.Trim() ?? string.Empty;
    public static string ToUpper(string? value) => value?.ToUpperInvariant() ?? string.Empty;
    public static string ToLower(string? value) => value?.ToLowerInvariant() ?? string.Empty;

    public static string Substring(string? value, int start, int length)
    {
        if (string.IsNullOrEmpty(value) || start < 0) return string.Empty;
        if (start >= value.Length) return string.Empty;
        return start + length > value.Length ? value[start..] : value.Substring(start, length);
    }
}
