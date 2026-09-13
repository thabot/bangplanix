using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bangplanix.Core.Models;

public enum PageNumberingStyle
{
    Continuous,
    PerSection
}

public sealed class CoverPageDefinition
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    [JsonPropertyName("preparedFor")]
    public string? PreparedFor { get; set; }

    [JsonPropertyName("preparedBy")]
    public string? PreparedBy { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("logoSource")]
    public string? LogoSource { get; set; }

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; } = "#1e3a8a";

    [JsonPropertyName("textColor")]
    public string? TextColor { get; set; } = "#ffffff";
}

public sealed class TocDefinition
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "สารบัญ (Table of Contents)";

    [JsonPropertyName("leaderChar")]
    public string LeaderChar { get; set; } = ".";

    [JsonPropertyName("showPageNumbers")]
    public bool ShowPageNumbers { get; set; } = true;

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "Sarabun";

    [JsonPropertyName("fontSize")]
    public float FontSize { get; set; } = 11f;
}

public sealed class PdfBookmarkNode
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; } = 1;

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; } = true;

    [JsonPropertyName("children")]
    public List<PdfBookmarkNode> Children { get; set; } = [];
}

public sealed class SectionDividerDefinition
{
    [JsonPropertyName("chapterNumber")]
    public string? ChapterNumber { get; set; } = "01";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("backgroundColor")]
    public string? BackgroundColor { get; set; } = "#0f172a";

    [JsonPropertyName("accentColor")]
    public string? AccentColor { get; set; } = "#3b82f6";
}

public sealed class HeaderFooterOptions
{
    [JsonPropertyName("showHeader")]
    public bool ShowHeader { get; set; } = true;

    [JsonPropertyName("showFooter")]
    public bool ShowFooter { get; set; } = true;

    [JsonPropertyName("headerLeft")]
    public string? HeaderLeft { get; set; } = "{DossierTitle}";

    [JsonPropertyName("headerRight")]
    public string? HeaderRight { get; set; } = "{SectionTitle}";

    [JsonPropertyName("footerLeft")]
    public string? FooterLeft { get; set; } = "Confidential & Proprietary";

    [JsonPropertyName("footerRight")]
    public string? FooterRight { get; set; } = "หน้า {PageNumber} จาก {TotalPages}";

    [JsonPropertyName("suppressOnCover")]
    public bool SuppressOnCover { get; set; } = true;

    [JsonPropertyName("suppressOnToc")]
    public bool SuppressOnToc { get; set; } = true;

    [JsonPropertyName("suppressOnDivider")]
    public bool SuppressOnDivider { get; set; } = true;
}

public sealed class PdfAttachmentDefinition
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; } = "application/octet-stream";

    [JsonPropertyName("data")]
    public byte[] Data { get; set; } = [];
}

public sealed class PdfLinkAnnotation
{
    [JsonPropertyName("rect")]
    public double[] Rect { get; set; } = [0, 0, 0, 0]; // [x1, y1, x2, y2]

    [JsonPropertyName("targetPageNumber")]
    public int? TargetPageNumber { get; set; }

    [JsonPropertyName("uri")]
    public string? Uri { get; set; }
}

public sealed class DossierSectionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("divider")]
    public SectionDividerDefinition? Divider { get; set; }

    [JsonPropertyName("report")]
    public ReportDefinition? Report { get; set; }

    [JsonPropertyName("reportPath")]
    public string? ReportPath { get; set; }

    [JsonPropertyName("parameters")]
    public Dictionary<string, object?> Parameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("dataRows")]
    public List<IDictionary<string, object?>> DataRows { get; set; } = [];

    [JsonPropertyName("includeInToc")]
    public bool IncludeInToc { get; set; } = true;

    [JsonPropertyName("tocTitle")]
    public string? TocTitle { get; set; }

    [JsonPropertyName("bookmarks")]
    public List<PdfBookmarkNode> Bookmarks { get; set; } = [];

    [JsonPropertyName("links")]
    public List<PdfLinkAnnotation> Links { get; set; } = [];
}

public sealed class DossierDefinition
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "Executive Dossier Report";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Bangplanix";

    [JsonPropertyName("pageSetup")]
    public PageSetup PageSetup { get; set; } = new() { Width = 595.28, Height = 841.89, Unit = UnitType.Pt }; // A4 Portrait

    [JsonPropertyName("coverPage")]
    public CoverPageDefinition? CoverPage { get; set; }

    [JsonPropertyName("tableOfContents")]
    public TocDefinition? TableOfContents { get; set; }

    [JsonPropertyName("headerFooter")]
    public HeaderFooterOptions? HeaderFooter { get; set; }

    [JsonPropertyName("pageNumberingStyle")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PageNumberingStyle PageNumberingStyle { get; set; } = PageNumberingStyle.Continuous;

    [JsonPropertyName("pageNumberFormat")]
    public string PageNumberFormat { get; set; } = "หน้า {0} จาก {1}";

    [JsonPropertyName("sections")]
    public List<DossierSectionDefinition> Sections { get; set; } = [];

    [JsonPropertyName("attachments")]
    public List<PdfAttachmentDefinition> Attachments { get; set; } = [];

    [JsonPropertyName("globalBookmarks")]
    public List<PdfBookmarkNode> GlobalBookmarks { get; set; } = [];
}
