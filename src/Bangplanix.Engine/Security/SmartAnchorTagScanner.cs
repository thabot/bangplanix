using System.Text.RegularExpressions;
using Bangplanix.Core.Models;
using Bangplanix.Core.Security;

namespace Bangplanix.Engine.Security;

public class LocatedAnchorTag
{
    public string TagName { get; set; } = string.Empty;
    public string TagArgument { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string BandName { get; set; } = string.Empty;
}

/// <summary>
/// Scans report templates and bands to locate smart anchor text tags like [[SIG:Approver]], [[DATE]], [[SEAL]]
/// and computes precise Bounding Boxes for dynamic signature placement.
/// </summary>
public static class SmartAnchorTagScanner
{
    private static readonly Regex AnchorRegex = new(@"\[\[(SIG(?::[A-Za-z0-9_]+)?|DATE|SEAL(?::[A-Za-z0-9_]+)?)\]\]", RegexOptions.Compiled);

    /// <summary>
    /// Scans band elements for anchor tags and converts matches to VisualSignatureAppearance.
    /// </summary>
    public static List<LocatedAnchorTag> ScanAnchorTags(ReportDefinition report, AnchorTagPlacementOptions? defaultOptions = null, bool maskTagInReport = false)
    {
        ArgumentNullException.ThrowIfNull(report);
        var results = new List<LocatedAnchorTag>();

        if (report.Bands == null) return results;

        double currentYOffset = 0;
        int pageNumber = 1;

        void InspectBand(string bandName, BandDefinition? band)
        {
            if (band?.Elements == null) return;

            foreach (var element in band.Elements)
            {
                var candidateText = element.Text ?? element.Expression ?? string.Empty;
                if (string.IsNullOrEmpty(candidateText)) continue;

                var matches = AnchorRegex.Matches(candidateText);
                foreach (Match match in matches)
                {
                    var fullTag = match.Value;
                    var inner = match.Groups[1].Value;
                    var parts = inner.Split(':');
                    var tagType = parts[0];
                    var tagArg = parts.Length > 1 ? parts[1] : string.Empty;

                    var boxWidth = defaultOptions?.SignatureBoxWidth ?? (element.Width > 0 ? element.Width : 150);
                    var boxHeight = defaultOptions?.SignatureBoxHeight ?? (element.Height > 0 ? element.Height : 60);

                    results.Add(new LocatedAnchorTag
                    {
                        TagName = fullTag,
                        TagArgument = tagArg,
                        PageNumber = pageNumber,
                        X = element.X,
                        Y = currentYOffset + element.Y,
                        Width = boxWidth,
                        Height = boxHeight,
                        BandName = bandName
                    });

                    // Hide original text tag in element so it does not render raw tag text if requested
                    if (maskTagInReport && element.Text != null && element.Text.Contains(fullTag))
                    {
                        element.Text = element.Text.Replace(fullTag, string.Empty).Trim();
                    }
                }
            }

            currentYOffset += band.Height;
        }

        InspectBand("ReportHeader", report.Bands.ReportHeader);
        InspectBand("PageHeader", report.Bands.PageHeader);
        InspectBand("Detail", report.Bands.Detail);
        InspectBand("ReportFooter", report.Bands.ReportFooter);
        InspectBand("PageFooter", report.Bands.PageFooter);

        return results;
    }

    /// <summary>
    /// Resolves VisualSignatureAppearance from an anchor tag specification.
    /// </summary>
    public static VisualSignatureAppearance? ResolveAppearanceFromAnchor(
        ReportDefinition report,
        AnchorTagPlacementOptions options)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(options);

        var located = ScanAnchorTags(report, options);
        var match = located.FirstOrDefault(l => string.Equals(l.TagName, options.AnchorTagName, StringComparison.OrdinalIgnoreCase))
                 ?? located.FirstOrDefault();

        if (match == null) return null;

        return new VisualSignatureAppearance
        {
            PageNumber = match.PageNumber,
            X = match.X,
            Y = match.Y,
            Width = options.SignatureBoxWidth > 0 ? options.SignatureBoxWidth : match.Width,
            Height = options.SignatureBoxHeight > 0 ? options.SignatureBoxHeight : match.Height,
            Title = options.Title ?? (string.IsNullOrEmpty(match.TagArgument) ? "Authorized Signatory" : match.TagArgument),
            SignatureImageBase64 = options.SignatureImageBase64,
            ShowSignerDetails = true
        };
    }
}
