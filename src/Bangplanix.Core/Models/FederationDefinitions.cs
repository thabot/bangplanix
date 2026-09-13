using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public enum JoinType
{
    Inner,
    LeftOuter,
    RightOuter,
    FullOuter,
    Cross
}

public enum AggregationFunction
{
    Sum,
    Avg,
    Count,
    DistinctCount,
    Min,
    Max,
    BahtText
}

public enum DatasetFallbackMode
{
    ThrowError,
    EmptyList,
    StaticFallback
}

public sealed class DatasetCachePolicy
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; } = 300;

    [JsonPropertyName("cacheKey")]
    public string? CacheKey { get; set; }
}

public sealed class DatasetFetchPolicy
{
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    [JsonPropertyName("fallbackMode")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DatasetFallbackMode FallbackMode { get; set; } = DatasetFallbackMode.ThrowError;

    [JsonPropertyName("retryCount")]
    public int RetryCount { get; set; }
}

public sealed class JoinKeyMapping
{
    [JsonPropertyName("leftField")]
    public string LeftField { get; set; } = string.Empty;

    [JsonPropertyName("rightField")]
    public string RightField { get; set; } = string.Empty;
}

public sealed class DataJoinDefinition
{
    [JsonPropertyName("leftDataset")]
    public string LeftDataset { get; set; } = string.Empty;

    [JsonPropertyName("rightDataset")]
    public string RightDataset { get; set; } = string.Empty;

    [JsonPropertyName("joinType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JoinType JoinType { get; set; } = JoinType.LeftOuter;

    [JsonPropertyName("keyMappings")]
    public List<JoinKeyMapping> KeyMappings { get; set; } = [];

    [JsonPropertyName("rightPrefix")]
    public string? RightPrefix { get; set; }

    [JsonPropertyName("outputDatasetName")]
    public string? OutputDatasetName { get; set; }
}

public sealed class MasterDetailRelationDefinition
{
    [JsonPropertyName("relationName")]
    public string RelationName { get; set; } = "Details";

    [JsonPropertyName("parentDataset")]
    public string ParentDataset { get; set; } = string.Empty;

    [JsonPropertyName("childDataset")]
    public string ChildDataset { get; set; } = string.Empty;

    [JsonPropertyName("parentKey")]
    public string ParentKey { get; set; } = string.Empty;

    [JsonPropertyName("childKey")]
    public string ChildKey { get; set; } = string.Empty;
}

public sealed class AggregateFieldDefinition
{
    [JsonPropertyName("sourceField")]
    public string SourceField { get; set; } = string.Empty;

    [JsonPropertyName("targetField")]
    public string TargetField { get; set; } = string.Empty;

    [JsonPropertyName("function")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AggregationFunction Function { get; set; } = AggregationFunction.Sum;
}

public sealed class DatasetAggregationDefinition
{
    [JsonPropertyName("groupByFields")]
    public List<string> GroupByFields { get; set; } = [];

    [JsonPropertyName("aggregations")]
    public List<AggregateFieldDefinition> Aggregations { get; set; } = [];

    [JsonPropertyName("enableRollup")]
    public bool EnableRollup { get; set; }

    [JsonPropertyName("includeGrandTotal")]
    public bool IncludeGrandTotal { get; set; } = true;
}

public sealed class CalculatedColumnDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;
}
