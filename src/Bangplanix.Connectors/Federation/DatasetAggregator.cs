using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using Bangplanix.Core.Formatting;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Federation;

public static class DatasetAggregator
{
    private static readonly Regex BracketFieldRegex = new(@"\[([^\]]+)\]", RegexOptions.Compiled);

    public static IReadOnlyList<IDictionary<string, object?>> Aggregate(
        IReadOnlyList<IDictionary<string, object?>> rows,
        DatasetAggregationDefinition aggDef)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(aggDef);

        if (rows.Count == 0)
        {
            return Array.Empty<IDictionary<string, object?>>();
        }

        if (aggDef.EnableRollup && aggDef.GroupByFields.Count > 0)
        {
            return AggregateRollup(rows, aggDef);
        }

        return AggregateFlat(rows, aggDef.GroupByFields ?? [], aggDef.Aggregations ?? []);
    }

    private static IReadOnlyList<IDictionary<string, object?>> AggregateFlat(
        IReadOnlyList<IDictionary<string, object?>> rows,
        List<string> groupByFields,
        List<AggregateFieldDefinition> aggregations,
        int groupingLevel = 1,
        bool isSubtotal = false,
        bool isGrandTotal = false)
    {
        var groups = new Dictionary<string, List<IDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        var groupKeySamples = new Dictionary<string, IDictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var groupKey = BuildGroupKey(row, groupByFields);
            if (!groups.TryGetValue(groupKey, out var list))
            {
                list = new List<IDictionary<string, object?>>();
                groups[groupKey] = list;

                var sample = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var gf in groupByFields)
                {
                    DataJoinEngine.TryGetCaseInsensitive(row, gf, out var val);
                    sample[gf] = val;
                }
                groupKeySamples[groupKey] = sample;
            }
            list.Add(row);
        }

        var result = new List<IDictionary<string, object?>>(groups.Count);

        foreach (var (groupKey, groupRows) in groups)
        {
            var summaryRow = new Dictionary<string, object?>(groupKeySamples[groupKey], StringComparer.OrdinalIgnoreCase);

            foreach (var agg in aggregations)
            {
                var targetName = string.IsNullOrEmpty(agg.TargetField)
                    ? $"{agg.Function}_{agg.SourceField}"
                    : agg.TargetField;

                summaryRow[targetName] = ComputeAggregate(groupRows, agg.SourceField, agg.Function);
            }

            summaryRow["__GroupingLevel"] = groupingLevel;
            summaryRow["__IsSubtotal"] = isSubtotal;
            summaryRow["__IsGrandTotal"] = isGrandTotal;

            result.Add(summaryRow);
        }

        return result;
    }

    private static IReadOnlyList<IDictionary<string, object?>> AggregateRollup(
        IReadOnlyList<IDictionary<string, object?>> rows,
        DatasetAggregationDefinition aggDef)
    {
        var result = new List<IDictionary<string, object?>>();
        var fields = aggDef.GroupByFields;
        var aggs = aggDef.Aggregations ?? [];

        // 1. Detailed Group Level (Leaf)
        var detailedRows = AggregateFlat(rows, fields, aggs, groupingLevel: fields.Count, isSubtotal: false, isGrandTotal: false);
        result.AddRange(detailedRows);

        // 2. Subtotal Levels (from fields.Count - 1 down to 1)
        for (int level = fields.Count - 1; level >= 1; level--)
        {
            var subFields = fields.Take(level).ToList();
            var subtotalRows = AggregateFlat(rows, subFields, aggs, groupingLevel: level, isSubtotal: true, isGrandTotal: false);

            // Fill missing group fields with "[Subtotal]" label
            foreach (var sRow in subtotalRows)
            {
                for (int f = level; f < fields.Count; f++)
                {
                    sRow[fields[f]] = "[Subtotal]";
                }
            }
            result.AddRange(subtotalRows);
        }

        // 3. Grand Total Level (Level 0)
        if (aggDef.IncludeGrandTotal)
        {
            var grandTotalRows = AggregateFlat(rows, [], aggs, groupingLevel: 0, isSubtotal: false, isGrandTotal: true);
            foreach (var gRow in grandTotalRows)
            {
                if (fields.Count > 0)
                {
                    gRow[fields[0]] = "[Grand Total]";
                    for (int f = 1; f < fields.Count; f++)
                    {
                        gRow[fields[f]] = string.Empty;
                    }
                }
            }
            result.AddRange(grandTotalRows);
        }

        return result;
    }

    private static string BuildGroupKey(IDictionary<string, object?> row, List<string> groupByFields)
    {
        if (groupByFields.Count == 0)
        {
            return "__ALL__";
        }

        var parts = new string[groupByFields.Count];
        for (int i = 0; i < groupByFields.Count; i++)
        {
            DataJoinEngine.TryGetCaseInsensitive(row, groupByFields[i], out var val);
            parts[i] = val == null ? "NULL" : DataJoinEngine.NormalizeValueKey(val);
        }
        return string.Join("|##|", parts);
    }

    public static object? ComputeAggregate(
        IReadOnlyList<IDictionary<string, object?>> groupRows,
        string sourceField,
        AggregationFunction function)
    {
        ArgumentNullException.ThrowIfNull(groupRows);
        if (groupRows.Count == 0) return null;

        var values = new List<object>();
        foreach (var r in groupRows)
        {
            if (DataJoinEngine.TryGetCaseInsensitive(r, sourceField, out var val) && val != null && val is not DBNull)
            {
                values.Add(val);
            }
        }

        return function switch
        {
            AggregationFunction.Count => values.Count,
            AggregationFunction.DistinctCount => values.Select(DataJoinEngine.NormalizeValueKey).Distinct().Count(),
            AggregationFunction.Sum => ComputeSum(values),
            AggregationFunction.Avg => ComputeAvg(values),
            AggregationFunction.Min => ComputeMin(values),
            AggregationFunction.Max => ComputeMax(values),
            AggregationFunction.BahtText => ComputeBahtText(values),
            _ => throw new NotSupportedException($"Aggregation function '{function}' is not supported.")
        };
    }

    private static double ComputeSum(List<object> values)
    {
        double sum = 0.0;
        foreach (var v in values)
        {
            if (TryConvertToDouble(v, out var d))
            {
                sum += d;
            }
        }
        return sum;
    }

    private static double ComputeAvg(List<object> values)
    {
        if (values.Count == 0) return 0.0;
        double sum = 0.0;
        int count = 0;
        foreach (var v in values)
        {
            if (TryConvertToDouble(v, out var d))
            {
                sum += d;
                count++;
            }
        }
        return count > 0 ? sum / count : 0.0;
    }

    private static object? ComputeMin(List<object> values)
    {
        if (values.Count == 0) return null;
        if (values.All(v => TryConvertToDouble(v, out _)))
        {
            return values.Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).Min();
        }
        return values.OrderBy(v => v.ToString()).First();
    }

    private static object? ComputeMax(List<object> values)
    {
        if (values.Count == 0) return null;
        if (values.All(v => TryConvertToDouble(v, out _)))
        {
            return values.Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).Max();
        }
        return values.OrderByDescending(v => v.ToString()).First();
    }

    private static string ComputeBahtText(List<object> values)
    {
        var sum = (decimal)ComputeSum(values);
        return BahtTextFormatter.ToBahtText(sum);
    }

    public static bool TryConvertToDouble(object? val, out double result)
    {
        result = 0.0;
        if (val == null || val is DBNull) return false;
        if (val is double d) { result = d; return true; }
        if (val is float f) { result = f; return true; }
        if (val is decimal m) { result = (double)m; return true; }
        if (val is int i) { result = i; return true; }
        if (val is long l) { result = l; return true; }
        if (val is short s) { result = s; return true; }
        return double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static IReadOnlyList<IDictionary<string, object?>> EvaluateCalculatedColumns(
        IReadOnlyList<IDictionary<string, object?>> rows,
        List<CalculatedColumnDefinition>? calculatedColumns)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (calculatedColumns == null || calculatedColumns.Count == 0 || rows.Count == 0)
        {
            return rows;
        }

        var result = new List<IDictionary<string, object?>>(rows.Count);
        using var table = new DataTable();

        foreach (var row in rows)
        {
            var newRow = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);

            foreach (var col in calculatedColumns)
            {
                if (string.IsNullOrWhiteSpace(col.Name) || string.IsNullOrWhiteSpace(col.Expression))
                    continue;

                newRow[col.Name] = EvaluateExpressionForRow(newRow, col.Expression, table);
            }

            result.Add(newRow);
        }

        return result;
    }

    private static object? EvaluateExpressionForRow(IDictionary<string, object?> row, string expression, DataTable helperTable)
    {
        try
        {
            var replacedExpr = BracketFieldRegex.Replace(expression, match =>
            {
                var fieldName = match.Groups[1].Value;
                if (DataJoinEngine.TryGetCaseInsensitive(row, fieldName, out var val) && val != null && val is not DBNull)
                {
                    if (TryConvertToDouble(val, out var num))
                    {
                        return num.ToString(CultureInfo.InvariantCulture);
                    }
                    if (val is bool b)
                    {
                        return b ? "1" : "0";
                    }
                    return $"'{val.ToString()?.Replace("'", "''", StringComparison.Ordinal)}'";
                }
                return "0";
            });

            var computed = helperTable.Compute(replacedExpr, string.Empty);
            if (computed is DBNull) return null;
            return computed;
        }
        catch
        {
            return null;
        }
    }
}
