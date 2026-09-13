using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Bangplanix.Engine.Seo;

public static class JsonLdGenerator
{
    public static string GenerateJsonLd(ReportSeoMetadata metadata, Dictionary<string, object>? data = null)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("@context", "https://schema.org");

            var schemaType = metadata.SchemaType switch
            {
                "Invoice" => "Invoice",
                "Receipt" => "Order",
                "FinancialReport" => "FinancialProduct",
                _ => "Report"
            };
            writer.WriteString("@type", schemaType);

            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                writer.WriteString("name", metadata.Title);
                writer.WriteString("headline", metadata.Title);
            }

            if (!string.IsNullOrWhiteSpace(metadata.Description))
            {
                writer.WriteString("description", metadata.Description);
            }

            if (!string.IsNullOrWhiteSpace(metadata.CanonicalUrl))
            {
                writer.WriteString("url", metadata.CanonicalUrl);
            }

            if (!string.IsNullOrWhiteSpace(metadata.OgImageUrl))
            {
                writer.WriteString("image", metadata.OgImageUrl);
            }

            if (metadata.PublishedAt.HasValue)
            {
                writer.WriteString("datePublished", metadata.PublishedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (metadata.ModifiedAt.HasValue)
            {
                writer.WriteString("dateModified", metadata.ModifiedAt.Value.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(metadata.Author))
            {
                writer.WriteStartObject("author");
                writer.WriteString("@type", "Organization");
                writer.WriteString("name", metadata.Author);
                writer.WriteEndObject();
            }

            if (metadata.SchemaType.Equals("Invoice", StringComparison.OrdinalIgnoreCase) && data != null)
            {
                if (data.TryGetValue("invoiceNumber", out var invNum) && invNum != null)
                {
                    writer.WriteString("identifier", invNum.ToString());
                }

                if (data.TryGetValue("totalAmount", out var total) && total != null)
                {
                    writer.WriteStartObject("totalPaymentDue");
                    writer.WriteString("@type", "MonetaryAmount");
                    var curr = data.TryGetValue("currency", out var c) && c != null ? c.ToString() : "THB";
                    writer.WriteString("currency", curr);
                    if (total is double d) writer.WriteNumber("value", d);
                    else if (total is int i) writer.WriteNumber("value", i);
                    else if (total is decimal dec) writer.WriteNumber("value", dec);
                    else if (double.TryParse(total.ToString(), out var parsedVal)) writer.WriteNumber("value", parsedVal);
                    writer.WriteEndObject();
                }

                if (data.TryGetValue("customerName", out var customer) && customer != null)
                {
                    writer.WriteStartObject("customer");
                    writer.WriteString("@type", "Person");
                    writer.WriteString("name", customer.ToString());
                    writer.WriteEndObject();
                }
            }

            if (metadata.CustomProperties != null)
            {
                foreach (var kvp in metadata.CustomProperties)
                {
                    if (kvp.Value is string s) writer.WriteString(kvp.Key, s);
                    else if (kvp.Value is int i) writer.WriteNumber(kvp.Key, i);
                    else if (kvp.Value is double d) writer.WriteNumber(kvp.Key, d);
                    else if (kvp.Value is bool b) writer.WriteBoolean(kvp.Key, b);
                    else if (kvp.Value != null) writer.WriteString(kvp.Key, kvp.Value.ToString());
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
