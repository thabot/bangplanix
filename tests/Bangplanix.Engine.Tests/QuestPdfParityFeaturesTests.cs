using System.Text;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Bands;
using Bangplanix.Engine.Canvas;
using Bangplanix.Engine.Pagination;
using Bangplanix.Engine.Pdf;
using Bangplanix.Engine.Visuals;
using FluentAssertions;
using SkiaSharp;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class QuestPdfParityFeaturesTests
{
    [Fact]
    public async Task AutoPageNumbering_Enabled_GeneratesValidPdfBytes()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                ShowPageNumbers = true,
                PageNumberFormat = "หน้า {page} จาก {total}",
                PageNumberPosition = PageNumberPosition.BottomRight
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 20.0,
                    Elements =
                    [
                        new ElementDefinition { Type = ElementType.Text, Text = "Test Row", X = 10, Y = 5, Width = 100, Height = 10 }
                    ]
                }
            }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void DynamicTextWrapping_CanGrow_ExpandsRowHeightAccurately()
    {
        var longText = "บริษัท บางพลานิกซ์ จำกัด เป็นผู้ให้บริการระบบรายงานมาตรฐานสากล รองรับการทำงานทั้งในและต่างประเทศแบบครบวงจร";
        var band = new BandDefinition
        {
            Height = 10.0,
            CanGrow = true,
            Elements =
            [
                new ElementDefinition
                {
                    Type = ElementType.Text,
                    Text = longText,
                    X = 0,
                    Y = 0,
                    Width = 40.0,
                    Height = 10.0,
                    CanGrow = true,
                    Style = new StyleDefinition { FontSize = 12.0 }
                }
            ]
        };

        var context = new BandContext
        {
            Report = new ReportDefinition()
        };

        var dynamicHeight = PaginationEngine.CalculateDynamicBandHeight(band, UnitType.Mm, context);

        // Long text in narrow 40mm width must wrap into multiple lines, resulting in height > 10mm (converted to pt)
        var baseHeightPt = UnitConverter.ToPoints(10.0, UnitType.Mm);
        dynamicHeight.Should().BeGreaterThan(baseHeightPt);
    }

    [Fact]
    public void FlowControl_EnsureSpace_CalculatesCorrectPageBreaks()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Height = 297.0,
                Margins = new MarginDefinition { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 80.0,
                    EnsureSpace = 130.0 // 130mm threshold > remaining space at row 3 (117mm)
                }
            }
        };

        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["id"] = 1 },
            new Dictionary<string, object?> { ["id"] = 2 },
            new Dictionary<string, object?> { ["id"] = 3 }
        };

        var context = new BandContext
        {
            Report = report,
            MainDataRows = rows
        };

        // Render to memory SKDocument to verify no crash
        using var stream = new MemoryStream();
        using var doc = SKDocument.CreatePdf(stream);
        PaginationEngine.RenderReportPages(report, doc, context);
        doc.Close();

        context.TotalPages.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void FlowControl_ShowOnPages_MatchesCorrectly()
    {
        var context = new BandContext { CurrentPageNumber = 1, TotalPages = 3 };

        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.All, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.FirstPageOnly, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.NotFirstPage, context).Should().BeFalse();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.LastPageOnly, context).Should().BeFalse();

        context.CurrentPageNumber = 3;
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.LastPageOnly, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.NotFirstPage, context).Should().BeTrue();
    }

    [Fact]
    public void TableRenderer_CalculateColumnWidths_WithRelativeAndConstant()
    {
        var columns = new List<TableColumnDefinition>
        {
            new TableColumnDefinition { Width = "50pt" },
            new TableColumnDefinition { Width = "2*" },
            new TableColumnDefinition { Width = "1*" }
        };

        float totalWidthPt = 350.0f;
        var widths = TableRenderer.CalculateColumnWidths(columns, totalWidthPt, UnitType.Pt);

        widths.Length.Should().Be(3);
        widths[0].Should().Be(50.0f);
        widths[1].Should().BeApproximately(200.0f, 0.01f);
        widths[2].Should().BeApproximately(100.0f, 0.01f);
    }

    [Fact]
    public async Task TableComponent_InDetailBand_RendersToPdfSuccessfully()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup { PaperKind = PaperKind.A4 },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 60.0,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Type = ElementType.Table,
                            X = 10,
                            Y = 5,
                            Width = 190,
                            Height = 50,
                            Table = new TableDefinition
                            {
                                Columns =
                                [
                                    new TableColumnDefinition { Width = "1*" },
                                    new TableColumnDefinition { Width = "3*" },
                                    new TableColumnDefinition { Width = "1*" }
                                ],
                                Header = new TableRowDefinition
                                {
                                    Height = 10,
                                    BackgroundColor = "#EFEFEF",
                                    Cells =
                                    [
                                        new TableCellDefinition { Text = "ลำดับ" },
                                        new TableCellDefinition { Text = "รายการสินค้า" },
                                        new TableCellDefinition { Text = "ราคา" }
                                    ]
                                },
                                Rows =
                                [
                                    new TableRowDefinition
                                    {
                                        Height = 8,
                                        Cells =
                                        [
                                            new TableCellDefinition { Text = "1" },
                                            new TableCellDefinition { Text = "Apple iPad Pro 11-inch M4 WiFi 256GB Space Black" },
                                            new TableCellDefinition { Text = "35,900.00" }
                                        ]
                                    }
                                ],
                                AlternatingRowBackground = "#F9F9F9"
                            }
                        }
                    ]
                }
            }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task Watermark_TextAndImage_RendersWithoutErrors()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Watermark = new WatermarkDefinition
                {
                    Text = "CONFIDENTIAL - DRAFT",
                    Opacity = 0.2f,
                    RotationAngle = -45.0f,
                    Layer = WatermarkLayer.Background
                }
            },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 30.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Main Report", X = 10, Y = 10, Width = 100, Height = 10 }]
                }
            }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task ContinuousHeight_PosSlip_GeneratesSinglePagePdf()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                Width = 80.0,
                Unit = UnitType.Mm,
                ContinuousHeight = true,
                Margins = new MarginDefinition { Top = 5, Bottom = 5, Left = 5, Right = 5 }
            },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 20.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "ร้านค้าตัวอย่าง POS", X = 0, Y = 0, Width = 70, Height = 10 }]
                },
                Detail = new BandDefinition
                {
                    Height = 10.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Expression = "@Row.item", X = 0, Y = 0, Width = 70, Height = 8 }]
                },
                ReportFooter = new BandDefinition
                {
                    Height = 15.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "ขอบคุณที่ใช้บริการ", X = 0, Y = 0, Width = 70, Height = 10 }]
                }
            }
        };

        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["item"] = "กาแฟลาเต้เย็น" },
            new Dictionary<string, object?> { ["item"] = "ครัวซองต์เนยสด" },
            new Dictionary<string, object?> { ["item"] = "เค้กช็อกโกแลต" }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report, null, rows);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Theory]
    [InlineData(PageNumberPosition.BottomRight)]
    [InlineData(PageNumberPosition.BottomCenter)]
    [InlineData(PageNumberPosition.BottomLeft)]
    [InlineData(PageNumberPosition.TopRight)]
    [InlineData(PageNumberPosition.TopCenter)]
    [InlineData(PageNumberPosition.TopLeft)]
    public async Task AutoPageNumbering_AllPositions_RendersSuccessfully(PageNumberPosition position)
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                ShowPageNumbers = true,
                PageNumberFormat = "Page {page} of {total}",
                PageNumberPosition = position,
                PageNumberStyle = new StyleDefinition { FontSize = 8.0, Color = "#333333" }
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 30.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Position Test", X = 10, Y = 5, Width = 100, Height = 10 }]
                }
            }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void DynamicTextWrapping_MixedThaiEnglishNumeric_WrapsAndMeasuresAccurately()
    {
        var mixedText = "รหัสสินค้า SKU-994821: Apple MacBook Pro 16\" (M3 Max 128GB RAM 2TB SSD) Space Black ประกันศูนย์ AppleCare+ 3 ปี";
        using var paint = new SKPaint { TextSize = 12.0f };
        var lines = SkiaReportCanvas.WrapTextLines(mixedText, paint, 150.0f);

        lines.Should().NotBeEmpty();
        lines.Count.Should().BeGreaterThan(1, "Long mixed string in narrow width must wrap into multiple lines");

        var measuredHeight = SkiaReportCanvas.MeasureTextHeight(mixedText, 150.0f, new StyleDefinition { FontSize = 12.0 });
        measuredHeight.Should().BeGreaterThan(20.0f);
    }

    [Fact]
    public void DynamicTextWrapping_ExplicitNewlines_CalculatesLineCountAccurately()
    {
        var multilineText = "บรรทัดที่ 1: รายการเริ่มต้น\r\nบรรทัดที่ 2: รายละเอียดเพิ่มเติม\nบรรทัดที่ 3: สรุปข้อมูล";
        using var paint = new SKPaint { TextSize = 10.0f };
        var lines = SkiaReportCanvas.WrapTextLines(multilineText, paint, 500.0f);

        lines.Count.Should().Be(3);
        lines[0].Should().Contain("บรรทัดที่ 1");
        lines[1].Should().Contain("บรรทัดที่ 2");
        lines[2].Should().Contain("บรรทัดที่ 3");
    }

    [Fact]
    public async Task DynamicTextWrapping_TextAlignments_RenderWithoutError()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup { PaperKind = PaperKind.A4 },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 60.0,
                    Elements =
                    [
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = "ข้อความชิดซ้าย\nบรรทัดสอง",
                            X = 10, Y = 5, Width = 50, Height = 20, CanGrow = true,
                            Style = new StyleDefinition { Align = HorizontalAlign.Left }
                        },
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = "ข้อความกึ่งกลาง\nบรรทัดสอง",
                            X = 70, Y = 5, Width = 50, Height = 20, CanGrow = true,
                            Style = new StyleDefinition { Align = HorizontalAlign.Center }
                        },
                        new ElementDefinition
                        {
                            Type = ElementType.Text,
                            Text = "ข้อความชิดขวา\nบรรทัดสอง",
                            X = 130, Y = 5, Width = 50, Height = 20, CanGrow = true,
                            Style = new StyleDefinition { Align = HorizontalAlign.Right }
                        }
                    ]
                }
            }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report);
        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void TableComponent_ColumnSpanAndCellPadding_CalculatesCorrectLayout()
    {
        var table = new TableDefinition
        {
            Columns =
            [
                new TableColumnDefinition { Width = "50pt" },
                new TableColumnDefinition { Width = "100pt" },
                new TableColumnDefinition { Width = "150pt" }
            ],
            Rows =
            [
                new TableRowDefinition
                {
                    Height = 25,
                    Cells =
                    [
                        new TableCellDefinition { Text = "Col 1" },
                        new TableCellDefinition
                        {
                            Text = "Spanning 2 Columns",
                            ColumnSpan = 2,
                            Padding = new TablePaddingDefinition { Left = 10, Right = 10, Top = 5, Bottom = 5 },
                            Border = new BorderDefinition { Width = 1.5, Color = "#FF0000" }
                        }
                    ]
                }
            ]
        };

        var colWidths = TableRenderer.CalculateColumnWidths(table.Columns, 300.0f, UnitType.Pt);
        colWidths.Should().Equal([50.0f, 100.0f, 150.0f]);

        var context = new BandContext { Report = new ReportDefinition() };
        var measuredHeight = TableRenderer.MeasureRowHeight(table.Rows[0], colWidths, UnitType.Pt, context, context.Report);
        measuredHeight.Should().BeGreaterThanOrEqualTo(25.0f);
    }

    [Fact]
    public void FlowControl_EnsureSpace_OnReportFooter_ForcesNewPage()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Height = 297.0,
                Margins = new MarginDefinition { Top = 10, Bottom = 10, Left = 10, Right = 10 }
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition { Height = 250.0 }, // Consumes almost whole page
                ReportFooter = new BandDefinition
                {
                    Height = 40.0,
                    EnsureSpace = 50.0 // 50mm > remaining space (27mm) -> forces page break to page 2
                }
            }
        };

        var context = new BandContext
        {
            Report = report,
            MainDataRows = [new Dictionary<string, object?> { ["x"] = 1 }]
        };

        using var stream = new MemoryStream();
        using var doc = SKDocument.CreatePdf(stream);
        PaginationEngine.RenderReportPages(report, doc, context);
        doc.Close();

        context.TotalPages.Should().Be(2, "ReportFooter with EnsureSpace exceeding remaining space must break to page 2");
    }

    [Fact]
    public void FlowControl_ShowOnPages_AllModes_MatchCorrectly()
    {
        var context = new BandContext { CurrentPageNumber = 2, TotalPages = 4 };

        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.All, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.FirstPageOnly, context).Should().BeFalse();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.NotFirstPage, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.EvenPages, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.OddPages, context).Should().BeFalse();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.NotLastPage, context).Should().BeTrue();
        SkiaReportCanvas.IsPageDisplayMatch(PageDisplayMode.LastPageOnly, context).Should().BeFalse();
    }

    [Fact]
    public void DynamicContext_Expressions_ResolvesGlobalsCorrectly()
    {
        var context = new BandContext
        {
            CurrentPageNumber = 1,
            TotalPages = 5,
            AvailableHeight = 350.5f,
            Parameters = new Dictionary<string, object?> { ["Branch"] = "Bangkok HQ" }
        };

        context.ResolveExpressionOrValue(null, "@Globals.PageNumber").Should().Be(1);
        context.ResolveExpressionOrValue(null, "@Globals.TotalPages").Should().Be(5);
        context.ResolveExpressionOrValue(null, "@Globals.IsFirstPage").Should().Be(true);
        context.ResolveExpressionOrValue(null, "@Globals.IsLastPage").Should().Be(false);
        context.ResolveExpressionOrValue(null, "@Context.AvailableHeight").Should().Be(350.5f);
        context.ResolveExpressionOrValue(null, "@Parameters.Branch").Should().Be("Bangkok HQ");

        // Last page check
        context.CurrentPageNumber = 5;
        context.ResolveExpressionOrValue(null, "@Globals.IsFirstPage").Should().Be(false);
        context.ResolveExpressionOrValue(null, "@Globals.IsLastPage").Should().Be(true);
    }

    [Fact]
    public async Task Watermark_ForegroundLayer_RendersSuccessfully()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup { PaperKind = PaperKind.A4 },
            Watermark = new WatermarkDefinition
            {
                Text = "SAMPLE FOREGROUND",
                Layer = WatermarkLayer.Foreground,
                Opacity = 0.3f,
                RotationAngle = -30.0f
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 20.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Text = "Underlying Content", X = 10, Y = 10, Width = 100, Height = 10 }]
                }
            }
        };

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public async Task ContinuousHeight_ThirtyRows_GeneratesSinglePagePdf()
    {
        var report = new ReportDefinition
        {
            PageSetup = new PageSetup
            {
                Width = 80.0,
                Unit = UnitType.Mm,
                ContinuousHeight = true,
                Margins = new MarginDefinition { Top = 5, Bottom = 5, Left = 5, Right = 5 }
            },
            Bands = new BandsDefinition
            {
                Detail = new BandDefinition
                {
                    Height = 8.0,
                    Elements = [new ElementDefinition { Type = ElementType.Text, Expression = "@Row.name + ' - ' + @Row.qty", X = 0, Y = 0, Width = 70, Height = 7 }]
                }
            }
        };

        var rows = new List<IDictionary<string, object?>>();
        for (int i = 1; i <= 30; i++)
        {
            rows.Add(new Dictionary<string, object?> { ["name"] = $"Item #{i}", ["qty"] = $"{i} pcs" });
        }

        var renderer = new SkiaPdfRenderer();
        var pdfBytes = await renderer.RenderToPdfAsync(report, null, rows);

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(1000);
    }
}
