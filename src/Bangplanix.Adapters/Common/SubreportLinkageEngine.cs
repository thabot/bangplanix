using System.Collections.ObjectModel;
using System.Xml.Linq;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Common;

/// <summary>
/// Metadata defining a linked subreport and its parameter bindings between Master and Detail.
/// </summary>
public sealed class SubreportLinkageInfo
{
    public string SubreportName { get; set; } = string.Empty;
    public string ReportPath { get; set; } = string.Empty;
    public string? MasterDatasetName { get; set; }
    public string? ChildDatasetName { get; set; }
    public Dictionary<string, string> ParameterBindings { get; } = new(StringComparer.OrdinalIgnoreCase);
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 100;
}

/// <summary>
/// Engine for detecting, extracting, and mapping Master-Detail subreports across legacy report definitions.
/// </summary>
public static class SubreportLinkageEngine
{
    /// <summary>
    /// Detects all subreport elements in an XML document across legacy dialects (Crystal, BIRT, ActiveReports, SSRS).
    /// </summary>
    public static ReadOnlyCollection<SubreportLinkageInfo> ExtractSubreports(XDocument doc)
    {
        var result = new List<SubreportLinkageInfo>();
        if (doc?.Root == null) return result.AsReadOnly();

        var visitedElements = new HashSet<XElement>();

        // Query all subreport-like nodes in one pass
        var subElements = doc.Descendants().Where(e =>
            string.Equals(e.Name.LocalName, "SubreportObject", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "Subreport", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "SubReport", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "AR.Subreport", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "subreport", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Name.LocalName, "include-report", StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(e.Name.LocalName, "Control", StringComparison.OrdinalIgnoreCase) &&
             e.Attribute("Type")?.Value.Contains("Subreport", StringComparison.OrdinalIgnoreCase) == true));

        foreach (var sub in subElements)
        {
            if (!visitedElements.Add(sub)) continue;

            var name = sub.Attribute("Name")?.Value ??
                       sub.Attribute("name")?.Value ??
                       sub.Element("Name")?.Value ??
                       sub.Element(sub.Name.Namespace + "Name")?.Value ??
                       "Subreport";

            var reportPath = sub.Attribute("ReportName")?.Value ??
                             sub.Attribute("SubreportName")?.Value ??
                             sub.Attribute("report-name")?.Value ??
                             sub.Attribute("Report")?.Value ??
                             sub.Element("ReportName")?.Value ??
                             sub.Element("report-name")?.Value ??
                             sub.Element(sub.Name.Namespace + "ReportName")?.Value ??
                             string.Empty;

            var info = new SubreportLinkageInfo
            {
                SubreportName = name,
                ReportPath = reportPath
            };

            // 1. Crystal / ActiveReports SubreportLink
            foreach (var link in sub.Descendants().Where(e =>
                string.Equals(e.Name.LocalName, "SubreportLink", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Name.LocalName, "ParameterLink", StringComparison.OrdinalIgnoreCase)))
            {
                var parentField = link.Attribute("MainReportFieldName")?.Value ?? link.Attribute("MasterField")?.Value ?? link.Element("MasterField")?.Value;
                var childParam = link.Attribute("SubreportParameterName")?.Value ?? link.Attribute("ChildParameter")?.Value ?? link.Element("ChildParameter")?.Value;

                if (!string.IsNullOrEmpty(parentField) && !string.IsNullOrEmpty(childParam))
                {
                    info.ParameterBindings[childParam] = LegacyExpressionTranspiler.Transpile(parentField, "Crystal");
                }
            }

            // 2. BIRT param-binding
            foreach (var param in sub.Descendants().Where(e => string.Equals(e.Name.LocalName, "param-binding", StringComparison.OrdinalIgnoreCase)))
            {
                var paramName = param.Attribute("name")?.Value ?? param.Element("name")?.Value;
                var expr = param.Element("expression")?.Value ?? param.Value;

                if (!string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(expr))
                {
                    info.ParameterBindings[paramName] = LegacyExpressionTranspiler.Transpile(expr, "Birt");
                }
            }

            // 3. SSRS Parameter
            foreach (var param in sub.Descendants().Where(e => string.Equals(e.Name.LocalName, "Parameter", StringComparison.OrdinalIgnoreCase)))
            {
                var pName = param.Attribute("Name")?.Value;
                var pVal = param.Element(param.Name.Namespace + "Value")?.Value;
                if (!string.IsNullOrEmpty(pName) && !string.IsNullOrEmpty(pVal))
                {
                    info.ParameterBindings[pName] = LegacyExpressionTranspiler.Transpile(pVal, "SSRS");
                }
            }

            result.Add(info);
        }

        return result.AsReadOnly();
    }

    /// <summary>
    /// Creates a placeholder element representing a linked subreport inside a Bangplanix Band layout.
    /// </summary>
    public static ElementDefinition CreateSubreportPlaceholder(SubreportLinkageInfo linkage)
    {
        ArgumentNullException.ThrowIfNull(linkage);

        var bindingDescription = string.Join(", ", linkage.ParameterBindings.Select(kv => $"{kv.Key} = {kv.Value}"));
        return new ElementDefinition
        {
            Type = ElementType.Text,
            Id = $"Subreport_{linkage.SubreportName}",
            X = linkage.X,
            Y = linkage.Y,
            Width = linkage.Width,
            Height = linkage.Height,
            Text = $"[Subreport: {linkage.SubreportName} ({linkage.ReportPath})] {(string.IsNullOrEmpty(bindingDescription) ? "" : $"Bindings: {bindingDescription}")}",
            Style = new StyleDefinition
            {
                FontFamily = "Sarabun",
                FontSize = 9.0,
                FontWeight = "Bold",
                Color = "#1E40AF",
                BackgroundColor = "#EFF6FF",
                Border = new BorderDefinition { Top = "#3B82F6", Bottom = "#3B82F6", Left = "#3B82F6", Right = "#3B82F6", Width = 1.0 }
            }
        };
    }
}
