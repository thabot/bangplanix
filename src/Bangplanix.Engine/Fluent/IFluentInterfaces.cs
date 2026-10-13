using Bangplanix.Core.Models;

namespace Bangplanix.Fluent;

public interface IDocumentContainer
{
    IPageBuilder Page(Action<IPageBuilder> pageConfig);
}

public interface IPageBuilder
{
    IPageBuilder Size(PaperKind kind);
    IPageBuilder Size(double width, double height, UnitType unit = UnitType.Mm);
    IPageBuilder Orientation(PageOrientation orientation);
    IPageBuilder Margin(double margin, UnitType unit = UnitType.Mm);
    IPageBuilder Margin(double top, double bottom, double left, double right, UnitType unit = UnitType.Mm);
    IPageBuilder ContinuousHeight(bool continuous = true);
    IPageBuilder Watermark(string text, float opacity = 0.2f, float rotationAngle = -45f);
    IPageBuilder DefaultTextStyle(Action<ITextStyleBuilder> styleConfig);
    IContainer Header();
    IContainer Content();
    IContainer Footer();
}

public interface ITextStyleBuilder
{
    ITextStyleBuilder FontFamily(string family);
    ITextStyleBuilder FontSize(double size);
    ITextStyleBuilder Color(string hexColor);
    ITextStyleBuilder Bold(bool bold = true);
    ITextStyleBuilder Italic(bool italic = true);
}

public interface IContainer
{
    IContainer Column(Action<IColumnBuilder> columnConfig);
    IContainer Row(Action<IRowBuilder> rowConfig);
    IContainer Table(Action<ITableBuilder> tableConfig);
    IContainer Text(string text);
    IContainer Text(Action<ITextBuilder> textConfig);
    IContainer Image(string source, double width = 0, double height = 0);
    IContainer Barcode(string value, BarcodeType type = BarcodeType.Code128, double width = 100, double height = 40);
    IContainer QrCode(string value, double size = 50);
    IContainer QrPromptPay(string targetId, double amount, double size = 80);
    IContainer Component(IReportComponent component);
    IContainer Element(Action<IContainer> elementConfig);

    // Layout Modifiers
    IContainer Padding(double all, UnitType unit = UnitType.Mm);
    IContainer PaddingVertical(double padding, UnitType unit = UnitType.Mm);
    IContainer PaddingHorizontal(double padding, UnitType unit = UnitType.Mm);
    IContainer PaddingTop(double pt, UnitType unit = UnitType.Mm);
    IContainer PaddingBottom(double pt, UnitType unit = UnitType.Mm);
    IContainer PaddingLeft(double pt, UnitType unit = UnitType.Mm);
    IContainer PaddingRight(double pt, UnitType unit = UnitType.Mm);
    IContainer AlignLeft();
    IContainer AlignCenter();
    IContainer AlignRight();
    IContainer BackgroundColor(string hexColor);
    IContainer Bold(bool bold = true);
    IContainer SemiBold();
    IContainer Italic(bool italic = true);
    IContainer FontSize(double size);
    IContainer FontColor(string hexColor);
    IContainer Color(string hexColor);
    IContainer FontFamily(string family);
    IContainer Width(double width, UnitType unit = UnitType.Mm);
    IContainer Height(double height, UnitType unit = UnitType.Mm);
}

public interface IColumnBuilder
{
    IColumnBuilder Spacing(double spacing, UnitType unit = UnitType.Mm);
    IContainer Item();
}

public interface IRowBuilder
{
    IRowBuilder Spacing(double spacing, UnitType unit = UnitType.Pt);
    IContainer ConstantItem(double width, UnitType unit = UnitType.Pt);
    IContainer RelativeItem(double weight = 1.0);
}

public interface ITableBuilder
{
    ITableBuilder ColumnsDefinition(Action<ITableColumnsBuilder> columnsConfig);
    ITableBuilder Header(Action<ITableRowBuilder> headerConfig);
    ITableBuilder Row(Action<ITableRowBuilder> rowConfig);
    ITableBuilder Footer(Action<ITableRowBuilder> footerConfig);
    ITableBuilder AlternatingRowBackground(string hexColor);
}

public interface ITableColumnsBuilder
{
    void ConstantColumn(double width, UnitType unit = UnitType.Pt);
    void RelativeColumn(double weight = 1.0);
}

public interface ITableRowBuilder
{
    ITableRowBuilder Height(double height);
    ITableRowBuilder BackgroundColor(string hexColor);
    ITableCellBuilder Cell();
}

public interface ITableCellBuilder
{
    ITableCellBuilder Text(string text);
    ITableCellBuilder Text(Action<ITextBuilder> textConfig);
    ITableCellBuilder Bold(bool bold = true);
    ITableCellBuilder SemiBold();
    ITableCellBuilder FontSize(double size);
    ITableCellBuilder Color(string hexColor);
    ITableCellBuilder Align(HorizontalAlign align);
    ITableCellBuilder AlignLeft();
    ITableCellBuilder AlignCenter();
    ITableCellBuilder AlignRight();
    ITableCellBuilder BackgroundColor(string hexColor);
    ITableCellBuilder ColumnSpan(int span);
    ITableCellBuilder RowSpan(int span);
    ITableCellBuilder Element(Action<IContainer> elementConfig);
}

public interface ITextBuilder
{
    ITextSpanBuilder Span(string text);
    ITextSpanBuilder CurrentPageNumber();
    ITextSpanBuilder TotalPages();
    ITextSpanBuilder BahtText(decimal amount);
    ITextSpanBuilder BahtText(double amount);
    ITextSpanBuilder ThaiDate(DateTime date, string format = "d MMMM yyyy");

    ITextBuilder Bold(bool bold = true);
    ITextBuilder SemiBold();
    ITextBuilder Italic(bool italic = true);
    ITextBuilder FontSize(double size);
    ITextBuilder Color(string hexColor);
    ITextBuilder FontFamily(string family);
    ITextBuilder Align(HorizontalAlign align);
    ITextBuilder AlignLeft();
    ITextBuilder AlignCenter();
    ITextBuilder AlignRight();
}

public interface ITextSpanBuilder
{
    ITextSpanBuilder Bold(bool bold = true);
    ITextSpanBuilder SemiBold();
    ITextSpanBuilder Italic(bool italic = true);
    ITextSpanBuilder FontSize(double size);
    ITextSpanBuilder Color(string hexColor);
}

public interface IReportComponent
{
    void Compose(IContainer container);
}
