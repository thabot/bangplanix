using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public sealed class ChartDataPoint
{
    [JsonPropertyName("x")]
    public object? X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double? Z { get; set; } // For Bubble radius / 3rd dimension

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("isTotal")]
    public bool IsTotal { get; set; }

    [JsonPropertyName("isSubtotal")]
    public bool IsSubtotal { get; set; }
}

public sealed class ChartDataBindingDefinition
{
    [JsonPropertyName("datasetRef")]
    public string? DatasetRef { get; set; }

    [JsonPropertyName("categoryField")]
    public string? CategoryField { get; set; }

    [JsonPropertyName("valueField")]
    public string? ValueField { get; set; }

    [JsonPropertyName("seriesGroupField")]
    public string? SeriesGroupField { get; set; }

    [JsonPropertyName("aggregateFunction")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChartAggregateFunction AggregateFunction { get; set; } = ChartAggregateFunction.Sum;
}

public sealed class ChartSeriesDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("seriesType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChartType? SeriesType { get; set; }

    [JsonPropertyName("axisTarget")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AxisTarget AxisTarget { get; set; } = AxisTarget.Primary;

    [JsonPropertyName("values")]
    public List<double> Values { get; set; } = [];

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    [JsonPropertyName("dataPoints")]
    public List<ChartDataPoint> DataPoints { get; set; } = [];

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("borderColor")]
    public string? BorderColor { get; set; }

    [JsonPropertyName("borderWidth")]
    public float BorderWidth { get; set; } = 1.5f;

    [JsonPropertyName("showDataLabels")]
    public bool ShowDataLabels { get; set; }

    [JsonPropertyName("dataLabelFormat")]
    public string? DataLabelFormat { get; set; }

    [JsonPropertyName("valueExpression")]
    public string? ValueExpression { get; set; }

    [JsonPropertyName("categoryExpression")]
    public string? CategoryExpression { get; set; }
}

public sealed class ChartAxisDefinition
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("showGridLines")]
    public bool ShowGridLines { get; set; } = true;

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    [JsonPropertyName("interval")]
    public double? Interval { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = [];

    [JsonPropertyName("rotation")]
    public float Rotation { get; set; }
}

public sealed class WaterfallOptions
{
    [JsonPropertyName("positiveColor")]
    public string PositiveColor { get; set; } = "#10b981"; // Emerald green

    [JsonPropertyName("negativeColor")]
    public string NegativeColor { get; set; } = "#ef4444"; // Red

    [JsonPropertyName("totalColor")]
    public string TotalColor { get; set; } = "#3b82f6"; // Blue

    [JsonPropertyName("connectorColor")]
    public string ConnectorColor { get; set; } = "#94a3b8"; // Slate gray

    [JsonPropertyName("showConnectorLines")]
    public bool ShowConnectorLines { get; set; } = true;
}

public sealed class FunnelOptions
{
    [JsonPropertyName("neckWidthRatio")]
    public float NeckWidthRatio { get; set; } = 0.3f;

    [JsonPropertyName("showConversionRates")]
    public bool ShowConversionRates { get; set; } = true;
}

public sealed class GaugeRange
{
    [JsonPropertyName("start")]
    public double Start { get; set; }

    [JsonPropertyName("end")]
    public double End { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#22c55e"; // Default green
}

public sealed class GaugeOptions
{
    [JsonPropertyName("minValue")]
    public double MinValue { get; set; } = 0.0;

    [JsonPropertyName("maxValue")]
    public double MaxValue { get; set; } = 100.0;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("targetValue")]
    public double? TargetValue { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("ranges")]
    public List<GaugeRange> Ranges { get; set; } = [];
}

public sealed class BulletOptions
{
    [JsonPropertyName("actual")]
    public double Actual { get; set; }

    [JsonPropertyName("target")]
    public double Target { get; set; }

    [JsonPropertyName("badRange")]
    public double BadRange { get; set; } = 60.0;

    [JsonPropertyName("satisfactoryRange")]
    public double SatisfactoryRange { get; set; } = 85.0;

    [JsonPropertyName("goodRange")]
    public double GoodRange { get; set; } = 100.0;

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }
}

public sealed class ChartDefinition
{
    [JsonPropertyName("chartType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChartType ChartType { get; set; } = ChartType.Column;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("palette")]
    public string? Palette { get; set; } = "Default";

    [JsonPropertyName("customColors")]
    public List<string> CustomColors { get; set; } = [];

    [JsonPropertyName("dataBinding")]
    public ChartDataBindingDefinition? DataBinding { get; set; }

    [JsonPropertyName("xAxis")]
    public ChartAxisDefinition? XAxis { get; set; }

    [JsonPropertyName("yAxis")]
    public ChartAxisDefinition? YAxis { get; set; }

    [JsonPropertyName("secondaryYAxis")]
    public ChartAxisDefinition? SecondaryYAxis { get; set; }

    [JsonPropertyName("series")]
    public List<ChartSeriesDefinition> Series { get; set; } = [];

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    [JsonPropertyName("showLegend")]
    public bool ShowLegend { get; set; } = true;

    [JsonPropertyName("legendPosition")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LegendPosition LegendPosition { get; set; } = LegendPosition.Bottom;

    [JsonPropertyName("donutHoleSize")]
    public float DonutHoleSize { get; set; } = 0.5f;

    [JsonPropertyName("explodedSliceIndex")]
    public int? ExplodedSliceIndex { get; set; }

    [JsonPropertyName("gaugeOptions")]
    public GaugeOptions? GaugeOptions { get; set; }

    [JsonPropertyName("bulletOptions")]
    public BulletOptions? BulletOptions { get; set; }

    [JsonPropertyName("waterfallOptions")]
    public WaterfallOptions? WaterfallOptions { get; set; }

    [JsonPropertyName("funnelOptions")]
    public FunnelOptions? FunnelOptions { get; set; }

    [JsonPropertyName("showTrendline")]
    public bool ShowTrendline { get; set; }
}

public sealed class SparklineDefinition
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SparklineType Type { get; set; } = SparklineType.Line;

    [JsonPropertyName("values")]
    public List<double> Values { get; set; } = [];

    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("lineColor")]
    public string? LineColor { get; set; } = "#2563eb";

    [JsonPropertyName("fillColor")]
    public string? FillColor { get; set; } = "#93c5fd33";

    [JsonPropertyName("highlightMinMax")]
    public bool HighlightMinMax { get; set; } = true;

    [JsonPropertyName("minColor")]
    public string? MinColor { get; set; } = "#ef4444";

    [JsonPropertyName("maxColor")]
    public string? MaxColor { get; set; } = "#10b981";

    [JsonPropertyName("lastColor")]
    public string? LastColor { get; set; } = "#f59e0b";
}
