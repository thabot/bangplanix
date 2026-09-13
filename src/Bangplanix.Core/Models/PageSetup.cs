using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public sealed class ReportMetadata
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public sealed class MarginDefinition
{
    [JsonPropertyName("top")]
    public double Top { get; set; } = 10.0;

    [JsonPropertyName("bottom")]
    public double Bottom { get; set; } = 10.0;

    [JsonPropertyName("left")]
    public double Left { get; set; } = 10.0;

    [JsonPropertyName("right")]
    public double Right { get; set; } = 10.0;
}

public sealed class PageSetup
{
    [JsonPropertyName("paperKind")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaperKind PaperKind { get; set; } = PaperKind.A4;

    [JsonPropertyName("orientation")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    [JsonPropertyName("width")]
    public double Width { get; set; } = 210.0;

    [JsonPropertyName("height")]
    public double Height { get; set; } = 297.0;

    [JsonPropertyName("unit")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UnitType Unit { get; set; } = UnitType.Mm;

    [JsonPropertyName("margins")]
    public MarginDefinition Margins { get; set; } = new();

    public (double WidthMm, double HeightMm) GetEffectiveDimensionsMm()
    {
        double w = Unit switch
        {
            UnitType.Mm => Width,
            UnitType.Cm => Width * 10.0,
            UnitType.In => Width * 25.4,
            UnitType.Pt => Width * 0.352778,
            UnitType.Px => Width * 0.264583,
            _ => Width
        };

        double h = Unit switch
        {
            UnitType.Mm => Height,
            UnitType.Cm => Height * 10.0,
            UnitType.In => Height * 25.4,
            UnitType.Pt => Height * 0.352778,
            UnitType.Px => Height * 0.264583,
            _ => Height
        };

        return Orientation == PageOrientation.Landscape ? (Math.Max(w, h), Math.Min(w, h)) : (Math.Min(w, h), Math.Max(w, h));
    }
}
