using System.Text.Json;
using System.Text.Json.Serialization;
using Bangplanix.Core.Models;

namespace Bangplanix.Core.Parser;

public static class BpxParser
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static ReportDefinition Parse(ReadOnlySpan<byte> utf8Json)
    {
        var result = JsonSerializer.Deserialize<ReportDefinition>(utf8Json, DefaultOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize .bpx report template: payload returned null.");
    }

    public static ReportDefinition Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var result = JsonSerializer.Deserialize<ReportDefinition>(json, DefaultOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize .bpx report template: payload returned null.");
    }

    public static ReportDefinition Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var result = JsonSerializer.Deserialize<ReportDefinition>(stream, DefaultOptions);
        return result ?? throw new InvalidOperationException("Failed to deserialize .bpx report template: payload returned null.");
    }

    public static async ValueTask<ReportDefinition> ParseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var result = await JsonSerializer.DeserializeAsync<ReportDefinition>(stream, DefaultOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new InvalidOperationException("Failed to deserialize .bpx report template: payload returned null.");
    }

    public static string ToJson(ReportDefinition report, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(report);
        var options = new JsonSerializerOptions(DefaultOptions)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(report, options);
    }

    public static byte[] ToUtf8Bytes(ReportDefinition report, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(report);
        var options = new JsonSerializerOptions(DefaultOptions)
        {
            WriteIndented = indented
        };
        return JsonSerializer.SerializeToUtf8Bytes(report, options);
    }
}
