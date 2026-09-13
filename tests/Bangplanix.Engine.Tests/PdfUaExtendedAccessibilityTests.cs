using System;
using System.Collections.Generic;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Accessibility;
using Xunit;

namespace Bangplanix.Engine.Tests;

public class PdfUaExtendedAccessibilityTests
{
    [Fact]
    public void ColorContrastAuditorShouldComputeAccurateWcagContrastRatios()
    {
        // Black on White -> 21:1
        var blackOnWhite = ColorContrastAuditor.CalculateContrast("#000000", "#FFFFFF");
        Assert.Equal(21.0, blackOnWhite.Ratio);
        Assert.True(blackOnWhite.PassesAaNormal);
        Assert.True(blackOnWhite.PassesAaaNormal);

        // White on Black -> 21:1
        var whiteOnBlack = ColorContrastAuditor.CalculateContrast("#FFFFFF", "#000000");
        Assert.Equal(21.0, whiteOnBlack.Ratio);
        Assert.True(whiteOnBlack.PassesAaNormal);

        // Light Gray on White -> Fails AA Normal
        var lightGrayOnWhite = ColorContrastAuditor.CalculateContrast("#CCCCCC", "#FFFFFF");
        Assert.True(lightGrayOnWhite.Ratio < 4.5);
        Assert.False(lightGrayOnWhite.PassesAaNormal);
    }

    [Fact]
    public void PdfAccessibilityValidatorShouldDetectMissingAltTextAndViolations()
    {
        var root = new PdfStructElement
        {
            TagType = PdfTagType.Document,
            Children =
            [
                new PdfStructElement { TagType = PdfTagType.H1, ActualText = "รายงานยอดขาย" },
                new PdfStructElement { TagType = PdfTagType.H3, ActualText = "ข้ามระดับ H2" }, // Heading level skip
                new PdfStructElement { TagType = PdfTagType.Figure, AltText = null, Id = "fig_chart1" } // Missing Alt
            ]
        };

        var options = new AccessibilityOptions
        {
            DocumentTitle = "รายงานภาษี",
            PrimaryLanguage = "th-TH",
            DisplayDocTitle = true
        };

        var report = PdfAccessibilityValidator.Validate(root, options);

        Assert.False(report.IsCompliant); // Has Error due to missing Alt
        Assert.Contains(report.Violations, v => v.RuleId == "WCAG-1.1.1-ALT" && v.Severity == AccessibilityViolationSeverity.Error);
        Assert.Contains(report.Violations, v => v.RuleId == "WCAG-1.3.1-HEADING" && v.Severity == AccessibilityViolationSeverity.Warning);
        Assert.True(report.Score < 100.0);
    }

    [Fact]
    public void PdfAccessibilityValidatorShouldPassCompliantTree()
    {
        var root = new PdfStructElement
        {
            TagType = PdfTagType.Document,
            Children =
            [
                new PdfStructElement { TagType = PdfTagType.H1, ActualText = "หัวข้อหลัก" },
                new PdfStructElement { TagType = PdfTagType.H2, ActualText = "หัวข้อย่อย" },
                new PdfStructElement { TagType = PdfTagType.Paragraph, ActualText = "เนื้อหารายงาน" },
                new PdfStructElement { TagType = PdfTagType.Figure, AltText = "แผนภูมิแสดงรายได้ประจำปี 2026", Id = "chart_1" }
            ]
        };

        var options = new AccessibilityOptions
        {
            DocumentTitle = "รายงานประจำปี 2026",
            PrimaryLanguage = "th-TH",
            DisplayDocTitle = true
        };

        var report = PdfAccessibilityValidator.Validate(root, options);

        Assert.True(report.IsCompliant);
        Assert.Equal(0, report.ErrorCount);
        Assert.Equal(0, report.WarningCount);
        Assert.Equal(100.0, report.Score);
    }

    [Fact]
    public void PdfReadingOrderOptimizerShouldSortMultiColumnCorrectly()
    {
        var root = new PdfStructElement
        {
            TagType = PdfTagType.Document,
            Children =
            [
                // Right column item higher up (X: 350, Y: 50)
                new PdfStructElement { Id = "right_top", X = 350, Y = 50, ActualText = "Right Top" },
                // Left column item lower down (X: 50, Y: 100)
                new PdfStructElement { Id = "left_bottom", X = 50, Y = 100, ActualText = "Left Bottom" },
                // Left column item top (X: 50, Y: 20)
                new PdfStructElement { Id = "left_top", X = 50, Y = 20, ActualText = "Left Top" },
                // Right column item bottom (X: 350, Y: 200)
                new PdfStructElement { Id = "right_bottom", X = 350, Y = 200, ActualText = "Right Bottom" }
            ]
        };

        var optimized = PdfReadingOrderOptimizer.OptimizeReadingOrder(root, pageWidth: 600, columns: 2);

        // Should read entire left column first (left_top, left_bottom), then right column (right_top, right_bottom)
        Assert.Equal("left_top", optimized.Children[0].Id);
        Assert.Equal("left_bottom", optimized.Children[1].Id);
        Assert.Equal("right_top", optimized.Children[2].Id);
        Assert.Equal("right_bottom", optimized.Children[3].Id);
    }

    [Fact]
    public void TableAccessibilityEnhancerShouldEnrichTableHeadersAndDataCellReferences()
    {
        var table = new PdfStructElement
        {
            TagType = PdfTagType.Table,
            Children =
            [
                new PdfStructElement
                {
                    TagType = PdfTagType.TableRow,
                    Children =
                    [
                        new PdfStructElement { TagType = PdfTagType.TableHeader, ActualText = "รหัสสินค้า" },
                        new PdfStructElement { TagType = PdfTagType.TableHeader, ActualText = "ชื่อสินค้า" },
                        new PdfStructElement { TagType = PdfTagType.TableHeader, ActualText = "ราคา" }
                    ]
                },
                new PdfStructElement
                {
                    TagType = PdfTagType.TableRow,
                    Children =
                    [
                        new PdfStructElement { TagType = PdfTagType.TableData, ActualText = "SKU-001" },
                        new PdfStructElement { TagType = PdfTagType.TableData, ActualText = "สินค้า A" },
                        new PdfStructElement { TagType = PdfTagType.TableData, ActualText = "500.00" }
                    ]
                }
            ]
        };

        var enhanced = TableAccessibilityEnhancer.EnhanceTable(table, "inv_");

        // Headers should have Column scope and IDs
        var headerRow = enhanced.Children[0];
        Assert.Equal(TableScopeType.Column, headerRow.Children[0].Scope);
        Assert.Equal("inv_th_r0_c0", headerRow.Children[0].Id);
        Assert.Equal("inv_th_r0_c1", headerRow.Children[1].Id);

        // Data cells should link to header IDs
        var dataRow = enhanced.Children[1];
        Assert.Contains("inv_th_r0_c0", dataRow.Children[0].Headers);
        Assert.Contains("inv_th_r0_c1", dataRow.Children[1].Headers);
        Assert.Contains("inv_th_r0_c2", dataRow.Children[2].Headers);
    }

    [Fact]
    public void AccessibleHtmlExporterShouldGenerateSemanticHtmlWithAria()
    {
        var reportDef = new ReportDefinition
        {
            Metadata = new ReportMetadata { Title = "รายงานงบการเงินประจำปี" }
        };

        var structTree = new PdfStructElement
        {
            TagType = PdfTagType.Document,
            Children =
            [
                new PdfStructElement { TagType = PdfTagType.H1, ActualText = "รายงานงบการเงินประจำปี 2026" },
                new PdfStructElement
                {
                    TagType = PdfTagType.Table,
                    Children =
                    [
                        new PdfStructElement
                        {
                            TagType = PdfTagType.TableRow,
                            Children =
                            [
                                new PdfStructElement { TagType = PdfTagType.TableHeader, Scope = TableScopeType.Column, ActualText = "รายการ" },
                                new PdfStructElement { TagType = PdfTagType.TableHeader, Scope = TableScopeType.Column, ActualText = "จำนวนเงิน (บาท)" }
                            ]
                        },
                        new PdfStructElement
                        {
                            TagType = PdfTagType.TableRow,
                            Children =
                            [
                                new PdfStructElement { TagType = PdfTagType.TableData, ActualText = "รายได้จากการขาย" },
                                new PdfStructElement { TagType = PdfTagType.TableData, ActualText = "1,500,000.00" }
                            ]
                        }
                    ]
                },
                new PdfStructElement { TagType = PdfTagType.Figure, AltText = "แผนภูมิแท่งเปรียบเทียบกำไรสุทธิ 5 ปีย้อนหลัง" }
            ]
        };

        var options = new AccessibilityOptions
        {
            DocumentTitle = "รายงานงบการเงินประจำปี",
            PrimaryLanguage = "th-TH"
        };

        string html = AccessibleHtmlExporter.ExportToSemanticHtml(reportDef, structTree, options);

        Assert.Contains("<html lang=\"th-TH\">", html);
        Assert.Contains("<header role=\"banner\">", html);
        Assert.Contains("<main id=\"main-content\" role=\"main\">", html);
        Assert.Contains("<table role=\"table\">", html);
        Assert.Contains("<th scope=\"col\">รายการ</th>", html);
        Assert.Contains("<figure role=\"img\" aria-label=\"แผนภูมิแท่งเปรียบเทียบกำไรสุทธิ 5 ปีย้อนหลัง\">", html);
        Assert.Contains("<figcaption>แผนภูมิแท่งเปรียบเทียบกำไรสุทธิ 5 ปีย้อนหลัง</figcaption>", html);
        Assert.Contains("<footer role=\"contentinfo\">", html);
    }
}