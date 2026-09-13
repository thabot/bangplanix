using System;
using System.Collections.Generic;

namespace Bangplanix.Engine.Seo;

public enum RobotsPolicy
{
    IndexFollow,
    NoIndexFollow,
    IndexNoFollow,
    NoIndexNoFollow
}

#pragma warning disable CA1056 // URI-like properties should not be strings
public class ReportSeoMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string OgImageUrl { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public RobotsPolicy Robots { get; set; } = RobotsPolicy.IndexFollow;
    public string SchemaType { get; set; } = "Report"; // Invoice, FinancialReport, Receipt, Report
    public DateTime? PublishedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Dictionary<string, object> CustomProperties { get; } = new();
}
#pragma warning restore CA1056

public class SitemapItem
{
    public string Location { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
    public string ChangeFrequency { get; set; } = "daily"; // always, hourly, daily, weekly, monthly, yearly, never
    public double Priority { get; set; } = 0.8;
}
