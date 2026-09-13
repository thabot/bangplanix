using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Visuals.Charts;

public static class SvgChartExporter
{
    public static string ExportToSvg(ChartDefinition chart, float width = 600, float height = 400)
    {
        if (chart == null) return "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>";

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\" class=\"bpx-chart-svg\">");
        sb.AppendLine("  <style>");
        sb.AppendLine("    .bpx-chart-node { cursor: pointer; transition: opacity 0.2s ease, transform 0.2s ease; }");
        sb.AppendLine("    .bpx-chart-node:hover { opacity: 0.85; filter: drop-shadow(0 2px 4px rgba(0,0,0,0.15)); }");
        sb.AppendLine("    .bpx-axis-text { font-family: 'Sarabun', sans-serif; font-size: 11px; fill: #64748b; }");
        sb.AppendLine("    .bpx-title-text { font-family: 'Sarabun', sans-serif; font-size: 14px; font-weight: bold; fill: #0f172a; }");
        sb.AppendLine("  </style>");

        // Title
        if (!string.IsNullOrEmpty(chart.Title))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <text x=\"{width / 2f}\" y=\"24\" text-anchor=\"middle\" class=\"bpx-title-text\">{SecurityElement.Escape(chart.Title)}</text>");
        }

        var plotX = 60f;
        var plotY = 40f;
        var plotW = width - 80f;
        var plotH = height - 80f;

        if (chart.ChartType is ChartType.Pie or ChartType.Doughnut)
        {
            RenderPieDoughnutSvg(sb, chart, width / 2f, height / 2f + 10f, Math.Min(plotW, plotH) / 2.2f);
        }
        else
        {
            RenderBarColumnSvg(sb, chart, plotX, plotY, plotW, plotH);
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void RenderBarColumnSvg(StringBuilder sb, ChartDefinition chart, float px, float py, float pw, float ph)
    {
        var categoryCount = chart.Categories.Count > 0 ? chart.Categories.Count : chart.Series.Select(s => s.Values.Count).DefaultIfEmpty(1).Max();
        if (categoryCount == 0 || chart.Series.Count == 0) return;

        var maxVal = chart.Series.SelectMany(s => s.Values).DefaultIfEmpty(10.0).Max();
        if (maxVal <= 0.0001) maxVal = 10.0;

        var groupWidth = pw / categoryCount;
        var barWidth = (groupWidth * 0.7f) / chart.Series.Count;

        for (int c = 0; c < categoryCount; c++)
        {
            var catName = c < chart.Categories.Count ? chart.Categories[c] : $"Category {c + 1}";
            var groupLeft = px + (c * groupWidth) + (groupWidth * 0.15f);

            for (int s = 0; s < chart.Series.Count; s++)
            {
                var series = chart.Series[s];
                if (c >= series.Values.Count) continue;

                var val = series.Values[c];
                var h = (float)(val / maxVal) * ph;
                var x = groupLeft + (s * barWidth);
                var y = py + ph - h;
                var color = ChartColorPalette.GetColor(chart.Palette, s, chart.CustomColors);
                var hexColor = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

                var tooltip = $"{series.Name} - {catName}: {val:N0}";
                sb.AppendLine(CultureInfo.InvariantCulture, $"  <g class=\"bpx-chart-node\" data-tooltip=\"{SecurityElement.Escape(tooltip)}\">");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <title>{SecurityElement.Escape(tooltip)}</title>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <rect x=\"{x:F1}\" y=\"{y:F1}\" width=\"{barWidth - 2f:F1}\" height=\"{h:F1}\" rx=\"2\" fill=\"{hexColor}\" />");
                sb.AppendLine("  </g>");
            }

            // X Axis Category Label
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <text x=\"{groupLeft + (groupWidth * 0.35f):F1}\" y=\"{py + ph + 18f:F1}\" text-anchor=\"middle\" class=\"bpx-axis-text\">{SecurityElement.Escape(catName)}</text>");
        }
    }

    private static void RenderPieDoughnutSvg(StringBuilder sb, ChartDefinition chart, float cx, float cy, float radius)
    {
        if (chart.Series.Count == 0 || chart.Series[0].Values.Count == 0) return;

        var series = chart.Series[0];
        var total = series.Values.Sum();
        if (total <= 0.0001) total = 1.0;

        var currentAngle = -90.0;

        for (int i = 0; i < series.Values.Count; i++)
        {
            var val = series.Values[i];
            var sweep = (val / total) * 360.0;
            if (sweep <= 0.001) continue;

            var startRad = currentAngle * Math.PI / 180.0;
            var endRad = (currentAngle + sweep) * Math.PI / 180.0;

            var x1 = cx + (radius * Math.Cos(startRad));
            var y1 = cy + (radius * Math.Sin(startRad));
            var x2 = cx + (radius * Math.Cos(endRad));
            var y2 = cy + (radius * Math.Sin(endRad));

            var largeArc = sweep > 180.0 ? 1 : 0;
            var pathData = $"M {cx:F1} {cy:F1} L {x1:F1} {y1:F1} A {radius:F1} {radius:F1} 0 {largeArc} 1 {x2:F1} {y2:F1} Z";

            var catName = i < chart.Categories.Count ? chart.Categories[i] : $"Item {i + 1}";
            var percentage = (val / total) * 100.0;
            var color = ChartColorPalette.GetColor(chart.Palette, i, chart.CustomColors);
            var hexColor = $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";

            var tooltip = $"{catName}: {val:N0} ({percentage:F1}%)";
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <g class=\"bpx-chart-node\" data-tooltip=\"{SecurityElement.Escape(tooltip)}\">");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <title>{SecurityElement.Escape(tooltip)}</title>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <path d=\"{pathData}\" fill=\"{hexColor}\" stroke=\"#ffffff\" stroke-width=\"1.5\" />");
            sb.AppendLine("  </g>");

            currentAngle += sweep;
        }
    }
}
