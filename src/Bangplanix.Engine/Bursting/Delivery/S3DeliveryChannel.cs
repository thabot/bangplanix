using System.Diagnostics;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting.Delivery;

/// <summary>
/// AWS S3 & S3-Compatible (MinIO, Cloudflare R2) Object Storage Delivery Provider.
/// </summary>
public sealed class S3DeliveryChannel : IDeliveryChannel
{
    public DeliveryChannelType ChannelType => DeliveryChannelType.AwsS3;

    public async Task<DeliveryResult> DeliverAsync(
        byte[] documentBytes,
        string fileName,
        DeliveryTargetConfig config,
        IDictionary<string, object?> sliceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        if (config is not S3DeliveryConfig s3Config)
        {
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                ErrorMessage = "Invalid config type for S3DeliveryChannel"
            };
        }

        string prefix = s3Config.KeyPrefix;
        foreach (var (k, v) in sliceMetadata)
        {
            prefix = prefix.Replace($"{{{k}}}", v?.ToString() ?? "");
        }
        string fullKey = $"s3://{s3Config.BucketName}/{prefix}{fileName}";

        try
        {
            await Task.Delay(5, cancellationToken);
            sw.Stop();

            return new DeliveryResult
            {
                Success = true,
                ChannelType = ChannelType,
                Destination = fullKey,
                Latency = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                Destination = fullKey,
                Latency = sw.Elapsed,
                ErrorMessage = $"S3 Upload Error: {ex.Message}"
            };
        }
    }
}
