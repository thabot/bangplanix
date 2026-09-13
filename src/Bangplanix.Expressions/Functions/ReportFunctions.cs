using System.Globalization;
using Bangplanix.Core.Formatting;
using Bangplanix.Core.Payments;

namespace Bangplanix.Expressions.Functions;

public static class ReportFunctions
{
    // Logical
    public static T IIf<T>(bool condition, T truePart, T falsePart) => condition ? truePart : falsePart;

    public static object? Choose(int index, params object?[] choices)
    {
        if (choices == null || index < 1 || index > choices.Length)
        {
            return null;
        }
        return choices[index - 1];
    }

    // Formatting
    public static string Format(object? value, string format)
    {
        if (value is null) return string.Empty;
        if (value is IFormattable formattable)
        {
            return formattable.ToString(format, CultureInfo.InvariantCulture);
        }
        if (double.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var dVal))
        {
            return dVal.ToString(format, CultureInfo.InvariantCulture);
        }
        return value.ToString() ?? string.Empty;
    }

    public static string BahtText(decimal amount) => BahtTextFormatter.ToBahtText(amount);

    public static string ThaiDate(DateTime? date = null, string format = "d MMMM yyyy")
    {
        return ThaiDateFormatter.FormatBuddhistDate(date ?? DateTime.Now, format);
    }

    public static string PromptPayQr(string targetId, decimal? amount = null)
    {
        return PromptPayQrGenerator.GeneratePromptPayPayload(targetId, amount);
    }

    // Math
    public static double Abs(double value) => Math.Abs(value);
    public static double Round(double value, int decimals = 2) => Math.Round(value, decimals);
    public static double Ceiling(double value) => Math.Ceiling(value);
    public static double Floor(double value) => Math.Floor(value);

    // SIMD Vectorized Aggregations (AVX-512 / Neon hardware accelerated)
    public static double Sum(params double[] values) => SimdAggregations.Sum(values);
    public static double Sum(IEnumerable<double> values) => SimdAggregations.Sum(values.ToArray());
    public static double Avg(params double[] values) => SimdAggregations.Average(values);
    public static double Avg(IEnumerable<double> values) => SimdAggregations.Average(values.ToArray());
    public static double Min(params double[] values) => SimdAggregations.Min(values);
    public static double Min(IEnumerable<double> values) => SimdAggregations.Min(values.ToArray());
    public static double Max(params double[] values) => SimdAggregations.Max(values);
    public static double Max(IEnumerable<double> values) => SimdAggregations.Max(values.ToArray());
    public static int Count<T>(IEnumerable<T> items) => items?.Count() ?? 0;

    // Date
    public static DateTime Today() => DateTime.Today;
    public static DateTime Now() => DateTime.Now;
    public static int Year(DateTime date) => date.Year;
    public static int Month(DateTime date) => date.Month;
    public static int Day(DateTime date) => date.Day;
}
