using System;
using System.Net;
using System.Text;

namespace Bangplanix.Engine.Seo;

public static class OpenGraphGenerator
{
    public static string GenerateMetaTags(ReportSeoMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            var title = WebUtility.HtmlEncode(metadata.Title);
            sb.Append("<title>").Append(title).AppendLine("</title>");
            sb.Append("<meta property=\"og:title\" content=\"").Append(title).AppendLine("\" />");
            sb.Append("<meta name=\"twitter:title\" content=\"").Append(title).AppendLine("\" />");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Description))
        {
            var desc = WebUtility.HtmlEncode(metadata.Description);
            sb.Append("<meta name=\"description\" content=\"").Append(desc).AppendLine("\" />");
            sb.Append("<meta property=\"og:description\" content=\"").Append(desc).AppendLine("\" />");
            sb.Append("<meta name=\"twitter:description\" content=\"").Append(desc).AppendLine("\" />");
        }

        if (!string.IsNullOrWhiteSpace(metadata.CanonicalUrl))
        {
            var canonical = WebUtility.HtmlEncode(metadata.CanonicalUrl);
            sb.Append("<link rel=\"canonical\" href=\"").Append(canonical).AppendLine("\" />");
            sb.Append("<meta property=\"og:url\" content=\"").Append(canonical).AppendLine("\" />");
        }

        if (!string.IsNullOrWhiteSpace(metadata.OgImageUrl))
        {
            var ogImg = WebUtility.HtmlEncode(metadata.OgImageUrl);
            sb.Append("<meta property=\"og:image\" content=\"").Append(ogImg).AppendLine("\" />");
            sb.Append("<meta name=\"twitter:image\" content=\"").Append(ogImg).AppendLine("\" />");
            sb.AppendLine("<meta name=\"twitter:card\" content=\"summary_large_image\" />");
        }
        else
        {
            sb.AppendLine("<meta name=\"twitter:card\" content=\"summary\" />");
        }

        sb.AppendLine("<meta property=\"og:type\" content=\"article\" />");

        if (!string.IsNullOrWhiteSpace(metadata.Author))
        {
            sb.Append("<meta name=\"author\" content=\"").Append(WebUtility.HtmlEncode(metadata.Author)).AppendLine("\" />");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Keywords))
        {
            sb.Append("<meta name=\"keywords\" content=\"").Append(WebUtility.HtmlEncode(metadata.Keywords)).AppendLine("\" />");
        }

        var robotsContent = metadata.Robots switch
        {
            RobotsPolicy.IndexFollow => "index, follow",
            RobotsPolicy.NoIndexFollow => "noindex, follow",
            RobotsPolicy.IndexNoFollow => "index, nofollow",
            RobotsPolicy.NoIndexNoFollow => "noindex, nofollow",
            _ => "index, follow"
        };
        sb.Append("<meta name=\"robots\" content=\"").Append(robotsContent).AppendLine("\" />");

        return sb.ToString().TrimEnd();
    }
}
