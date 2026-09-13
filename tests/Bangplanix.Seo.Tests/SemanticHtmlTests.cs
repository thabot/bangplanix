using System.Collections.Generic;
using Bangplanix.Core.Models;
using Bangplanix.Engine.Seo;
using Xunit;

namespace Bangplanix.Seo.Tests;

public class SemanticHtmlTests
{
    [Fact]
    public void RenderToSemanticHtmlShouldContainSemanticHtml5TagsAndAriaAttributes()
    {
        var report = new ReportDefinition
        {
            Version = "1.0.0",
            Metadata = new ReportMetadata
            {
                Title = "Monthly Performance Report",
                Description = "Executive Sales & Operational Metrics",
                Author = "Acme Corp"
            },
            Datasets = new List<DatasetDefinition>
            {
                new() { Name = "sales" }
            },
            Bands = new BandsDefinition
            {
                ReportHeader = new BandDefinition
                {
                    Height = 40,
                    Elements = new List<ElementDefinition>
                    {
                        new()
                        {
                            Type = ElementType.Text,
                            Text = "Monthly Performance Report",
                            Style = new StyleDefinition { FontSize = 20 }
                        }
                    }
                },
                Detail = new BandDefinition
                {
                    Height = 25,
                    Elements = new List<ElementDefinition>
                    {
                        new()
                        {
                            Type = ElementType.Text,
                            Expression = "=Fields!Item.Value"
                        },
                        new()
                        {
                            Type = ElementType.Text,
                            Expression = "=Fields!Amount.Value",
                            Style = new StyleDefinition { Align = HorizontalAlign.Right }
                        }
                    }
                }
            }
        };

        var data = new Dictionary<string, object>
        {
            ["sales"] = new List<Dictionary<string, object>>
            {
                new() { ["Item"] = "Enterprise Server", ["Amount"] = "50000" },
                new() { ["Item"] = "Cloud Database", ["Amount"] = "15000" }
            }
        };

        var html = SemanticHtmlRenderer.RenderToSemanticHtml(report, data);

        Assert.Contains("<!DOCTYPE html>", html, System.StringComparison.Ordinal);
        Assert.Contains("<article class=\"bpx-report-container\" role=\"document\"", html, System.StringComparison.Ordinal);
        Assert.Contains("<header class=\"bpx-header\" role=\"banner\">", html, System.StringComparison.Ordinal);
        Assert.Contains("<main class=\"bpx-main\" role=\"main\">", html, System.StringComparison.Ordinal);
        Assert.Contains("<table class=\"bpx-table\" role=\"table\">", html, System.StringComparison.Ordinal);
        Assert.Contains("<th scope=\"col\"", html, System.StringComparison.Ordinal);
        Assert.Contains("<footer class=\"bpx-footer\" role=\"contentinfo\">", html, System.StringComparison.Ordinal);
        Assert.Contains("Enterprise Server", html, System.StringComparison.Ordinal);
        Assert.Contains("50000", html, System.StringComparison.Ordinal);
        Assert.Contains("Cloud Database", html, System.StringComparison.Ordinal);
        Assert.Contains("15000", html, System.StringComparison.Ordinal);
    }
}
