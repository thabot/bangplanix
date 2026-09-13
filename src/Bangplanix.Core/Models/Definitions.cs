using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public sealed class ParameterOption
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

public sealed class ParameterDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ParameterType Type { get; set; } = ParameterType.String;

    [JsonPropertyName("defaultValue")]
    public object? DefaultValue { get; set; }

    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; }

    [JsonPropertyName("multiSelect")]
    public bool MultiSelect { get; set; }

    [JsonPropertyName("cascadingParent")]
    public string? CascadingParent { get; set; }

    [JsonPropertyName("datasetRef")]
    public string? DatasetRef { get; set; }

    [JsonPropertyName("valueField")]
    public string? ValueField { get; set; }

    [JsonPropertyName("labelField")]
    public string? LabelField { get; set; }

    [JsonPropertyName("validationPattern")]
    public string? ValidationPattern { get; set; }

    [JsonPropertyName("minValue")]
    public object? MinValue { get; set; }

    [JsonPropertyName("maxValue")]
    public object? MaxValue { get; set; }

    [JsonPropertyName("availableValues")]
    public List<ParameterOption> AvailableValues { get; set; } = [];
}

public sealed class DatasetDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DatasetType Type { get; set; } = DatasetType.Static;

    [JsonPropertyName("connectionRef")]
    public string? ConnectionRef { get; set; }

    [JsonPropertyName("queryOrUrl")]
    public string? QueryOrUrl { get; set; }

    [JsonPropertyName("staticData")]
    public object? StaticData { get; set; }

    [JsonPropertyName("headers")]
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("cachePolicy")]
    public DatasetCachePolicy? CachePolicy { get; set; }

    [JsonPropertyName("fetchPolicy")]
    public DatasetFetchPolicy? FetchPolicy { get; set; }

    [JsonPropertyName("calculatedColumns")]
    public List<CalculatedColumnDefinition> CalculatedColumns { get; set; } = [];

    [JsonPropertyName("aggregation")]
    public DatasetAggregationDefinition? Aggregation { get; set; }
}

public sealed class BorderDefinition
{
    [JsonPropertyName("top")]
    public string? Top { get; set; }

    [JsonPropertyName("bottom")]
    public string? Bottom { get; set; }

    [JsonPropertyName("left")]
    public string? Left { get; set; }

    [JsonPropertyName("right")]
    public string? Right { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; } = 1.0;
}

public sealed class StyleDefinition
{
    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; } = "Sarabun";

    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 10.0;

    [JsonPropertyName("fontWeight")]
    public string? FontWeight { get; set; } = "Normal";

    [JsonPropertyName("fontStyle")]
    public string? FontStyle { get; set; } = "Normal";

    [JsonPropertyName("color")]
    public string? Color { get; set; } = "#000000";

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("align")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HorizontalAlign Align { get; set; } = HorizontalAlign.Left;

    [JsonPropertyName("verticalAlign")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VerticalAlign VerticalAlign { get; set; } = VerticalAlign.Top;

    [JsonPropertyName("border")]
    public BorderDefinition? Border { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }
}

public sealed class ElementDefinition
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ElementType Type { get; set; } = ElementType.Text;

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("style")]
    public StyleDefinition? Style { get; set; }

    [JsonPropertyName("styleRef")]
    public string? StyleRef { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("imageSource")]
    public string? ImageSource { get; set; }

    [JsonPropertyName("barcodeType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BarcodeType? BarcodeType { get; set; }

    [JsonPropertyName("showBarcodeText")]
    public bool? ShowBarcodeText { get; set; } = true;

    [JsonPropertyName("barcodeTextSize")]
    public double? BarcodeTextSize { get; set; }

    [JsonPropertyName("imageQuality")]
    public int? ImageQuality { get; set; }

    [JsonPropertyName("maxDpi")]
    public int? MaxDpi { get; set; }

    [JsonPropertyName("qrEccLevel")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QrEccLevel? QrEccLevel { get; set; }

    [JsonPropertyName("shapeType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShapeType? ShapeType { get; set; }

    [JsonPropertyName("cornerRadius")]
    public double CornerRadius { get; set; }

    [JsonPropertyName("chart")]
    public ChartDefinition? Chart { get; set; }

    [JsonPropertyName("sparkline")]
    public SparklineDefinition? Sparkline { get; set; }
}
