using System;
using System.Collections.Generic;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Accessibility;

/// <summary>
/// PAC-style PDF/UA and WCAG 2.1 Rule Engine and Compliance Auditor.
/// </summary>
public static class PdfAccessibilityValidator
{
    public static AccessibilityAuditReport Validate(
        PdfStructElement root,
        AccessibilityOptions? options = null,
        ReportDefinition? reportDef = null)
    {
        ArgumentNullException.ThrowIfNull(root);

        var opts = options ?? new AccessibilityOptions();
        var violations = new List<AccessibilityViolation>();
        int passedCount = 0;

        // 1. Document Title Check (PDF/UA ISO 14289-1 §7.1)
        string? docTitle = !string.IsNullOrWhiteSpace(opts.DocumentTitle)
            ? opts.DocumentTitle
            : reportDef?.Metadata.Title;

        if (string.IsNullOrWhiteSpace(docTitle))
        {
            violations.Add(new AccessibilityViolation
            {
                RuleId = "PDFUA-TITLE-001",
                Message = "Document Title is missing. PDF/UA requires an explicit document title in metadata.",
                Severity = AccessibilityViolationSeverity.Error,
                Suggestion = "Set AccessibilityOptions.DocumentTitle or ReportDefinition.Metadata.Title."
            });
        }
        else
        {
            passedCount++;
        }

        // 2. Primary Language Check (PDF/UA ISO 14289-1 §7.2)
        if (string.IsNullOrWhiteSpace(opts.PrimaryLanguage))
        {
            violations.Add(new AccessibilityViolation
            {
                RuleId = "PDFUA-LANG-001",
                Message = "Document Primary Language (/Lang) is not specified.",
                Severity = AccessibilityViolationSeverity.Error,
                Suggestion = "Specify PrimaryLanguage (e.g. 'th-TH' or 'en-US')."
            });
        }
        else
        {
            passedCount++;
        }

        // 3. Document Title Viewer Preference (PDF/UA ISO 14289-1 §7.1)
        if (!opts.DisplayDocTitle)
        {
            violations.Add(new AccessibilityViolation
            {
                RuleId = "PDFUA-VIEWTITLE-001",
                Message = "ViewerPreferences /DisplayDocTitle is false or missing.",
                Severity = AccessibilityViolationSeverity.Warning,
                Suggestion = "Set DisplayDocTitle to true."
            });
        }
        else
        {
            passedCount++;
        }

        // 4. Recursive Structural & WCAG element check
        int lastHeadingLevel = 0;
        AuditElementHierarchy(root, violations, ref passedCount, ref lastHeadingLevel);

        // 5. Color Contrast Check across report elements if definition provided
        if (reportDef != null && opts.EnforceColorContrast)
        {
            AuditReportColorContrast(reportDef, opts, violations, ref passedCount);
        }

        int errorCount = 0;
        int warningCount = 0;
        foreach (var v in violations)
        {
            if (v.Severity == AccessibilityViolationSeverity.Error) errorCount++;
            else if (v.Severity == AccessibilityViolationSeverity.Warning) warningCount++;
        }

        int totalChecks = passedCount + warningCount + errorCount;
        double score = totalChecks > 0
            ? Math.Round(Math.Max(0.0, 100.0 - (errorCount * 20.0) - (warningCount * 5.0)), 1)
            : 100.0;

        return new AccessibilityAuditReport
        {
            PassedCount = passedCount,
            WarningCount = warningCount,
            ErrorCount = errorCount,
            Score = score,
            Violations = violations
        };
    }

    private static void AuditElementHierarchy(
        PdfStructElement elem,
        List<AccessibilityViolation> violations,
        ref int passedCount,
        ref int lastHeadingLevel)
    {
        // Check Figure Alt Text (WCAG 1.1.1 Non-Text Content)
        if (elem.TagType == PdfTagType.Figure)
        {
            if (string.IsNullOrWhiteSpace(elem.AltText))
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "WCAG-1.1.1-ALT",
                    Message = $"Figure element '{elem.Id ?? "unnamed"}' is missing alternative text (/Alt).",
                    Severity = AccessibilityViolationSeverity.Error,
                    ElementId = elem.Id,
                    Suggestion = "Provide a meaningful AltText describing the graphic or chart."
                });
            }
            else
            {
                passedCount++;
            }
        }

        // Check Heading Hierarchy (WCAG 1.3.1 Info and Relationships)
        int currentHeadingLevel = elem.TagType switch
        {
            PdfTagType.H1 => 1,
            PdfTagType.H2 => 2,
            PdfTagType.H3 => 3,
            PdfTagType.H4 => 4,
            PdfTagType.H5 => 5,
            PdfTagType.H6 => 6,
            _ => 0
        };

        if (currentHeadingLevel > 0)
        {
            if (lastHeadingLevel > 0 && currentHeadingLevel > lastHeadingLevel + 1)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "WCAG-1.3.1-HEADING",
                    Message = $"Skipped heading level: from H{lastHeadingLevel} directly to H{currentHeadingLevel}.",
                    Severity = AccessibilityViolationSeverity.Warning,
                    ElementId = elem.Id,
                    Suggestion = $"Use H{lastHeadingLevel + 1} instead of H{currentHeadingLevel} to preserve semantic hierarchy."
                });
            }
            else
            {
                passedCount++;
            }
            lastHeadingLevel = currentHeadingLevel;
        }

        // Check Table Headers
        if (elem.TagType == PdfTagType.TableHeader)
        {
            if (elem.Scope == TableScopeType.None)
            {
                violations.Add(new AccessibilityViolation
                {
                    RuleId = "WCAG-1.3.1-TH-SCOPE",
                    Message = $"Table header cell '{elem.Id ?? "th"}' is missing scope (/Scope).",
                    Severity = AccessibilityViolationSeverity.Warning,
                    ElementId = elem.Id,
                    Suggestion = "Assign TableScopeType.Column or TableScopeType.Row."
                });
            }
            else
            {
                passedCount++;
            }
        }

        foreach (var child in elem.Children)
        {
            AuditElementHierarchy(child, violations, ref passedCount, ref lastHeadingLevel);
        }
    }

    private static void AuditReportColorContrast(
        ReportDefinition reportDef,
        AccessibilityOptions opts,
        List<AccessibilityViolation> violations,
        ref int passedCount)
    {
        var allBands = new List<BandDefinition>();
        if (reportDef.Bands.ReportHeader != null) allBands.Add(reportDef.Bands.ReportHeader);
        if (reportDef.Bands.PageHeader != null) allBands.Add(reportDef.Bands.PageHeader);
        foreach (var g in reportDef.Bands.GroupHeaders) allBands.Add(new BandDefinition { Height = g.Height, Elements = g.Elements });
        if (reportDef.Bands.Detail != null) allBands.Add(reportDef.Bands.Detail);
        foreach (var g in reportDef.Bands.GroupFooters) allBands.Add(new BandDefinition { Height = g.Height, Elements = g.Elements });
        if (reportDef.Bands.PageFooter != null) allBands.Add(reportDef.Bands.PageFooter);
        if (reportDef.Bands.ReportFooter != null) allBands.Add(reportDef.Bands.ReportFooter);

        foreach (var band in allBands)
        {
            string bandBg = "#FFFFFF";

            foreach (var el in band.Elements)
            {
                if (el.Type == ElementType.Text)
                {
                    string fg = el.Style?.Color ?? "#000000";
                    string bg = el.Style?.BackgroundColor ?? bandBg;

                    var contrast = ColorContrastAuditor.CalculateContrast(fg, bg);
                    if (contrast.Ratio < opts.MinContrastRatio)
                    {
                        violations.Add(new AccessibilityViolation
                        {
                            RuleId = "WCAG-1.4.3-CONTRAST",
                            Message = $"Element '{el.Id}' has low contrast ratio ({contrast.Ratio}:1 < {opts.MinContrastRatio}:1) between text {fg} and background {bg}.",
                            Severity = AccessibilityViolationSeverity.Warning,
                            ElementId = el.Id,
                            Suggestion = $"Adjust text or background color to achieve at least {opts.MinContrastRatio}:1 contrast."
                        });
                    }
                    else
                    {
                        passedCount++;
                    }
                }
            }
        }
    }
}