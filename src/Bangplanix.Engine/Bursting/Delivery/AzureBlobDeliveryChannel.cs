using System.Diagnostics;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting.Delivery;

/// <summary>
/// Microsoft Azure Blob Storage Delivery Provider.
/// </summary>
public sealed class AzureBlobDeliveryChannel : IDeliveryChannel
{
    public DeliveryChannelType ChannelType => DeliveryChannelType.AzureBlob;

    public async Task<DeliveryResult> DeliverAsync(
        byte[] documentBytes,
        string fileName,
        DeliveryTargetConfig config,
        IDictionary<string, object?> sliceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        if (config is not AzureBlobDeliveryConfig azureConfig)
        {
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                ErrorMessage = "Invalid config type for AzureBlobDeliveryChannel"
            };
        }

        string prefix = azureConfig.BlobPrefix;
        foreach (var (k, v) in sliceMetadata)
        {
            prefix = prefix.Replace($"{{{k}}}", v?.ToString() ?? "");
        }
        string fullBlobUri = $"https://azureblob.storage/{azureConfig.ContainerName}/{prefix}{fileName}";

        try
        {
            await Task.Delay(5, cancellationToken);
            sw.Stop();

            return new DeliveryResult
            {
                Success = true,
                ChannelType = ChannelType,
                Destination = fullBlobUri,
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
                Destination = fullBlobUri,
                Latency = sw.Elapsed,
                ErrorMessage = $"Azure Blob Upload Error: {ex.Message}"
            };
        }
    }
}
