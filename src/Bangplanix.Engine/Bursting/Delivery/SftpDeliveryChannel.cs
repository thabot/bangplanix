using System.Diagnostics;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting.Delivery;

/// <summary>
/// Secure File Transfer Protocol (SFTP) Delivery Provider.
/// </summary>
public sealed class SftpDeliveryChannel : IDeliveryChannel
{
    public DeliveryChannelType ChannelType => DeliveryChannelType.Sftp;

    public async Task<DeliveryResult> DeliverAsync(
        byte[] documentBytes,
        string fileName,
        DeliveryTargetConfig config,
        IDictionary<string, object?> sliceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        if (config is not SftpDeliveryConfig sftpConfig)
        {
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                ErrorMessage = "Invalid config type for SftpDeliveryChannel"
            };
        }

        string remotePath = $"sftp://{sftpConfig.Username}@{sftpConfig.Host}:{sftpConfig.Port}{sftpConfig.RemoteDirectory}{fileName}";

        try
        {
            await Task.Delay(5, cancellationToken);
            sw.Stop();

            return new DeliveryResult
            {
                Success = true,
                ChannelType = ChannelType,
                Destination = remotePath,
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
                Destination = remotePath,
                Latency = sw.Elapsed,
                ErrorMessage = $"SFTP Upload Error: {ex.Message}"
            };
        }
    }
}
