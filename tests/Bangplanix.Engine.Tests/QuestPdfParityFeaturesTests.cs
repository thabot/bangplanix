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
}
