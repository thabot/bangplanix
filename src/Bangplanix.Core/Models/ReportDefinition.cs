using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public sealed class BandDefinition
{
    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("keepTogether")]
    public bool KeepTogether { get; set; }

    [JsonPropertyName("visibleCondition")]
    public string? VisibleCondition { get; set; }

    [JsonPropertyName("elements")]
    public List<ElementDefinition> Elements { get; set; } = [];
}

public sealed class GroupBandDefinition
{
    [JsonPropertyName("groupBy")]
    public string GroupBy { get; set; } = string.Empty;

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("repeatOnEveryPage")]
    public bool RepeatOnEveryPage { get; set; }

    [JsonPropertyName("keepTogether")]
    public bool KeepTogether { get; set; }

    [JsonPropertyName("elements")]
    public List<ElementDefinition> Elements { get; set; } = [];
}

public sealed class BandsDefinition
{
    [JsonPropertyName("reportHeader")]
    public BandDefinition? ReportHeader { get; set; }

    [JsonPropertyName("pageHeader")]
    public BandDefinition? PageHeader { get; set; }

    [JsonPropertyName("groupHeaders")]
    public List<GroupBandDefinition> GroupHeaders { get; set; } = [];

    [JsonPropertyName("detail")]
    public BandDefinition? Detail { get; set; }

    [JsonPropertyName("groupFooters")]
    public List<GroupBandDefinition> GroupFooters { get; set; } = [];

    [JsonPropertyName("pageFooter")]
    public BandDefinition? PageFooter { get; set; }

    [JsonPropertyName("reportFooter")]
    public BandDefinition? ReportFooter { get; set; }
}

public sealed class ReportDefinition
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("metadata")]
    public ReportMetadata Metadata { get; set; } = new();

    [JsonPropertyName("pageSetup")]
    public PageSetup PageSetup { get; set; } = new();

    [JsonPropertyName("parameters")]
    public List<ParameterDefinition> Parameters { get; set; } = [];

    [JsonPropertyName("datasets")]
    public List<DatasetDefinition> Datasets { get; set; } = [];

    [JsonPropertyName("joins")]
    public List<DataJoinDefinition> Joins { get; set; } = [];

    [JsonPropertyName("relations")]
    public List<MasterDetailRelationDefinition> Relations { get; set; } = [];

    [JsonPropertyName("styles")]
    public Dictionary<string, StyleDefinition> Styles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("bands")]
    public BandsDefinition Bands { get; set; } = new();
}
