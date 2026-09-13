using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Bangplanix.Engine.Seo;

public static class SitemapGenerator
{
    public static string GenerateSitemapXml(IEnumerable<SitemapItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Location)) continue;

            sb.AppendLine("  <url>");
            sb.Append("    <loc>").Append(WebUtility.HtmlEncode(item.Location)).AppendLine("</loc>");

            if (item.LastModified.HasValue)
            {
                sb.Append("    <lastmod>").Append(item.LastModified.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).AppendLine("</lastmod>");
            }

            if (!string.IsNullOrWhiteSpace(item.ChangeFrequency))
            {
#pragma warning disable CA1308 // Normalize strings to uppercase (Sitemap XML specs require lowercase)
                sb.Append("    <changefreq>").Append(item.ChangeFrequency.ToLowerInvariant()).AppendLine("</changefreq>");
#pragma warning restore CA1308
            }

            sb.Append("    <priority>").Append(item.Priority.ToString("F1", CultureInfo.InvariantCulture)).AppendLine("</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        return sb.ToString().TrimEnd();
    }
}
