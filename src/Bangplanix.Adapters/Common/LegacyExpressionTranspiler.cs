using System;
using System.Text.RegularExpressions;

namespace Bangplanix.Adapters.Common;

public static class LegacyExpressionTranspiler
{
    private static readonly Regex SsrsFieldRegex = new(@"Fields!([a-zA-Z0-9_]+)(?:\.Value)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SsrsParamRegex = new(@"Parameters!([a-zA-Z0-9_]+)(?:\.Value)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SsrsGlobalsRegex = new(@"Globals!([a-zA-Z0-9_]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SsrsIifRegex = new(@"IIf\s*\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SsrsIsNothingRegex = new(@"IsNothing\s*\(\s*([^)]+?)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex JasperFieldRegex = new(@"\$F\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex JasperParamRegex = new(@"\$P\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex JasperVarRegex = new(@"\$V\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);

    private static readonly Regex BracketFieldRegex = new(@"\[([a-zA-Z0-9_\.]+)\]", RegexOptions.Compiled);
    private static readonly Regex CurlyFieldRegex = new(@"(?<!\{)\{([a-zA-Z0-9_\.]+)\}(?!\})", RegexOptions.Compiled);
    private static readonly Regex LiquidFieldRegex = new(@"\{\{\s*([a-zA-Z0-9_\.]+)\s*\}\}", RegexOptions.Compiled);

    // Crystal Reports Patterns
    private static readonly Regex CrystalParamRegex = new(@"\{\?([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex CrystalFieldRegex = new(@"\{([a-zA-Z0-9_]+)\.([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex CrystalSimpleFieldRegex = new(@"\{([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex CrystalFormulaRegex = new(@"\{@([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex CrystalRunningTotalRegex = new(@"\{#([a-zA-Z0-9_]+)\}", RegexOptions.Compiled);
    private static readonly Regex CrystalIfElseRegex = new(@"if\s+(.+?)\s+then\s+(.+?)\s+else\s+(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // BIRT Patterns
    private static readonly Regex BirtDatasetRowRegex = new(@"(?:dataSetRow|row)\[[""']([a-zA-Z0-9_]+)[""']\]", RegexOptions.Compiled);
    private static readonly Regex BirtParamRegex = new(@"params\[[""']([a-zA-Z0-9_]+)[""']\](?:\.value)?", RegexOptions.Compiled);

    // Oracle Reports & BI Publisher Patterns
    private static readonly Regex OracleParamRegex = new(@"&<([a-zA-Z0-9_]+)>", RegexOptions.Compiled);
    private static readonly Regex OracleXdoFieldRegex = new(@"<\?(?:\.)?([a-zA-Z0-9_]+)\?>", RegexOptions.Compiled);
    private static readonly Regex OracleNvlRegex = new(@"NVL\s*\(\s*([^,]+?)\s*,\s*([^)]+?)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex OracleDecodeRegex = new(@"DECODE\s*\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Transpile(string? rawExpression, string sourceEngine = "SSRS")
    {
        if (string.IsNullOrWhiteSpace(rawExpression)) return string.Empty;

        var engine = sourceEngine ?? "SSRS";
        var expr = rawExpression.Trim();
        if (expr.StartsWith('='))
        {
            expr = expr[1..].Trim();
        }

        expr = engine.ToUpperInvariant() switch
        {
            "SSRS" or "RDL" or "RDLC" => TranspileSsrs(expr),
            "JASPER" or "JRXML" => TranspileJasper(expr),
            "FASTREPORT" or "FRX" => TranspileGenericBrackets(expr),
            "STIMULSOFT" or "MRT" => TranspileStimulsoft(expr),
            "TELERIK" or "TRDX" or "TRDP" => TranspileTelerik(expr),
            "DEVEXPRESS" or "REPX" => TranspileGenericBrackets(expr),
            "LIQUID" or "HTML" or "OFFICE" => TranspileLiquid(expr),
            "CRYSTAL" or "RPT" or "RPTXML" => TranspileCrystal(expr),
            "BIRT" or "RPTDESIGN" => TranspileBirt(expr),
            "ORACLE" or "REX" or "XDO" or "RDF" or "XSL" or "RTF" => TranspileOracle(expr),
            "ACTIVEREPORTS" or "RDLX" or "RPX" => TranspileActiveReports(expr),
            "COGNOS" or "SPEC" => TranspileCognos(expr),
            "PENTAHO" or "PRPT" => TranspilePentaho(expr),
            "HANDLEBARS" or "HBS" or "MUSTACHE" => TranspileLiquid(expr),
            "LABEL" or "ZPL" or "EPL" => TranspileGenericBrackets(expr),
            "ADOBE" or "XFA" or "XDP" or "FORMCALC" => TranspileXfa(expr),
            "ACCESS" or "ACCDB" or "MDB" or "VBA" => TranspileAccess(expr),
            "BARTENDER" or "BTW" => TranspileBarTender(expr),
            _ => TranspileSsrs(TranspileJasper(TranspileGenericBrackets(expr)))
        };

        return expr.StartsWith('=') ? expr : $"={expr}";
    }

    public static string TranspileCognos(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        expr = BracketFieldRegex.Replace(expr, "Fields.$1");
        return expr.StartsWith("Fields.") || expr.StartsWith("Parameters.") || expr.StartsWith("Variables.") ? expr : $"Fields.{expr}";
    }

    public static string TranspilePentaho(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        // Handle Pentaho [Field] or $(Field) or {Field}
        expr = CurlyFieldRegex.Replace(expr, "Fields.$1");
        expr = BracketFieldRegex.Replace(expr, "Fields.$1");
        return expr.StartsWith("Fields.") || expr.StartsWith("Parameters.") || expr.StartsWith("Variables.") ? expr : $"Fields.{expr}";
    }

    public static string TranspileSsrs(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        expr = SsrsFieldRegex.Replace(expr, "Fields.$1");
        expr = SsrsParamRegex.Replace(expr, "Parameters.$1");
        expr = SsrsGlobalsRegex.Replace(expr, "Globals.$1");
        expr = SsrsIsNothingRegex.Replace(expr, "($1 == null)");
        expr = SsrsIifRegex.Replace(expr, "($1 ? $2 : $3)");
        expr = Regex.Replace(expr, @"(?<=\s)&(?=\s)", "+");
        return expr;
    }

    public static string TranspileJasper(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        expr = JasperFieldRegex.Replace(expr, "Fields.$1");
        expr = JasperParamRegex.Replace(expr, "Parameters.$1");
        expr = JasperVarRegex.Replace(expr, match =>
        {
            var varName = match.Groups[1].Value;
            return varName switch
            {
                "PAGE_NUMBER" => "Globals.PageNumber",
                "TOTAL_PAGES" => "Globals.TotalPages",
                _ => $"Variables.{varName}"
            };
        });
        expr = Regex.Replace(expr, @"\.(?:doubleValue|intValue|longValue|toString)\(\)", "");
        return expr;
    }

    public static string TranspileGenericBrackets(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        return BracketFieldRegex.Replace(expr, match =>
        {
            var name = match.Groups[1].Value;
            if (name.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Fields.", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
            return $"Fields.{name}";
        });
    }

    public static string TranspileStimulsoft(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        return CurlyFieldRegex.Replace(expr, match =>
        {
            var name = match.Groups[1].Value;
            if (name.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Fields.", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
            return $"Fields.{name}";
        });
    }

    public static string TranspileTelerik(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        if (expr.StartsWith("=Fields.", StringComparison.OrdinalIgnoreCase) ||
            expr.StartsWith("Fields.", StringComparison.OrdinalIgnoreCase) ||
            expr.StartsWith("=Parameters.", StringComparison.OrdinalIgnoreCase) ||
            expr.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase))
        {
            return expr.TrimStart('=');
        }
        return TranspileGenericBrackets(expr);
    }

    public static string TranspileLiquid(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        return LiquidFieldRegex.Replace(expr, match =>
        {
            var name = match.Groups[1].Value;
            if (name.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Fields.", StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }
            return $"Fields.{name}";
        });
    }

    public static string TranspileCrystal(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        expr = CrystalParamRegex.Replace(expr, "Parameters.$1");
        expr = CrystalFormulaRegex.Replace(expr, "Formulas.$1");
        expr = CrystalRunningTotalRegex.Replace(expr, "Variables.$1");
        expr = CrystalFieldRegex.Replace(expr, "Fields.$2"); // e.g. {Customer.Name} -> Fields.Name
        expr = CrystalSimpleFieldRegex.Replace(expr, "Fields.$1");

        expr = Regex.Replace(expr, @"IsNull\s*\(\s*([^)]+?)\s*\)", "($1 == null)", RegexOptions.IgnoreCase);
        expr = Regex.Replace(expr, @"ToText\s*\(\s*([^)]+?)\s*\)", "Convert.ToString($1)", RegexOptions.IgnoreCase);
        expr = Regex.Replace(expr, @"Val\s*\(\s*([^)]+?)\s*\)", "Convert.ToDouble($1)", RegexOptions.IgnoreCase);

        if (CrystalIfElseRegex.IsMatch(expr))
        {
            expr = CrystalIfElseRegex.Replace(expr, "($1 ? $2 : $3)");
        }

        return expr;
    }

    public static string TranspileBirt(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        expr = BirtDatasetRowRegex.Replace(expr, "Fields.$1");
        expr = BirtParamRegex.Replace(expr, "Parameters.$1");
        expr = Regex.Replace(expr, @"Total\.sum\s*\(", "Sum(", RegexOptions.IgnoreCase);
        expr = Regex.Replace(expr, @"Total\.ave\s*\(", "Average(", RegexOptions.IgnoreCase);
        expr = Regex.Replace(expr, @"Total\.count\s*\(", "Count(", RegexOptions.IgnoreCase);
        return expr;
    }

    public static string TranspileOracle(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);

        if (!expr.Contains('&', StringComparison.Ordinal) &&
            !expr.Contains("<?", StringComparison.Ordinal) &&
            !expr.Contains('(', StringComparison.Ordinal) &&
            !expr.Contains('+', StringComparison.Ordinal) &&
            !expr.Contains('-', StringComparison.Ordinal) &&
            !expr.Contains('*', StringComparison.Ordinal) &&
            !expr.Contains('/', StringComparison.Ordinal))
        {
            return expr.StartsWith("Fields.", StringComparison.OrdinalIgnoreCase) ? expr : $"Fields.{expr}";
        }

        expr = OracleParamRegex.Replace(expr, "Parameters.$1");
        expr = OracleXdoFieldRegex.Replace(expr, "Fields.$1");
        expr = OracleNvlRegex.Replace(expr, "($1 ?? $2)");
        expr = OracleDecodeRegex.Replace(expr, "($1 == $2 ? $3 : $4)");
        return expr;
    }

    public static string TranspileActiveReports(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        expr = SsrsFieldRegex.Replace(expr, "Fields.$1");
        expr = SsrsParamRegex.Replace(expr, "Parameters.$1");
        expr = BracketFieldRegex.Replace(expr, "Fields.$1");
        expr = SsrsIifRegex.Replace(expr, "($1 ? $2 : $3)");
        return expr;
    }

    public static string TranspileXfa(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        var s = expr.Trim();

        // Strip leading assignment e.g. "$ = ..." or "event.value = ..."
        s = Regex.Replace(s, @"^\s*(?:\$|event\.value)\s*=\s*", string.Empty, RegexOptions.IgnoreCase);

        // Acrobat JS: this.getField("FieldName").value / xfa.resolveNode("...").rawValue
        s = Regex.Replace(s, @"this\.getField\s*\(\s*[""']([^""']+)[""']\s*\)(?:\.value)?", "Fields.$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"xfa\.resolveNode\s*\(\s*[""'](?:.*?\.)?([a-zA-Z0-9_]+)[""']\s*\)(?:\.rawValue)?", "Fields.$1", RegexOptions.IgnoreCase);

        // FormCalc / XFA record paths: $record.FIELD or xfa.record.FIELD or data.subform.FIELD
        s = Regex.Replace(s, @"(?:\$record|xfa\.record|data(?:\.[a-zA-Z0-9_]+)*)\.([a-zA-Z0-9_]+)", "Fields.$1", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"(?:\$|this)\.rawValue", "Fields.Value", RegexOptions.IgnoreCase);

        // FormCalc Sum(items[*].amount) or Sum(amount)
        s = Regex.Replace(s, @"Sum\s*\(\s*(?:[a-zA-Z0-9_]+\[\*\]\.)?([a-zA-Z0-9_]+)\s*\)", "Sum(Fields.$1)", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"Count\s*\(\s*(?:[a-zA-Z0-9_]+\[\*\]\.)?([a-zA-Z0-9_]+)\s*\)", "Count(Fields.$1)", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"Avg\s*\(\s*(?:[a-zA-Z0-9_]+\[\*\]\.)?([a-zA-Z0-9_]+)\s*\)", "Average(Fields.$1)", RegexOptions.IgnoreCase);

        // FormCalc Concat(...)
        s = Regex.Replace(s, @"Concat\s*\(", "string.Concat(", RegexOptions.IgnoreCase);

        // FormCalc HasValue(x) -> (x != null)
        s = Regex.Replace(s, @"HasValue\s*\(\s*([^)]+?)\s*\)", "($1 != null)", RegexOptions.IgnoreCase);

        // FormCalc if (...) then ... [else ...] endif
        s = Regex.Replace(s, @"if\s*\(\s*(.+?)\s*\)\s*then\s*(.+?)\s*(?:else\s*(.+?)\s*)?endif", "($1 ? $2 : ($3))", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return s.Trim();
    }

    public static string TranspileAccess(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        var s = expr.Trim();

        // Access string concatenation using &
        if (s.Contains('&'))
        {
            var parts = s.Split('&');
            var cleanParts = new List<string>();
            foreach (var part in parts)
            {
                var p = part.Trim();
                if (p.StartsWith('[') && p.EndsWith(']'))
                {
                    cleanParts.Add($"Fields.{p.Trim('[', ']')}");
                }
                else
                {
                    cleanParts.Add(BracketFieldRegex.Replace(p, "Fields.$1"));
                }
            }
            return $"string.Concat({string.Join(", ", cleanParts)})";
        }

        // Bracketed field references [Field]
        s = BracketFieldRegex.Replace(s, "Fields.$1");

        // Access functions
        s = Regex.Replace(s, @"Nz\s*\(\s*([a-zA-Z0-9_\.]+)\s*,\s*([^)]+?)\s*\)", "($1 ?? $2)", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"IIf\s*\(\s*([^,]+?)\s*,\s*([^,]+?)\s*,\s*([^)]+?)\s*\)", m =>
        {
            var cond = m.Groups[1].Value.Trim();
            // Ensure comparison operators have spacing for readability
            cond = Regex.Replace(cond, @"(?<=\w)(>|<|==|>=|<=|!=)(?=\w)", " $1 ");
            var trueVal = m.Groups[2].Value.Trim();
            var falseVal = m.Groups[3].Value.Trim();
            return $"({cond} ? {trueVal} : {falseVal})";
        }, RegexOptions.IgnoreCase);

        s = Regex.Replace(s, @"Sum\s*\(\s*([a-zA-Z0-9_\.]+)\s*\)", "Sum($1)", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"Avg\s*\(\s*([a-zA-Z0-9_\.]+)\s*\)", "Average($1)", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"Count\s*\(\s*([a-zA-Z0-9_\.]+)\s*\)", "Count($1)", RegexOptions.IgnoreCase);

        return s;
    }

    public static string TranspileBarTender(string expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        var s = expr.Trim();

        // BarTender variable tokens: %FieldName% or [Database.FieldName]
        s = Regex.Replace(s, @"%([a-zA-Z0-9_]+)%", "Fields.$1");
        s = Regex.Replace(s, @"\[(?:Database\.)?([a-zA-Z0-9_]+)\]", "Fields.$1");

        return s;
    }
}