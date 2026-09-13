using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bangplanix.Core.Caching;

public static class DeterministicCacheKeyBuilder
{
    public static string BuildCanonicalCacheKey(string templateJson, string? parametersJson = null, string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateJson);

        using var sha256 = SHA256.Create();
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                writer.WriteString("tenantId", tenantId);
            }

            using var templateDoc = JsonDocument.Parse(templateJson);
            writer.WritePropertyName("template");
            WriteCanonicalJsonElement(writer, templateDoc.RootElement);

            if (!string.IsNullOrWhiteSpace(parametersJson))
            {
                using var paramDoc = JsonDocument.Parse(parametersJson);
                writer.WritePropertyName("parameters");
                WriteCanonicalJsonElement(writer, paramDoc.RootElement);
            }

            writer.WriteEndObject();
        }

        stream.Position = 0;
        var hashBytes = sha256.ComputeHash(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static void WriteCanonicalJsonElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                // Sort properties by name alphabetically for deterministic hash
                var properties = new List<JsonProperty>();
                foreach (var prop in element.EnumerateObject())
                {
                    properties.Add(prop);
                }
                properties.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

                foreach (var prop in properties)
                {
                    writer.WritePropertyName(prop.Name);
                    WriteCanonicalJsonElement(writer, prop.Value);
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonicalJsonElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l))
                    writer.WriteNumberValue(l);
                else
                    writer.WriteNumberValue(element.GetDouble());
                break;

            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;

            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;

            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
        }
    }
}
