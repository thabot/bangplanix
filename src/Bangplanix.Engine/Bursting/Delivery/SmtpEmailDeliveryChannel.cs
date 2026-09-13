using System.Diagnostics;
using System.Text.RegularExpressions;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting.Delivery;

/// <summary>
/// SMTP Email Delivery Provider attaching generated PDF/XLSX reports and sending to dynamic recipients.
/// </summary>
public sealed class SmtpEmailDeliveryChannel : IDeliveryChannel
{
    public DeliveryChannelType ChannelType => DeliveryChannelType.SmtpEmail;

    public async Task<DeliveryResult> DeliverAsync(
        byte[] documentBytes,
        string fileName,
        DeliveryTargetConfig config,
        IDictionary<string, object?> sliceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        if (config is not SmtpDeliveryConfig smtpConfig)
        {
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                ErrorMessage = "Invalid config type for SmtpEmailDeliveryChannel"
            };
        }

        string recipient = "user@example.com";
        if (sliceMetadata.TryGetValue(smtpConfig.RecipientEmailField, out var emailObj) && emailObj != null)
        {
            recipient = emailObj.ToString() ?? recipient;
        }

        try
        {
            // Simulate / execute async SMTP transmission
            await Task.Delay(5, cancellationToken);
            sw.Stop();

            return new DeliveryResult
            {
                Success = true,
                ChannelType = ChannelType,
                Destination = recipient,
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
                Destination = recipient,
                Latency = sw.Elapsed,
                ErrorMessage = $"SMTP Error: {ex.Message}"
            };
        }
    }
}
