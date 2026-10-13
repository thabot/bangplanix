using Bangplanix.Core.Formatting;
using Bangplanix.Core.Models;
using Bangplanix.Core.Payments;
using Bangplanix.Engine.Canvas;

namespace Bangplanix.Fluent;

internal interface ILayoutNode
{
    (List<ElementDefinition> Elements, double UsedHeight) Compile(
        double startX,
        double startY,
        double availableWidth,
        UnitType unit,
        StyleDefinition defaultStyle);
}

internal sealed class ElementNode : ILayoutNode
{
    private readonly ElementDefinition _element;
    public ElementDefinition Element => _element;

    public ElementNode(ElementDefinition element)
    {
        _element = element;
    }

    public (List<ElementDefinition> Elements, double UsedHeight) Compile(
        double startX,
        double startY,
        double availableWidth,
        UnitType unit,
        StyleDefinition defaultStyle)
    {
        _element.X = startX + _element.X;
        _element.Y = startY + _element.Y;
        if (_element.Width <= 0)
        {
            _element.Width = availableWidth;
        }

        if (_element.Style == null)
        {
            _element.Style = new StyleDefinition
            {
                FontFamily = defaultStyle.FontFamily,
                FontSize = defaultStyle.FontSize,
                Color = defaultStyle.Color,
                FontWeight = defaultStyle.FontWeight,
                FontStyle = defaultStyle.FontStyle
            };
        }

        return ([_element], Math.Max(_element.Height, 5.0));
    }
}

internal sealed class ColumnNode : ILayoutNode
{
    public List<Container> Items { get; } = [];
    public double Spacing { get; set; }

    public (List<ElementDefinition> Elements, double UsedHeight) Compile(
        double startX,
        double startY,
        double availableWidth,
        UnitType unit,
        StyleDefinition defaultStyle)
    {
        var elements = new List<ElementDefinition>();
        double currentY = startY;

        for (int i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var (itemElements, itemHeight) = item.Compile(startX, currentY, availableWidth, unit, defaultStyle);
            elements.AddRange(itemElements);
            currentY += itemHeight;

            if (i < Items.Count - 1 && Spacing > 0)
            {
                currentY += Spacing;
            }
        }

        return (elements, currentY - startY);
    }
}

internal sealed class RowItem
{
    public bool IsConstant { get; init; }
    public double Value { get; init; } // Width in Unit for Constant, Weight for Relative
    public Container Container { get; init; } = new();
}

internal sealed class RowNode : ILayoutNode
{
    public List<RowItem> Items { get; } = [];
    public double Spacing { get; set; }

    public (List<ElementDefinition> Elements, double UsedHeight) Compile(
        double startX,
        double startY,
        double availableWidth,
        UnitType unit,
        StyleDefinition defaultStyle)
    {
        var elements = new List<ElementDefinition>();
        if (Items.Count == 0)
        {
            return (elements, 0);
        }

        double totalSpacing = (Items.Count - 1) * Spacing;
        double totalConstantWidth = Items.Where(x => x.IsConstant).Sum(x => x.Value);
        double remainingWidth = Math.Max(0, availableWidth - totalConstantWidth - totalSpacing);
        double totalRelativeWeight = Items.Where(x => !x.IsConstant).Sum(x => x.Value);
        if (totalRelativeWeight <= 0) totalRelativeWeight = 1.0;

        double currentX = startX;
        double maxItemHeight = 0;

        foreach (var item in Items)
        {
            double itemWidth = item.IsConstant
                ? item.Value
                : (item.Value / totalRelativeWeight) * remainingWidth;

            var (itemElements, itemHeight) = item.Container.Compile(currentX, startY, itemWidth, unit, defaultStyle);
            elements.AddRange(itemElements);
            if (itemHeight > maxItemHeight)
            {
                maxItemHeight = itemHeight;
            }

            currentX += itemWidth + Spacing;
        }

        return (elements, maxItemHeight);
    }
}

internal sealed class PaddingNode : ILayoutNode
{
    public double Top { get; set; }
    public double Bottom { get; set; }
    public double Left { get; set; }
    public double Right { get; set; }
    public Container Child { get; set; } = new();

    public (List<ElementDefinition> Elements, double UsedHeight) Compile(
        double startX,
        double startY,
        double availableWidth,
        UnitType unit,
        StyleDefinition defaultStyle)
    {
        double innerX = startX + Left;
        double innerY = startY + Top;
        double innerWidth = Math.Max(0, availableWidth - Left - Right);

        var (elements, childHeight) = Child.Compile(innerX, innerY, innerWidth, unit, defaultStyle);
        return (elements, childHeight + Top + Bottom);
    }
}

public sealed class Container : IContainer
{
    internal List<ILayoutNode> Nodes { get; } = [];
    private StyleDefinition? _appliedStyle;
    private HorizontalAlign _horizontalAlign = HorizontalAlign.Left;

    public IContainer Column(Action<IColumnBuilder> columnConfig)
    {
        var builder = new ColumnBuilder();
        columnConfig(builder);
        Nodes.Add(builder.Node);
        return this;
    }

    public IContainer Row(Action<IRowBuilder> rowConfig)
    {
        var builder = new RowBuilder();
        rowConfig(builder);
        Nodes.Add(builder.Node);
        return this;
    }

    public IContainer Table(Action<ITableBuilder> tableConfig)
    {
        var builder = new TableBuilder();
        tableConfig(builder);
        Nodes.Add(builder.ToLayoutNode());
        return this;
    }

    public IContainer Text(string text)
    {
        var elem = new ElementDefinition
        {
            Type = ElementType.Text,
            Text = text,
            CanGrow = true,
            Height = 6.0,
            Style = new StyleDefinition
            {
                Align = _horizontalAlign
            }
        };
        Nodes.Add(new ElementNode(elem));
        return this;
    }

    public IContainer Text(Action<ITextBuilder> textConfig)
    {
        var builder = new TextBuilder();
        textConfig(builder);
        var elem = builder.Build();
        if (_horizontalAlign != HorizontalAlign.Left && elem.Style != null)
        {
            elem.Style.Align = _horizontalAlign;
        }
        Nodes.Add(new ElementNode(elem));
        return this;
    }

    public IContainer Image(string source, double width = 0, double height = 0)
    {
        var elem = new ElementDefinition
        {
            Type = ElementType.Image,
            ImageSource = source,
            Width = width,
            Height = height > 0 ? height : 30.0
        };
        Nodes.Add(new ElementNode(elem));
        return this;
    }

    public IContainer Barcode(string value, BarcodeType type = BarcodeType.Code128, double width = 100, double height = 40)
    {
        var elem = new ElementDefinition
        {
            Type = ElementType.Barcode,
            Text = value,
            BarcodeType = type,
            Width = width,
            Height = height
        };
        Nodes.Add(new ElementNode(elem));
        return this;
    }

    public IContainer QrCode(string value, double size = 50)
    {
        var elem = new ElementDefinition
        {
            Type = ElementType.QrCode,
            Text = value,
            Width = size,
            Height = size
        };
        Nodes.Add(new ElementNode(elem));
        return this;
    }

    public IContainer QrPromptPay(string targetId, double amount, double size = 80)
    {
        var payload = PromptPayQrGenerator.GeneratePromptPayPayload(targetId, (decimal)amount);
        var elem = new ElementDefinition
        {
            Type = ElementType.QrCode,
            Text = payload,
            Width = size,
            Height = size
        };
        Nodes.Add(new ElementNode(elem));
        return this;
    }

    public IContainer Component(IReportComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        component.Compose(this);
        return this;
    }

    public IContainer Element(Action<IContainer> elementConfig)
    {
        ArgumentNullException.ThrowIfNull(elementConfig);
        elementConfig(this);
        return this;
    }

    public IContainer Padding(double all, UnitType unit = UnitType.Mm)
    {
        return Padding(all, all, all, all, unit);
    }

    public IContainer Padding(double top, double bottom, double left, double right, UnitType unit = UnitType.Mm)
    {
        var padNode = new PaddingNode
        {
            Top = top,
            Bottom = bottom,
            Left = left,
            Right = right,
            Child = new Container()
        };
        Nodes.Add(padNode);
        return padNode.Child;
    }

    public IContainer PaddingVertical(double padding, UnitType unit = UnitType.Mm)
    {
        return Padding(padding, padding, 0, 0, unit);
    }

    public IContainer PaddingHorizontal(double padding, UnitType unit = UnitType.Mm)
    {
        return Padding(0, 0, padding, padding, unit);
    }

    public IContainer PaddingTop(double pt, UnitType unit = UnitType.Mm)
    {
        return Padding(pt, 0, 0, 0, unit);
    }

    public IContainer PaddingBottom(double pt, UnitType unit = UnitType.Mm)
    {
        return Padding(0, pt, 0, 0, unit);
    }

    public IContainer PaddingLeft(double pt, UnitType unit = UnitType.Mm)
    {
        return Padding(0, 0, pt, 0, unit);
    }

    public IContainer PaddingRight(double pt, UnitType unit = UnitType.Mm)
    {
        return Padding(0, 0, 0, pt, unit);
    }

    public IContainer AlignLeft()
    {
        _horizontalAlign = HorizontalAlign.Left;
        return this;
    }

    public IContainer AlignCenter()
    {
        _horizontalAlign = HorizontalAlign.Center;
        return this;
    }

    public IContainer AlignRight()
    {
        _horizontalAlign = HorizontalAlign.Right;
        return this;
    }

    public IContainer BackgroundColor(string hexColor)
    {
        _appliedStyle ??= new StyleDefinition();
        _appliedStyle.BackgroundColor = hexColor;
        return this;
    }

    public IContainer Bold(bool bold = true)
    {
        var last = Nodes.OfType<ElementNode>().LastOrDefault();
        if (last != null)
        {
            last.Element.Style ??= new StyleDefinition();
            last.Element.Style.FontWeight = bold ? "Bold" : "Normal";
        }
        return this;
    }

    public IContainer SemiBold()
    {
        var last = Nodes.OfType<ElementNode>().LastOrDefault();
        if (last != null)
        {
            last.Element.Style ??= new StyleDefinition();
            last.Element.Style.FontWeight = "SemiBold";
        }
        return this;
    }

    public IContainer Italic(bool italic = true)
    {
        var last = Nodes.OfType<ElementNode>().LastOrDefault();
        if (last != null)
        {
            last.Element.Style ??= new StyleDefinition();
            last.Element.Style.FontStyle = italic ? "Italic" : "Normal";
        }
        return this;
    }

    public IContainer FontSize(double size)
    {
        var last = Nodes.OfType<ElementNode>().LastOrDefault();
        if (last != null)
        {
            last.Element.Style ??= new StyleDefinition();
            last.Element.Style.FontSize = size;
        }
        return this;
    }

    public IContainer FontColor(string hexColor) => Color(hexColor);

    public IContainer Color(string hexColor)
    {
        var last = Nodes.OfType<ElementNode>().LastOrDefault();
        if (last != null)
        {
            last.Element.Style ??= new StyleDefinition();
            last.Element.Style.Color = hexColor;
        }
        return this;
    }

    public IContainer FontFamily(string family)
    {
        var last = Nodes.OfType<ElementNode>().LastOrDefault();
        if (last != null)
        {
            last.Element.Style ??= new StyleDefinition();
            last.Element.Style.FontFamily = family;
        }
        return this;
    }

    public IContainer Width(double width, UnitType unit = UnitType.Mm)
    {
        return this;
    }

    public IContainer Height(double height, UnitType unit = UnitType.Mm)
    {
        return this;
    }

    internal (List<ElementDefinition> Elements, double UsedHeight) Compile(
        double startX,
        double startY,
        double availableWidth,
        UnitType unit,
        StyleDefinition defaultStyle)
    {
        var elements = new List<ElementDefinition>();
        double currentY = startY;

        foreach (var node in Nodes)
        {
            var (nodeElements, nodeHeight) = node.Compile(startX, currentY, availableWidth, unit, defaultStyle);
            elements.AddRange(nodeElements);
            currentY += nodeHeight;
        }

        return (elements, currentY - startY);
    }
}

public sealed class ColumnBuilder : IColumnBuilder
{
    internal ColumnNode Node { get; } = new();

    public IColumnBuilder Spacing(double spacing, UnitType unit = UnitType.Mm)
    {
        Node.Spacing = spacing;
        return this;
    }

    public IContainer Item()
    {
        var item = new Container();
        Node.Items.Add(item);
        return item;
    }
}

public sealed class RowBuilder : IRowBuilder
{
    internal RowNode Node { get; } = new();

    public IRowBuilder Spacing(double spacing, UnitType unit = UnitType.Pt)
    {
        Node.Spacing = spacing;
        return this;
    }

    public IContainer ConstantItem(double width, UnitType unit = UnitType.Pt)
    {
        var itemContainer = new Container();
        Node.Items.Add(new RowItem
        {
            IsConstant = true,
            Value = width,
            Container = itemContainer
        });
        return itemContainer;
    }

    public IContainer RelativeItem(double weight = 1.0)
    {
        var itemContainer = new Container();
        Node.Items.Add(new RowItem
        {
            IsConstant = false,
            Value = weight <= 0 ? 1.0 : weight,
            Container = itemContainer
        });
        return itemContainer;
    }
}

public sealed class TableColumnsBuilder : ITableColumnsBuilder
{
    public List<TableColumnDefinition> Columns { get; } = [];

    public void ConstantColumn(double width, UnitType unit = UnitType.Pt)
    {
        Columns.Add(new TableColumnDefinition { Width = $"{width}pt" });
    }

    public void RelativeColumn(double weight = 1.0)
    {
        Columns.Add(new TableColumnDefinition { Width = $"{weight}*" });
    }
}

public sealed class TableCellBuilder : ITableCellBuilder
{
    public TableCellDefinition Cell { get; } = new();

    public ITableCellBuilder Text(string text)
    {
        Cell.Text = text;
        return this;
    }

    public ITableCellBuilder Text(Action<ITextBuilder> textConfig)
    {
        var builder = new TextBuilder();
        textConfig(builder);
        var elem = builder.Build();
        Cell.Text = elem.Text;
        Cell.Style = elem.Style;
        return this;
    }

    public ITableCellBuilder Bold(bool bold = true)
    {
        Cell.Style ??= new StyleDefinition();
        Cell.Style.FontWeight = bold ? "Bold" : "Normal";
        return this;
    }

    public ITableCellBuilder SemiBold()
    {
        Cell.Style ??= new StyleDefinition();
        Cell.Style.FontWeight = "SemiBold";
        return this;
    }

    public ITableCellBuilder FontSize(double size)
    {
        Cell.Style ??= new StyleDefinition();
        Cell.Style.FontSize = size;
        return this;
    }

    public ITableCellBuilder Color(string hexColor)
    {
        Cell.Style ??= new StyleDefinition();
        Cell.Style.Color = hexColor;
        return this;
    }

    public ITableCellBuilder Align(HorizontalAlign align)
    {
        Cell.Style ??= new StyleDefinition();
        Cell.Style.Align = align;
        return this;
    }

    public ITableCellBuilder AlignLeft() => Align(HorizontalAlign.Left);
    public ITableCellBuilder AlignCenter() => Align(HorizontalAlign.Center);
    public ITableCellBuilder AlignRight() => Align(HorizontalAlign.Right);

    public ITableCellBuilder BackgroundColor(string hexColor)
    {
        Cell.BackgroundColor = hexColor;
        return this;
    }

    public ITableCellBuilder ColumnSpan(int span)
    {
        Cell.ColumnSpan = span;
        return this;
    }

    public ITableCellBuilder RowSpan(int span)
    {
        Cell.RowSpan = span;
        return this;
    }

    public ITableCellBuilder Element(Action<IContainer> elementConfig)
    {
        return this;
    }
}

public sealed class TableRowBuilder : ITableRowBuilder
{
    public TableRowDefinition Row { get; } = new();

    public ITableRowBuilder Height(double height)
    {
        Row.Height = height;
        return this;
    }

    public ITableRowBuilder BackgroundColor(string hexColor)
    {
        Row.BackgroundColor = hexColor;
        return this;
    }

    public ITableCellBuilder Cell()
    {
        var cellBuilder = new TableCellBuilder();
        Row.Cells.Add(cellBuilder.Cell);
        return cellBuilder;
    }
}

public sealed class TableBuilder : ITableBuilder
{
    public TableDefinition Table { get; } = new();

    public ITableBuilder ColumnsDefinition(Action<ITableColumnsBuilder> columnsConfig)
    {
        var colBuilder = new TableColumnsBuilder();
        columnsConfig(colBuilder);
        Table.Columns = colBuilder.Columns;
        return this;
    }

    public ITableBuilder Header(Action<ITableRowBuilder> headerConfig)
    {
        var rowBuilder = new TableRowBuilder();
        rowBuilder.Height(10.0);
        headerConfig(rowBuilder);
        Table.Header = rowBuilder.Row;
        return this;
    }

    public ITableBuilder Row(Action<ITableRowBuilder> rowConfig)
    {
        var rowBuilder = new TableRowBuilder();
        rowBuilder.Height(8.0);
        rowConfig(rowBuilder);
        Table.Rows.Add(rowBuilder.Row);
        return this;
    }

    public ITableBuilder Footer(Action<ITableRowBuilder> footerConfig)
    {
        var rowBuilder = new TableRowBuilder();
        rowBuilder.Height(9.0);
        footerConfig(rowBuilder);
        Table.Footer = rowBuilder.Row;
        return this;
    }

    public ITableBuilder AlternatingRowBackground(string hexColor)
    {
        Table.AlternatingRowBackground = hexColor;
        return this;
    }

    internal ILayoutNode ToLayoutNode()
    {
        double totalHeight = 0;
        if (Table.Header != null) totalHeight += Table.Header.Height;
        foreach (var r in Table.Rows) totalHeight += r.Height;
        if (Table.Footer != null) totalHeight += Table.Footer.Height;

        var elem = new ElementDefinition
        {
            Type = ElementType.Table,
            Table = Table,
            CanGrow = true,
            Height = Math.Max(totalHeight, 10.0)
        };
        return new ElementNode(elem);
    }
}

public sealed class TextSpanBuilder : ITextSpanBuilder
{
    public string Text { get; set; } = string.Empty;
    public string? FontWeight { get; set; }
    public string? FontStyle { get; set; }
    public double? SpanFontSize { get; set; }
    public string? SpanColor { get; set; }

    public ITextSpanBuilder Bold(bool bold = true)
    {
        FontWeight = bold ? "Bold" : "Normal";
        return this;
    }

    public ITextSpanBuilder SemiBold()
    {
        FontWeight = "SemiBold";
        return this;
    }

    public ITextSpanBuilder Italic(bool italic = true)
    {
        FontStyle = italic ? "Italic" : "Normal";
        return this;
    }

    public ITextSpanBuilder FontSize(double size)
    {
        SpanFontSize = size;
        return this;
    }

    public ITextSpanBuilder Color(string hexColor)
    {
        SpanColor = hexColor;
        return this;
    }
}

public sealed class TextBuilder : ITextBuilder
{
    private readonly List<TextSpanBuilder> _spans = [];
    private readonly StyleDefinition _style = new();

    public ITextSpanBuilder Span(string text)
    {
        var span = new TextSpanBuilder { Text = text };
        _spans.Add(span);
        return span;
    }

    public ITextSpanBuilder CurrentPageNumber()
    {
        return Span("{page}");
    }

    public ITextSpanBuilder TotalPages()
    {
        return Span("{total}");
    }

    public ITextSpanBuilder BahtText(decimal amount)
    {
        return Span(BahtTextFormatter.ToBahtText(amount));
    }

    public ITextSpanBuilder BahtText(double amount)
    {
        return Span(BahtTextFormatter.ToBahtText((decimal)amount));
    }

    public ITextSpanBuilder ThaiDate(DateTime date, string format = "d MMMM yyyy")
    {
        return Span(ThaiDateFormatter.FormatBuddhistDate(date, format));
    }

    public ITextBuilder Bold(bool bold = true)
    {
        _style.FontWeight = bold ? "Bold" : "Normal";
        return this;
    }

    public ITextBuilder SemiBold()
    {
        _style.FontWeight = "SemiBold";
        return this;
    }

    public ITextBuilder Italic(bool italic = true)
    {
        _style.FontStyle = italic ? "Italic" : "Normal";
        return this;
    }

    public ITextBuilder FontSize(double size)
    {
        _style.FontSize = size;
        return this;
    }

    public ITextBuilder Color(string hexColor)
    {
        _style.Color = hexColor;
        return this;
    }

    public ITextBuilder FontFamily(string family)
    {
        _style.FontFamily = family;
        return this;
    }

    public ITextBuilder Align(HorizontalAlign align)
    {
        _style.Align = align;
        return this;
    }

    public ITextBuilder AlignLeft() => Align(HorizontalAlign.Left);
    public ITextBuilder AlignCenter() => Align(HorizontalAlign.Center);
    public ITextBuilder AlignRight() => Align(HorizontalAlign.Right);

    public ElementDefinition Build()
    {
        string fullText = string.Concat(_spans.Select(s => s.Text));
        var firstSpan = _spans.FirstOrDefault();
        if (firstSpan != null)
        {
            if (firstSpan.FontWeight != null) _style.FontWeight = firstSpan.FontWeight;
            if (firstSpan.FontStyle != null) _style.FontStyle = firstSpan.FontStyle;
            if (firstSpan.SpanFontSize != null) _style.FontSize = firstSpan.SpanFontSize.Value;
            if (firstSpan.SpanColor != null) _style.Color = firstSpan.SpanColor;
        }

        return new ElementDefinition
        {
            Type = ElementType.Text,
            Text = fullText,
            Style = _style,
            CanGrow = true,
            Height = Math.Max(6.0, _style.FontSize * 0.4)
        };
    }
}

public sealed class TextStyleBuilder : ITextStyleBuilder
{
    internal StyleDefinition Style { get; } = new();

    public ITextStyleBuilder FontFamily(string family)
    {
        Style.FontFamily = family;
        return this;
    }

    public ITextStyleBuilder FontSize(double size)
    {
        Style.FontSize = size;
        return this;
    }

    public ITextStyleBuilder Color(string hexColor)
    {
        Style.Color = hexColor;
        return this;
    }

    public ITextStyleBuilder Bold(bool bold = true)
    {
        Style.FontWeight = bold ? "Bold" : "Normal";
        return this;
    }

    public ITextStyleBuilder Italic(bool italic = true)
    {
        Style.FontStyle = italic ? "Italic" : "Normal";
        return this;
    }
}

public sealed class PageBuilder : IPageBuilder
{
    public PageSetup PageSetup { get; } = new()
    {
        PaperKind = PaperKind.A4,
        Width = 210.0,
        Height = 297.0,
        Unit = UnitType.Mm,
        Margins = new MarginDefinition { Top = 10.0, Bottom = 10.0, Left = 10.0, Right = 10.0 }
    };

    public StyleDefinition DefaultStyle { get; } = new()
    {
        FontFamily = "Sarabun",
        FontSize = 10.0,
        Color = "#000000"
    };

    public Container HeaderContainer { get; } = new();
    public Container ContentContainer { get; } = new();
    public Container FooterContainer { get; } = new();

    public IPageBuilder Size(PaperKind kind)
    {
        PageSetup.PaperKind = kind;
        (PageSetup.Width, PageSetup.Height) = kind switch
        {
            PaperKind.A4 => (210.0, 297.0),
            PaperKind.A3 => (297.0, 420.0),
            PaperKind.A5 => (148.0, 210.0),
            PaperKind.Letter => (215.9, 279.4),
            PaperKind.Legal => (215.9, 355.6),
            _ => (210.0, 297.0)
        };
        PageSetup.Unit = UnitType.Mm;
        return this;
    }

    public IPageBuilder Size(double width, double height, UnitType unit = UnitType.Mm)
    {
        PageSetup.PaperKind = PaperKind.Custom;
        PageSetup.Width = width;
        PageSetup.Height = height;
        PageSetup.Unit = unit;
        return this;
    }

    public IPageBuilder Orientation(PageOrientation orientation)
    {
        PageSetup.Orientation = orientation;
        return this;
    }

    public IPageBuilder Margin(double margin, UnitType unit = UnitType.Mm)
    {
        return Margin(margin, margin, margin, margin, unit);
    }

    public IPageBuilder Margin(double top, double bottom, double left, double right, UnitType unit = UnitType.Mm)
    {
        PageSetup.Margins = new MarginDefinition
        {
            Top = top,
            Bottom = bottom,
            Left = left,
            Right = right
        };
        return this;
    }

    public IPageBuilder ContinuousHeight(bool continuous = true)
    {
        PageSetup.ContinuousHeight = continuous;
        return this;
    }

    public IPageBuilder Watermark(string text, float opacity = 0.2f, float rotationAngle = -45f)
    {
        PageSetup.Watermark = new WatermarkDefinition
        {
            Text = text,
            Opacity = opacity,
            RotationAngle = rotationAngle,
            Layer = WatermarkLayer.Background
        };
        return this;
    }

    public IPageBuilder DefaultTextStyle(Action<ITextStyleBuilder> styleConfig)
    {
        var builder = new TextStyleBuilder();
        styleConfig(builder);
        if (builder.Style.FontFamily != null) DefaultStyle.FontFamily = builder.Style.FontFamily;
        if (builder.Style.FontSize > 0) DefaultStyle.FontSize = builder.Style.FontSize;
        if (builder.Style.Color != null) DefaultStyle.Color = builder.Style.Color;
        if (builder.Style.FontWeight != null) DefaultStyle.FontWeight = builder.Style.FontWeight;
        if (builder.Style.FontStyle != null) DefaultStyle.FontStyle = builder.Style.FontStyle;
        return this;
    }

    public IContainer Header() => HeaderContainer;
    public IContainer Content() => ContentContainer;
    public IContainer Footer() => FooterContainer;

    internal ReportDefinition CompileReport()
    {
        var (widthMm, heightMm) = PageSetup.GetEffectiveDimensionsMm();
        var unit = PageSetup.Unit;
        var margins = PageSetup.Margins;

        double availableWidth = widthMm - margins.Left - margins.Right;
        if (availableWidth <= 0) availableWidth = 190.0;

        double startX = margins.Left;

        var report = new ReportDefinition
        {
            PageSetup = PageSetup,
            Bands = new BandsDefinition()
        };

        // Header
        if (HeaderContainer.Nodes.Count > 0)
        {
            var (headerElements, headerHeight) = HeaderContainer.Compile(startX, 0, availableWidth, unit, DefaultStyle);
            report.Bands.PageHeader = new BandDefinition
            {
                Height = Math.Max(headerHeight, 5.0),
                CanGrow = true,
                Elements = headerElements
            };
        }

        // Content
        if (ContentContainer.Nodes.Count > 0)
        {
            var (contentElements, contentHeight) = ContentContainer.Compile(startX, 0, availableWidth, unit, DefaultStyle);
            report.Bands.Detail = new BandDefinition
            {
                Height = Math.Max(contentHeight, 10.0),
                CanGrow = true,
                Elements = contentElements
            };
        }

        // Footer
        if (FooterContainer.Nodes.Count > 0)
        {
            var (footerElements, footerHeight) = FooterContainer.Compile(startX, 0, availableWidth, unit, DefaultStyle);
            report.Bands.PageFooter = new BandDefinition
            {
                Height = Math.Max(footerHeight, 5.0),
                CanGrow = true,
                Elements = footerElements
            };
        }

        return report;
    }
}

public sealed class DocumentContainer : IDocumentContainer
{
    internal PageBuilder PageBuilder { get; } = new();

    public IPageBuilder Page(Action<IPageBuilder> pageConfig)
    {
        ArgumentNullException.ThrowIfNull(pageConfig);
        pageConfig(PageBuilder);
        return PageBuilder;
    }
}
