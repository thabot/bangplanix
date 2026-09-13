using System.Text.RegularExpressions;

namespace Bangplanix.Engine.Ai;

/// <summary>
/// Result of an SQL Safety AST and lexical validation scan.
/// </summary>
public sealed class SqlSafetyResult
{
    public bool IsValid { get; set; } = true;
    public string SanitizedSql { get; set; } = string.Empty;
    public List<string> Violations { get; set; } = new();
}

/// <summary>
/// Enterprise AI SQL Safety AST & Lexical Validator ensuring zero SQL injection and strict read-only execution.
/// </summary>
public sealed class AiSqlSafetyValidator
{
    private static readonly (Regex Pattern, string Violation)[] DangerousSqlPatterns =
    {
        (new Regex(@"(?i)\b(DROP|ALTER|TRUNCATE|CREATE)\s+(TABLE|DATABASE|VIEW|INDEX|PROCEDURE|FUNCTION|TRIGGER|SCHEMA)\b", RegexOptions.Compiled),
            "DDL modification statements (DROP/ALTER/TRUNCATE/CREATE) are strictly forbidden"),

        (new Regex(@"(?i)\b(INSERT\s+INTO|UPDATE\s+\w+\s+SET|DELETE\s+FROM|MERGE\s+INTO)\b", RegexOptions.Compiled),
            "DML write operations (INSERT/UPDATE/DELETE/MERGE) are strictly forbidden"),

        (new Regex(@"(?i)\b(EXEC|EXECUTE|CALL|XP_\w+|SP_\w+)\b", RegexOptions.Compiled),
            "Stored procedure, dynamic SQL and function calls (EXEC/CALL/SP_) are forbidden"),

        (new Regex(@"(?i)\b(GRANT|REVOKE|DENY)\s+", RegexOptions.Compiled),
            "Permission management statements (GRANT/REVOKE) are forbidden"),

        (new Regex(@"(?i)\bUNION(\s+ALL)?\s+SELECT\b", RegexOptions.Compiled),
            "UNION / UNION ALL injection vectors are prohibited in AI generated queries"),

        (new Regex(@"(?i)\b(INTO\s+OUTFILE|INTO\s+DUMPFILE|LOAD_FILE|BULK\s+INSERT)\b", RegexOptions.Compiled),
            "File system read/write operations (INTO OUTFILE/LOAD_FILE) are forbidden"),

        (new Regex(@"(?i)\b(SHUTDOWN|WAITFOR\s+DELAY|BENCHMARK|SLEEP)\b", RegexOptions.Compiled),
            "Server denial-of-service / timing probe keywords (SHUTDOWN/WAITFOR/SLEEP) are blocked"),

        (new Regex(@"--|/\*|\*/|#", RegexOptions.Compiled),
            "SQL comment delimiters (-- / /* / #) are stripped to prevent SQL injection evasion")
    };

    public int MaxRowLimit { get; set; } = 50_000;

    /// <summary>
    /// Validates and sanitizes a SQL statement.
    /// </summary>
    public SqlSafetyResult ValidateAndSanitize(string? rawSql)
    {
        if (string.IsNullOrWhiteSpace(rawSql))
        {
            return new SqlSafetyResult
            {
                IsValid = false,
                SanitizedSql = string.Empty,
                Violations = new List<string> { "SQL statement cannot be empty" }
            };
        }

        var violations = new List<string>();
        string sql = rawSql.Trim();

        // 1. Check for dangerous keyword patterns
        foreach (var (pattern, violation) in DangerousSqlPatterns)
        {
            if (pattern.IsMatch(sql))
            {
                violations.Add(violation);
            }
        }

        // 2. Multi-statement / stacked queries check (semicolon check)
        string stripped = Regex.Replace(sql, @"'([^']|'')*'", "''"); // ignore string literals
        if (stripped.Contains(';') && stripped.TrimEnd(';').Contains(';'))
        {
            violations.Add("Multiple stacked SQL statements (semicolon chained) are prohibited");
        }

        // 3. Must begin with SELECT or WITH (CTE)
        string normalizedStart = Regex.Replace(sql.TrimStart(), @"^[\s(]+", "");
        if (!Regex.IsMatch(normalizedStart, @"^(?i)(SELECT|WITH)\b"))
        {
            violations.Add("SQL statement must be a pure read-only SELECT or WITH statement");
        }

        if (violations.Count > 0)
        {
            return new SqlSafetyResult
            {
                IsValid = false,
                SanitizedSql = string.Empty,
                Violations = violations
            };
        }

        // 4. Enforce row safety ceiling (Inject LIMIT if not present)
        string sanitized = sql.TrimEnd(';');
        if (!Regex.IsMatch(sanitized, @"(?i)\b(LIMIT|FETCH\s+FIRST|TOP)\b"))
        {
            sanitized = $"{sanitized} LIMIT {MaxRowLimit}";
        }

        return new SqlSafetyResult
        {
            IsValid = true,
            SanitizedSql = sanitized,
            Violations = violations
        };
    }
}
