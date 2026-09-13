using System.Globalization;
using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Federation;

public static class DataJoinEngine
{
    public static IReadOnlyList<IDictionary<string, object?>> ExecuteJoin(
        IReadOnlyList<IDictionary<string, object?>> leftRows,
        IReadOnlyList<IDictionary<string, object?>> rightRows,
        DataJoinDefinition joinDef)
    {
        ArgumentNullException.ThrowIfNull(leftRows);
        ArgumentNullException.ThrowIfNull(rightRows);
        ArgumentNullException.ThrowIfNull(joinDef);

        if (joinDef.JoinType == JoinType.Cross)
        {
            return ExecuteCrossJoin(leftRows, rightRows, joinDef.RightPrefix);
        }

        var keyMappings = joinDef.KeyMappings;
        if (keyMappings == null || keyMappings.Count == 0)
        {
            throw new ArgumentException("At least one key mapping must be specified for non-cross join.", nameof(joinDef));
        }

        return joinDef.JoinType switch
        {
            JoinType.Inner => ExecuteInnerJoin(leftRows, rightRows, keyMappings, joinDef.RightPrefix),
            JoinType.LeftOuter => ExecuteLeftOuterJoin(leftRows, rightRows, keyMappings, joinDef.RightPrefix),
            JoinType.RightOuter => ExecuteRightOuterJoin(leftRows, rightRows, keyMappings, joinDef.RightPrefix),
            JoinType.FullOuter => ExecuteFullOuterJoin(leftRows, rightRows, keyMappings, joinDef.RightPrefix),
            _ => throw new NotSupportedException($"Join type '{joinDef.JoinType}' is not supported.")
        };
    }

    private static IReadOnlyList<IDictionary<string, object?>> ExecuteCrossJoin(
        IReadOnlyList<IDictionary<string, object?>> leftRows,
        IReadOnlyList<IDictionary<string, object?>> rightRows,
        string? rightPrefix)
    {
        if (leftRows.Count == 0 || rightRows.Count == 0)
            return Array.Empty<IDictionary<string, object?>>();

        var result = new List<IDictionary<string, object?>>(leftRows.Count * rightRows.Count);
        foreach (var left in leftRows)
        {
            foreach (var right in rightRows)
            {
                result.Add(MergeRow(left, right, rightPrefix));
            }
        }
        return result;
    }

    private static IReadOnlyList<IDictionary<string, object?>> ExecuteInnerJoin(
        IReadOnlyList<IDictionary<string, object?>> leftRows,
        IReadOnlyList<IDictionary<string, object?>> rightRows,
        List<JoinKeyMapping> keyMappings,
        string? rightPrefix)
    {
        var rightLookup = BuildLookup(rightRows, keyMappings.Select(k => k.RightField).ToList());
        var result = new List<IDictionary<string, object?>>();

        var leftKeys = keyMappings.Select(k => k.LeftField).ToList();
        foreach (var left in leftRows)
        {
            var key = ExtractCompositeKey(left, leftKeys);
            if (key != null && rightLookup.TryGetValue(key, out var matchedRights))
            {
                foreach (var right in matchedRights)
                {
                    result.Add(MergeRow(left, right, rightPrefix));
                }
            }
        }

        return result;
    }

    private static IReadOnlyList<IDictionary<string, object?>> ExecuteLeftOuterJoin(
        IReadOnlyList<IDictionary<string, object?>> leftRows,
        IReadOnlyList<IDictionary<string, object?>> rightRows,
        List<JoinKeyMapping> keyMappings,
        string? rightPrefix)
    {
        var rightLookup = BuildLookup(rightRows, keyMappings.Select(k => k.RightField).ToList());
        var allRightKeys = CollectDistinctKeys(rightRows);
        var result = new List<IDictionary<string, object?>>(leftRows.Count);

        var leftKeys = keyMappings.Select(k => k.LeftField).ToList();
        foreach (var left in leftRows)
        {
            var key = ExtractCompositeKey(left, leftKeys);
            if (key != null && rightLookup.TryGetValue(key, out var matchedRights) && matchedRights.Count > 0)
            {
                foreach (var right in matchedRights)
                {
                    result.Add(MergeRow(left, right, rightPrefix));
                }
            }
            else
            {
                result.Add(MergeRowWithNulls(left, allRightKeys, rightPrefix));
            }
        }

        return result;
    }

    private static IReadOnlyList<IDictionary<string, object?>> ExecuteRightOuterJoin(
        IReadOnlyList<IDictionary<string, object?>> leftRows,
        IReadOnlyList<IDictionary<string, object?>> rightRows,
        List<JoinKeyMapping> keyMappings,
        string? rightPrefix)
    {
        var leftLookup = BuildLookup(leftRows, keyMappings.Select(k => k.LeftField).ToList());
        var allLeftKeys = CollectDistinctKeys(leftRows);
        var result = new List<IDictionary<string, object?>>(rightRows.Count);

        var rightKeys = keyMappings.Select(k => k.RightField).ToList();
        foreach (var right in rightRows)
        {
            var key = ExtractCompositeKey(right, rightKeys);
            if (key != null && leftLookup.TryGetValue(key, out var matchedLefts) && matchedLefts.Count > 0)
            {
                foreach (var left in matchedLefts)
                {
                    result.Add(MergeRow(left, right, rightPrefix));
                }
            }
            else
            {
                result.Add(MergeRightWithLeftNulls(right, allLeftKeys, rightPrefix));
            }
        }

        return result;
    }

    private static IReadOnlyList<IDictionary<string, object?>> ExecuteFullOuterJoin(
        IReadOnlyList<IDictionary<string, object?>> leftRows,
        IReadOnlyList<IDictionary<string, object?>> rightRows,
        List<JoinKeyMapping> keyMappings,
        string? rightPrefix)
    {
        var rightLookup = BuildIndexedLookup(rightRows, keyMappings.Select(k => k.RightField).ToList());
        var matchedRightIndices = new HashSet<int>();
        var allRightKeys = CollectDistinctKeys(rightRows);
        var allLeftKeys = CollectDistinctKeys(leftRows);

        var result = new List<IDictionary<string, object?>>();

        var leftKeys = keyMappings.Select(k => k.LeftField).ToList();
        foreach (var left in leftRows)
        {
            var key = ExtractCompositeKey(left, leftKeys);
            if (key != null && rightLookup.TryGetValue(key, out var matchedRights) && matchedRights.Count > 0)
            {
                foreach (var (rightIndex, right) in matchedRights)
                {
                    matchedRightIndices.Add(rightIndex);
                    result.Add(MergeRow(left, right, rightPrefix));
                }
            }
            else
            {
                result.Add(MergeRowWithNulls(left, allRightKeys, rightPrefix));
            }
        }

        for (int i = 0; i < rightRows.Count; i++)
        {
            if (!matchedRightIndices.Contains(i))
            {
                result.Add(MergeRightWithLeftNulls(rightRows[i], allLeftKeys, rightPrefix));
            }
        }

        return result;
    }

    private static Dictionary<string, List<(int Index, IDictionary<string, object?> Row)>> BuildIndexedLookup(
        IReadOnlyList<IDictionary<string, object?>> rows,
        List<string> keyFields)
    {
        var lookup = new Dictionary<string, List<(int Index, IDictionary<string, object?> Row)>>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < rows.Count; i++)
        {
            var key = ExtractCompositeKey(rows[i], keyFields);
            if (key == null) continue;

            if (!lookup.TryGetValue(key, out var list))
            {
                list = new List<(int, IDictionary<string, object?>)>();
                lookup[key] = list;
            }
            list.Add((i, rows[i]));
        }
        return lookup;
    }

    private static Dictionary<string, List<IDictionary<string, object?>>> BuildLookup(
        IReadOnlyList<IDictionary<string, object?>> rows,
        List<string> keyFields)
    {
        var lookup = new Dictionary<string, List<IDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var key = ExtractCompositeKey(row, keyFields);
            if (key == null) continue;

            if (!lookup.TryGetValue(key, out var list))
            {
                list = new List<IDictionary<string, object?>>();
                lookup[key] = list;
            }
            list.Add(row);
        }
        return lookup;
    }

    private static HashSet<string> CollectDistinctKeys(IReadOnlyList<IDictionary<string, object?>> rows)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in rows)
        {
            foreach (var k in r.Keys)
            {
                keys.Add(k);
            }
        }
        return keys;
    }

    private static string? ExtractCompositeKey(IDictionary<string, object?> row, List<string> keyFields)
    {
        var parts = new string[keyFields.Count];
        for (int i = 0; i < keyFields.Count; i++)
        {
            var field = keyFields[i];
            if (!TryGetCaseInsensitive(row, field, out var val) || val == null || val is DBNull)
            {
                return null;
            }
            parts[i] = NormalizeValueKey(val);
        }
        return string.Join("|##|", parts);
    }

    public static string NormalizeValueKey(object val)
    {
        if (val is double or float or decimal)
        {
            var d = Convert.ToDouble(val, CultureInfo.InvariantCulture);
            return d.ToString("0.######", CultureInfo.InvariantCulture);
        }
        if (val is int or long or short or byte or uint or ulong or ushort or sbyte)
        {
            return Convert.ToInt64(val, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }
        if (val is DateTime dt)
        {
            return dt.ToString("o", CultureInfo.InvariantCulture);
        }
        return val.ToString()?.Trim() ?? string.Empty;
    }

    public static bool TryGetCaseInsensitive(IDictionary<string, object?> row, string key, out object? value)
    {
        if (row.TryGetValue(key, out value)) return true;
        foreach (var kvp in row)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = kvp.Value;
                return true;
            }
        }
        value = null;
        return false;
    }

    private static IDictionary<string, object?> MergeRow(
        IDictionary<string, object?> left,
        IDictionary<string, object?> right,
        string? rightPrefix)
    {
        var merged = new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in right)
        {
            var targetKey = string.IsNullOrEmpty(rightPrefix) ? k : $"{rightPrefix}_{k}";
            if (!string.IsNullOrEmpty(rightPrefix) || !merged.ContainsKey(k))
            {
                merged[targetKey] = v;
            }
            else
            {
                merged[$"Right_{k}"] = v;
            }
        }
        return merged;
    }

    private static IDictionary<string, object?> MergeRowWithNulls(
        IDictionary<string, object?> left,
        IEnumerable<string> rightKeys,
        string? rightPrefix)
    {
        var merged = new Dictionary<string, object?>(left, StringComparer.OrdinalIgnoreCase);
        foreach (var k in rightKeys)
        {
            var targetKey = string.IsNullOrEmpty(rightPrefix) ? k : $"{rightPrefix}_{k}";
            if (!merged.ContainsKey(targetKey))
            {
                merged[targetKey] = null;
            }
        }
        return merged;
    }

    private static IDictionary<string, object?> MergeRightWithLeftNulls(
        IDictionary<string, object?> right,
        IEnumerable<string> leftKeys,
        string? rightPrefix)
    {
        var merged = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in leftKeys)
        {
            merged[k] = null;
        }
        foreach (var (k, v) in right)
        {
            var targetKey = string.IsNullOrEmpty(rightPrefix) ? k : $"{rightPrefix}_{k}";
            merged[targetKey] = v;
        }
        return merged;
    }
}
