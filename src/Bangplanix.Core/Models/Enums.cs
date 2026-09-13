namespace Bangplanix.Core.Models;

public enum PaperKind
{
    A4,
    A3,
    A5,
    Letter,
    Legal,
    Custom
}

public enum PageOrientation
{
    Portrait,
    Landscape
}

public enum UnitType
{
    Mm,
    Cm,
    In,
    Pt,
    Px
}

public enum ElementType
{
    Text,
    Image,
    Barcode,
    QrCode,
    Shape,
    Table,
    Container,
    Chart,
    Sparkline
}

public enum ChartType
{
    Column,
    Bar,
    StackedColumn,
    StackedBar,
    PercentStackedColumn,
    PercentStackedBar,
    Line,
    Spline,
    StepLine,
    Area,
    SplineArea,
    StackedArea,
    Pie,
    Doughnut,
    Gauge,
    RadialSpeedometer,
    Bullet,
    Scatter,
    Bubble,
    Radar,
    Combo,
    Waterfall,
    Funnel
}

public enum AxisTarget
{
    Primary,
    Secondary
}

public enum ChartAggregateFunction
{
    Sum,
    Average,
    Count,
    Min,
    Max
}

public enum LegendPosition
{
    None,
    Top,
    Bottom,
    Left,
    Right
}

public enum SparklineType
{
    Line,
    Bar,
    Area,
    WinLoss
}

public enum BarcodeType
{
    Code128,
    GS1_128,
    Code39,
    EAN13,
    EAN8,
    UPCA,
    ITF14
}

public enum QrEccLevel
{
    L,
    M,
    Q,
    H
}

public enum ShapeType
{
    Rectangle,
    RoundedRect,
    Line,
    Ellipse
}

public enum HorizontalAlign
{
    Left,
    Center,
    Right,
    Justify
}

public enum VerticalAlign
{
    Top,
    Middle,
    Bottom
}

public enum ParameterType
{
    String,
    Number,
    Boolean,
    DateTime,
    List
}

public enum DatasetType
{
    Sql,
    Rest,
    Json,
    Static,
    ClickHouse,
    Snowflake,
    BigQuery,
    SapRaylight
}
