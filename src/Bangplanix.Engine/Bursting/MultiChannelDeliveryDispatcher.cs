using System.Collections.Concurrent;
using Bangplanix.Core.Bursting;
using Bangplanix.Engine.Bursting.Delivery;

namespace Bangplanix.Engine.Bursting;

/// <summary>
/// Multi-channel delivery dispatcher with automatic retry and channel provider registry.
/// </summary>
public sealed class MultiChannelDeliveryDispatcher
{
    private readonly Dictionary<DeliveryChannelType, IDeliveryChannel> _channels = new();

    public MultiChannelDeliveryDispatcher()
    {
        // Register default built-in delivery channels
        RegisterChannel(new SmtpEmailDeliveryChannel());
        RegisterChannel(new S3DeliveryChannel());
        RegisterChannel(new AzureBlobDeliveryChannel());
        RegisterChannel(new SftpDeliveryChannel());
        RegisterChannel(new WebhookDeliveryChannel());
    }

    public void RegisterChannel(IDeliveryChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        _channels[channel.ChannelType] = channel;
    }

    /// <summary>
    /// Dispatches a generated document to all configured delivery targets with retry handling.
    /// </summary>
    public async Task<List<DeliveryResult>> DispatchAsync(
        byte[] documentBytes,
        string fileName,
        IEnumerable<DeliveryTargetConfig> targets,
        IDictionary<string, object?> sliceMetadata,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DeliveryResult>();

        foreach (var target in targets.Where(t => t.IsEnabled))
        {
            if (!_channels.TryGetValue(target.ChannelType, out var provider))
            {
                results.Add(new DeliveryResult
                {
                    Success = false,
                    ChannelType = target.ChannelType,
                    ErrorMessage = $"No delivery channel provider registered for {target.ChannelType}"
                });
                continue;
            }

            DeliveryResult result = null!;
            int attempt = 0;
            int delayMs = 50;

            while (attempt < maxRetries)
            {
                attempt++;
                result = await provider.DeliverAsync(documentBytes, fileName, target, sliceMetadata, cancellationToken);
                if (result.Success)
                {
                    break;
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(delayMs, cancellationToken);
                    delayMs *= 2; // Exponential backoff
                }
            }

            results.Add(result);
        }

        return results;
    }
}
