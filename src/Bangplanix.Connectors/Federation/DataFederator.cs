using Bangplanix.Core.Models;

namespace Bangplanix.Connectors.Federation;

public sealed class FederatedDataResult
{
    public Dictionary<string, IReadOnlyList<IDictionary<string, object?>>> Datasets { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, IReadOnlyList<IDictionary<string, object?>>> MasterDetails { get; } = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<IDictionary<string, object?>>? PrimaryDataset =>
        Datasets.Values.FirstOrDefault();
}

public sealed class DataFederator
{
    private readonly Func<DatasetType, IDataConnector> _connectorFactory;
    private readonly DatasetMemoryCache _cache;

    public DataFederator(
        Func<DatasetType, IDataConnector>? connectorFactory = null,
        DatasetMemoryCache? cache = null)
    {
        _connectorFactory = connectorFactory ?? (type => DataConnectorFactory.CreateConnector(type));
        _cache = cache ?? DatasetMemoryCache.Default;
    }

    public async Task<FederatedDataResult> FederateAsync(
        ReportDefinition report,
        IDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var result = new FederatedDataResult();

        // 1. Fetch all raw datasets concurrently with caching, timeout, and templating
        var fetchTasks = report.Datasets.Select(ds => FetchDatasetWithPolicyAsync(ds, parameters, cancellationToken)).ToList();

        var fetchedResults = await Task.WhenAll(fetchTasks);
        foreach (var (name, rows) in fetchedResults)
        {
            result.Datasets[name] = rows;
        }

        // 2. Execute Data Joins in sequence
        if (report.Joins != null && report.Joins.Count > 0)
        {
            foreach (var join in report.Joins)
            {
                if (!result.Datasets.TryGetValue(join.LeftDataset, out var leftRows))
                {
                    leftRows = Array.Empty<IDictionary<string, object?>>();
                }

                if (!result.Datasets.TryGetValue(join.RightDataset, out var rightRows))
                {
                    rightRows = Array.Empty<IDictionary<string, object?>>();
                }

                var joinedRows = DataJoinEngine.ExecuteJoin(leftRows, rightRows, join);
                var outputName = !string.IsNullOrWhiteSpace(join.OutputDatasetName)
                    ? join.OutputDatasetName
                    : $"{join.LeftDataset}_{join.RightDataset}_Joined";

                result.Datasets[outputName] = joinedRows;
            }
        }

        // 3. Establish Master-Detail relations
        if (report.Relations != null && report.Relations.Count > 0)
        {
            foreach (var rel in report.Relations)
            {
                if (result.Datasets.TryGetValue(rel.ParentDataset, out var parentRows) &&
                    result.Datasets.TryGetValue(rel.ChildDataset, out var childRows))
                {
                    var correlated = MasterDetailCorrelator.Correlate(parentRows, childRows, rel);
                    var relationKey = string.IsNullOrWhiteSpace(rel.RelationName)
                        ? $"{rel.ParentDataset}_{rel.ChildDataset}"
                        : rel.RelationName;

                    result.MasterDetails[relationKey] = correlated;
                }
            }
        }

        return result;
    }

    private async Task<(string Name, IReadOnlyList<IDictionary<string, object?>> Rows)> FetchDatasetWithPolicyAsync(
        DatasetDefinition ds,
        IDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        // 1. Resolve template variables in QueryOrUrl and Headers
        var resolvedDataset = CloneWithResolvedTemplates(ds, parameters);

        // 2. Check Cache Policy
        if (resolvedDataset.CachePolicy != null && resolvedDataset.CachePolicy.Enabled)
        {
            var cacheKey = DatasetMemoryCache.GenerateCacheKey(resolvedDataset, parameters);
            var duration = TimeSpan.FromSeconds(Math.Max(1, resolvedDataset.CachePolicy.DurationSeconds));

            var cachedRows = await _cache.GetOrCreateAsync(
                cacheKey,
                duration,
                () => ExecuteFetchWithResilienceAsync(resolvedDataset, parameters, cancellationToken));

            return (resolvedDataset.Name, cachedRows);
        }

        var rows = await ExecuteFetchWithResilienceAsync(resolvedDataset, parameters, cancellationToken);
        return (resolvedDataset.Name, rows);
    }

    private async Task<IReadOnlyList<IDictionary<string, object?>>> ExecuteFetchWithResilienceAsync(
        DatasetDefinition ds,
        IDictionary<string, object?>? parameters,
        CancellationToken cancellationToken)
    {
        var fetchPolicy = ds.FetchPolicy ?? new DatasetFetchPolicy();
        var timeout = TimeSpan.FromSeconds(Math.Max(1, fetchPolicy.TimeoutSeconds));

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var connector = _connectorFactory(ds.Type);
            var rows = await connector.FetchDataAsync(ds, parameters, linkedCts.Token);

            // Apply calculated columns if defined on dataset
            if (ds.CalculatedColumns.Count > 0)
            {
                rows = DatasetAggregator.EvaluateCalculatedColumns(rows, ds.CalculatedColumns);
            }

            // Apply aggregation if defined on dataset
            if (ds.Aggregation != null)
            {
                rows = DatasetAggregator.Aggregate(rows, ds.Aggregation);
            }

            return rows;
        }
        catch (Exception) when (fetchPolicy.FallbackMode != DatasetFallbackMode.ThrowError)
        {
            if (fetchPolicy.FallbackMode == DatasetFallbackMode.StaticFallback && ds.StaticData != null)
            {
                var staticConnector = DataConnectorFactory.CreateConnector(DatasetType.Static);
                return await staticConnector.FetchDataAsync(ds, parameters, cancellationToken);
            }

            return Array.Empty<IDictionary<string, object?>>();
        }
    }

    private static DatasetDefinition CloneWithResolvedTemplates(
        DatasetDefinition ds,
        IDictionary<string, object?>? parameters)
    {
        var cloned = new DatasetDefinition
        {
            Name = ds.Name,
            Type = ds.Type,
            ConnectionRef = ParameterTemplateResolver.Resolve(ds.ConnectionRef, parameters, urlEncode: false),
            QueryOrUrl = ParameterTemplateResolver.Resolve(ds.QueryOrUrl, parameters, urlEncode: false),
            StaticData = ds.StaticData,
            CachePolicy = ds.CachePolicy,
            FetchPolicy = ds.FetchPolicy,
            CalculatedColumns = ds.CalculatedColumns,
            Aggregation = ds.Aggregation,
            Headers = ParameterTemplateResolver.ResolveHeaders(ds.Headers, parameters)
        };

        return cloned;
    }
}
