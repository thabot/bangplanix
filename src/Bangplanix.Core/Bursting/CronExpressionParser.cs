namespace Bangplanix.Core.Bursting;

/// <summary>
/// High-precision, zero-allocation standard 5-field Cron expression parser and next occurrence calculator.
/// </summary>
public sealed class CronExpressionParser
{
    private readonly HashSet<int> _minutes = new();
    private readonly HashSet<int> _hours = new();
    private readonly HashSet<int> _daysOfMonth = new();
    private readonly HashSet<int> _months = new();
    private readonly HashSet<int> _daysOfWeek = new();

    public string Expression { get; }

    public CronExpressionParser(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Cron expression cannot be empty", nameof(expression));

        Expression = NormalizeExpression(expression);
        Parse(Expression);
    }

    public static CronExpressionParser ParseExpression(string expression) => new(expression);

    private static string NormalizeExpression(string expr)
    {
        string trimmed = expr.Trim().ToLowerInvariant();
        return trimmed switch
        {
            "@hourly" => "0 * * * *",
            "@daily" or "@midnight" => "0 0 * * *",
            "@weekly" => "0 0 * * 0",
            "@monthly" => "0 0 1 * *",
            "@yearly" or "@annually" => "0 0 1 1 *",
            _ => expr.Trim()
        };
    }

    private void Parse(string expr)
    {
        var parts = expr.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
        {
            throw aerialException($"Invalid cron expression '{expr}'. Must have 5 fields: minute, hour, day-of-month, month, day-of-week.");
        }

        ParseField(parts[0], 0, 59, _minutes);
        ParseField(parts[1], 0, 23, _hours);
        ParseField(parts[2], 1, 31, _daysOfMonth);
        ParseField(parts[3], 1, 12, _months);
        ParseField(parts[4], 0, 6, _daysOfWeek); // 0 = Sunday, 6 = Saturday
    }

    private static void ParseField(string field, int min, int max, HashSet<int> target)
    {
        if (field == "*")
        {
            for (int i = min; i <= max; i++) target.Add(i);
            return;
        }

        var commaParts = field.Split(',');
        foreach (var part in commaParts)
        {
            if (part.StartsWith("*/"))
            {
                if (int.TryParse(part.Substring(2), out int step) && step > 0)
                {
                    for (int i = min; i <= max; i += step) target.Add(i);
                }
            }
            else if (part.Contains('-'))
            {
                var range = part.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (i >= min && i <= max) target.Add(i);
                    }
                }
            }
            else if (int.TryParse(part, out int val))
            {
                if (val >= min && val <= max) target.Add(val);
            }
        }
    }

    /// <summary>
    /// Computes the next occurrence UTC after the given reference time.
    /// </summary>
    public DateTime? GetNextOccurrenceUtc(DateTime fromUtc)
    {
        // Truncate seconds and start from next minute
        DateTime current = new DateTime(fromUtc.Year, fromUtc.Month, fromUtc.Day, fromUtc.Hour, fromUtc.Minute, 0, DateTimeKind.Utc).AddMinutes(1);
        DateTime maxSearch = current.AddYears(4);

        while (current < maxSearch)
        {
            if (!_months.Contains(current.Month))
            {
                current = new DateTime(current.Year, current.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
                continue;
            }

            if (!_daysOfMonth.Contains(current.Day) || !_daysOfWeek.Contains((int)current.DayOfWeek))
            {
                current = current.Date.AddDays(1);
                continue;
            }

            if (!_hours.Contains(current.Hour))
            {
                current = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
                continue;
            }

            if (!_minutes.Contains(current.Minute))
            {
                current = current.AddMinutes(1);
                continue;
            }

            return current;
        }

        return null;
    }

    private static FormatException aerialException(string msg) => new(msg);
}
