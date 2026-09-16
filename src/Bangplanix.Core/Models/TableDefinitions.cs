using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public sealed class TablePaddingDefinition
{
    [JsonPropertyName("top")]
    public double Top { get; set; } = 2.0;

    [JsonPropertyName("bottom")]
    public double Bottom { get; set; } = 2.0;

    [JsonPropertyName("left")]
    public double Left { get; set; } = 4.0;

    [JsonPropertyName("right")]
    public double Right { get; set; } = 4.0;
}

public sealed class TableColumnDefinition
{
    [JsonPropertyName("width")]
    public string Width { get; set; } = "1*";

    [JsonPropertyName("align")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public HorizontalAlign? Align { get; set; }
}

public sealed class TableCellDefinition
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("columnSpan")]
    public int ColumnSpan { get; set; } = 1;

    [JsonPropertyName("rowSpan")]
    public int RowSpan { get; set; } = 1;

    [JsonPropertyName("style")]
    public StyleDefinition? Style { get; set; }

    [JsonPropertyName("styleRef")]
    public string? StyleRef { get; set; }

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("border")]
    public BorderDefinition? Border { get; set; }

    [JsonPropertyName("padding")]
    public TablePaddingDefinition? Padding { get; set; }
}

public sealed class TableRowDefinition
{
    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("canGrow")]
    public bool CanGrow { get; set; } = true;

    [JsonPropertyName("cells")]
    public List<TableCellDefinition> Cells { get; set; } = [];

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("style")]
    public StyleDefinition? Style { get; set; }

    [JsonPropertyName("styleRef")]
    public string? StyleRef { get; set; }
}

public sealed class TableDefinition
{
    [JsonPropertyName("columns")]
    public List<TableColumnDefinition> Columns { get; set; } = [];

    [JsonPropertyName("header")]
    public TableRowDefinition? Header { get; set; }

    [JsonPropertyName("rows")]
    public List<TableRowDefinition> Rows { get; set; } = [];

    [JsonPropertyName("footer")]
    public TableRowDefinition? Footer { get; set; }

    [JsonPropertyName("repeatHeaderOnNewPage")]
    public bool RepeatHeaderOnNewPage { get; set; } = true;

    [JsonPropertyName("canGrow")]
    public bool CanGrow { get; set; } = true;

    [JsonPropertyName("defaultRowHeight")]
    public double DefaultRowHeight { get; set; } = 20.0;

    [JsonPropertyName("datasetRef")]
    public string? DatasetRef { get; set; }

    [JsonPropertyName("border")]
    public BorderDefinition? Border { get; set; }

    [JsonPropertyName("alternatingRowBackground")]
    public string? AlternatingRowBackground { get; set; }
}
