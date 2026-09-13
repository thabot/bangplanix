using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Federation;

public static class MasterDetailCorrelator
{
    public static IReadOnlyList<IDictionary<string, object?>> Correlate(
        IReadOnlyList<IDictionary<string, object?>> parentRows,
        IReadOnlyList<IDictionary<string, object?>> childRows,
        MasterDetailRelationDefinition relation)
    {
        ArgumentNullException.ThrowIfNull(parentRows);
        ArgumentNullException.ThrowIfNull(childRows);
        ArgumentNullException.ThrowIfNull(relation);

        if (string.IsNullOrWhiteSpace(relation.ParentKey) || string.IsNullOrWhiteSpace(relation.ChildKey))
        {
            throw new ArgumentException("ParentKey and ChildKey must not be empty.", nameof(relation));
        }

        var relationName = string.IsNullOrWhiteSpace(relation.RelationName) ? "Details" : relation.RelationName;

        // Build child lookup
        var childLookup = new Dictionary<string, List<IDictionary<string, object?>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in childRows)
        {
            if (DataJoinEngine.TryGetCaseInsensitive(child, relation.ChildKey, out var keyVal) && keyVal != null && keyVal is not DBNull)
            {
                var normalizedKey = DataJoinEngine.NormalizeValueKey(keyVal);
                if (!childLookup.TryGetValue(normalizedKey, out var list))
                {
                    list = new List<IDictionary<string, object?>>();
                    childLookup[normalizedKey] = list;
                }
                list.Add(child);
            }
        }

        var correlatedResult = new List<IDictionary<string, object?>>(parentRows.Count);
        foreach (var parent in parentRows)
        {
            var correlatedRow = new Dictionary<string, object?>(parent, StringComparer.OrdinalIgnoreCase);

            if (DataJoinEngine.TryGetCaseInsensitive(parent, relation.ParentKey, out var pKeyVal) &&
                pKeyVal != null && pKeyVal is not DBNull &&
                childLookup.TryGetValue(DataJoinEngine.NormalizeValueKey(pKeyVal), out var matchedChildren))
            {
                correlatedRow[relationName] = matchedChildren;
            }
            else
            {
                correlatedRow[relationName] = Array.Empty<IDictionary<string, object?>>();
            }

            correlatedResult.Add(correlatedRow);
        }

        return correlatedResult;
    }
}
