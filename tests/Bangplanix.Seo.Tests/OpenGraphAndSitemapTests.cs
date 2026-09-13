using System;
using System.Collections.Generic;
using Bangplanix.Engine.Seo;
using Xunit;

namespace Bangplanix.Seo.Tests;

public class OpenGraphAndSitemapTests
{
    [Fact]
    public void GenerateMetaTagsShouldOutputOpenGraphTwitterAndRobotsTags()
    {
        var metadata = new ReportSeoMetadata
        {
            Title = "Bangplanix Annual Financial Statement 2026",
            Description = "Audited consolidated financial statements and balance sheet.",
            CanonicalUrl = "https://bangplanix.io/reports/annual-2026",
            OgImageUrl = "https://bangplanix.io/reports/annual-2026/preview.webp",
            Author = "Bangplanix Finance",
            Keywords = "finance, report, enterprise, audit",
            Robots = RobotsPolicy.IndexFollow
        };

        var meta = OpenGraphGenerator.GenerateMetaTags(metadata);

        Assert.Contains("<title>Bangplanix Annual Financial Statement 2026</title>", meta, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:title\" content=\"Bangplanix Annual Financial Statement 2026\" />", meta, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:image\" content=\"https://bangplanix.io/reports/annual-2026/preview.webp\" />", meta, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"twitter:card\" content=\"summary_large_image\" />", meta, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"canonical\" href=\"https://bangplanix.io/reports/annual-2026\" />", meta, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"robots\" content=\"index, follow\" />", meta, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateSitemapXmlShouldProduceValidXmlSitemapStructure()
    {
        var items = new List<SitemapItem>
        {
            new()
            {
                Location = "https://bangplanix.io/reports/sales-q3",
                LastModified = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc),
                ChangeFrequency = "weekly",
                Priority = 0.9
            },
            new()
            {
                Location = "https://bangplanix.io/reports/inventory-live",
                ChangeFrequency = "hourly",
                Priority = 1.0
            }
        };

        var sitemap = SitemapGenerator.GenerateSitemapXml(items);

        Assert.Contains("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", sitemap, StringComparison.Ordinal);
        Assert.Contains("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">", sitemap, StringComparison.Ordinal);
        Assert.Contains("<loc>https://bangplanix.io/reports/sales-q3</loc>", sitemap, StringComparison.Ordinal);
        Assert.Contains("<lastmod>2026-09-12T12:00:00Z</lastmod>", sitemap, StringComparison.Ordinal);
        Assert.Contains("<changefreq>weekly</changefreq>", sitemap, StringComparison.Ordinal);
        Assert.Contains("<priority>0.9</priority>", sitemap, StringComparison.Ordinal);
        Assert.Contains("<loc>https://bangplanix.io/reports/inventory-live</loc>", sitemap, StringComparison.Ordinal);
        Assert.Contains("<priority>1.0</priority>", sitemap, StringComparison.Ordinal);
    }
}
