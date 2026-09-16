using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public sealed class WatermarkDefinition
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("imageSource")]
    public string? ImageSource { get; set; }

    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 0.15f;

    [JsonPropertyName("rotationAngle")]
    public float RotationAngle { get; set; } = -45.0f;

    [JsonPropertyName("layer")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WatermarkLayer Layer { get; set; } = WatermarkLayer.Background;

    [JsonPropertyName("style")]
    public StyleDefinition? Style { get; set; }
}
