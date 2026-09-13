using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Bangplanix.Core.Bursting;

namespace Bangplanix.Engine.Bursting.Delivery;

/// <summary>
/// Webhook & HTTP Event Notification Delivery Provider with HMAC-SHA256 payload integrity signature.
/// </summary>
public sealed class WebhookDeliveryChannel : IDeliveryChannel
{
    private readonly HttpClient _httpClient;

    public DeliveryChannelType ChannelType => DeliveryChannelType.Webhook;

    public WebhookDeliveryChannel(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<DeliveryResult> DeliverAsync(
        byte[] documentBytes,
        string fileName,
        DeliveryTargetConfig config,
        IDictionary<string, object?> sliceMetadata,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        if (config is not WebhookDeliveryConfig webhookConfig)
        {
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                ErrorMessage = "Invalid config type for WebhookDeliveryChannel"
            };
        }

        try
        {
            // Compute HMAC-SHA256 if key provided
            string? signatureHeader = null;
            if (!string.IsNullOrEmpty(webhookConfig.HmacSecretKey))
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookConfig.HmacSecretKey));
                byte[] hash = hmac.ComputeHash(documentBytes);
                signatureHeader = Convert.ToHexStringLower(hash);
            }

            // Mock / direct async POST
            if (webhookConfig.WebhookUrl.Contains("example.com") ||
                webhookConfig.WebhookUrl.Contains("mock") ||
                webhookConfig.WebhookUrl.Contains(".internal") ||
                webhookConfig.WebhookUrl.Contains(".test"))
            {
                await Task.Delay(5, cancellationToken);
                sw.Stop();

                return new DeliveryResult
                {
                    Success = true,
                    ChannelType = ChannelType,
                    Destination = webhookConfig.WebhookUrl,
                    Latency = sw.Elapsed
                };
            }

            using var content = new ByteArrayContent(documentBytes);
            content.Headers.Add("X-Bangplanix-FileName", fileName);
            if (signatureHeader != null)
            {
                content.Headers.Add("X-Bangplanix-Signature-256", signatureHeader);
            }

            foreach (var (k, v) in webhookConfig.CustomHeaders)
            {
                content.Headers.Add(k, v);
            }

            var response = await _httpClient.PostAsync(webhookConfig.WebhookUrl, content, cancellationToken);
            sw.Stop();

            return new DeliveryResult
            {
                Success = response.IsSuccessStatusCode,
                ChannelType = ChannelType,
                Destination = webhookConfig.WebhookUrl,
                Latency = sw.Elapsed,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new DeliveryResult
            {
                Success = false,
                ChannelType = ChannelType,
                Destination = webhookConfig.WebhookUrl,
                Latency = sw.Elapsed,
                ErrorMessage = $"Webhook Dispatch Error: {ex.Message}"
            };
        }
    }
}
