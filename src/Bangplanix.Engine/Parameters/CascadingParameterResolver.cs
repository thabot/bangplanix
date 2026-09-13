using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Parameters;

public static class CascadingParameterResolver
{
    public static IReadOnlyList<ParameterDefinition> GetExecutionOrder(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (report.Parameters.Count <= 1)
        {
            return report.Parameters;
        }

        var list = new List<ParameterDefinition>();
        var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // First add all root parameters (no cascading parent)
        foreach (var p in report.Parameters.Where(p => string.IsNullOrEmpty(p.CascadingParent)))
        {
            list.Add(p);
            added.Add(p.Name);
        }

        // Then add dependent children iteratively
        bool progressed = true;
        while (progressed && list.Count < report.Parameters.Count)
        {
            progressed = false;
            foreach (var p in report.Parameters.Where(p => !added.Contains(p.Name)))
            {
                if (added.Contains(p.CascadingParent!))
                {
                    list.Add(p);
                    added.Add(p.Name);
                    progressed = true;
                }
            }
        }

        // Add any remaining (fallback)
        foreach (var p in report.Parameters.Where(p => !added.Contains(p.Name)))
        {
            list.Add(p);
        }

        return list;
    }

    public static IReadOnlyList<ParameterOption> FilterOptionsByParent(
        ParameterDefinition childDef,
        object? parentValue,
        IEnumerable<IDictionary<string, object?>>? datasetRows = null)
    {
        ArgumentNullException.ThrowIfNull(childDef);

        if (parentValue == null)
        {
            return [];
        }

        var parentStr = parentValue.ToString()?.Trim() ?? string.Empty;

        // 1. Dynamic Dataset Rows Filter
        if (datasetRows != null && !string.IsNullOrEmpty(childDef.ValueField))
        {
            var result = new List<ParameterOption>();
            var valField = childDef.ValueField;
            var labelField = childDef.LabelField ?? valField;
            var parentField = childDef.CascadingParent;

            foreach (var row in datasetRows)
            {
                if (parentField != null && row.TryGetValue(parentField, out var rowParentVal))
                {
                    if (string.Equals(rowParentVal?.ToString()?.Trim(), parentStr, StringComparison.OrdinalIgnoreCase))
                    {
                        var val = row.TryGetValue(valField, out var v) ? v : null;
                        var label = row.TryGetValue(labelField, out var l) ? l?.ToString() ?? "" : val?.ToString() ?? "";
                        result.Add(new ParameterOption { Label = label, Value = val });
                    }
                }
            }
            return result;
        }

        // 2. Static AvailableValues filter (Value or Label prefix matching)
        if (childDef.AvailableValues.Count > 0)
        {
            return childDef.AvailableValues
                .Where(opt => opt.Value?.ToString()?.StartsWith(parentStr, StringComparison.OrdinalIgnoreCase) == true ||
                              opt.Label.StartsWith(parentStr, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return childDef.AvailableValues;
    }
}