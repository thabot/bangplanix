using System.Net;
using System.Net.Sockets;

namespace Bangplanix.Core.Security;

public static class AntiSsrfValidator
{
    public static bool IsSafeUrl(string? url, out string? reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            reason = "URL cannot be empty.";
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            reason = "Invalid URL format.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            reason = $"Forbidden URL scheme '{uri.Scheme}'. Only HTTP and HTTPS are allowed.";
            return false;
        }

        var host = uri.Host;

        // Check if host is IP
        if (IPAddress.TryParse(host, out var ip))
        {
            if (IsPrivateOrBlockedIp(ip))
            {
                reason = $"Blocked private/loopback IP address: {ip}";
                return false;
            }
        }
        else
        {
            // Block localhost domain names
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Blocked internal host: {host}";
                return false;
            }

            // Resolve DNS to verify no private IP
            try
            {
                var resolvedIps = Dns.GetHostAddresses(host);
                foreach (var resolvedIp in resolvedIps)
                {
                    if (IsPrivateOrBlockedIp(resolvedIp))
                    {
                        reason = $"Host '{host}' resolves to blocked private IP: {resolvedIp}";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                reason = $"DNS resolution failed for host '{host}': {ex.Message}";
                return false;
            }
        }

        return true;
    }

    public static bool IsPrivateOrBlockedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
        {
            return true;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            byte b0 = bytes[0];
            byte b1 = bytes[1];

            // 0.0.0.0/8 (Broadcast/This host)
            if (b0 == 0) return true;

            // 10.0.0.0/8 (RFC 1918 Private)
            if (b0 == 10) return true;

            // 127.0.0.0/8 (Loopback)
            if (b0 == 127) return true;

            // 172.16.0.0/12 (RFC 1918 Private: 172.16.0.0 - 172.31.255.255)
            if (b0 == 172 && b1 >= 16 && b1 <= 31) return true;

            // 192.168.0.0/16 (RFC 1918 Private)
            if (b0 == 192 && b1 == 168) return true;

            // 169.254.0.0/16 (Link Local & Cloud Metadata: 169.254.169.254)
            if (b0 == 169 && b1 == 254) return true;

            // 100.64.0.0/10 (Carrier-Grade NAT)
            if (b0 == 100 && b1 >= 64 && b1 <= 127) return true;

            // 224.0.0.0/4 (Multicast) & 240.0.0.0/4 (Reserved)
            if (b0 >= 224) return true;
        }
        else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal || IPAddress.IPv6Loopback.Equals(ip) || IPAddress.IPv6None.Equals(ip) || IPAddress.IPv6Any.Equals(ip))
            {
                return true;
            }

            var bytes = ip.GetAddressBytes();
            // Unique Local Address (fc00::/7)
            if ((bytes[0] & 0xFE) == 0xFC) return true;
        }

        return false;
    }

    public static SocketsHttpHandler CreateSafeSocketsHttpHandler()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false, // Critical: prevent 302 redirect bypass to private IP
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var port = context.DnsEndPoint.Port;

                // Resolve DNS
                var ips = await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
                if (ips.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }

                // Verify every IP and pick the first safe IP
                IPAddress? safeIp = null;
                foreach (var ip in ips)
                {
                    if (!IsPrivateOrBlockedIp(ip))
                    {
                        safeIp = ip;
                        break;
                    }
                }

                if (safeIp == null)
                {
                    throw new InvalidOperationException($"SSRF Blocked: All resolved IPs for host '{host}' are private or restricted.");
                }

                // Directly connect to the validated IP to defeat DNS Rebinding (TOCTOU) attacks
                var socket = new Socket(safeIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                try
                {
                    await socket.ConnectAsync(new IPEndPoint(safeIp, port), cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }

    public static HttpClient CreateSafeHttpClient(TimeSpan? timeout = null)
    {
        var handler = CreateSafeSocketsHttpHandler();
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(5)
        };
    }

    public static async Task<byte[]?> DownloadBytesSafelyAsync(string url, long maxBytes = 15 * 1024 * 1024, CancellationToken ct = default)
    {
        if (!IsSafeUrl(url, out _)) return null;

        using var client = CreateSafeHttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;

        if (response.Content.Headers.ContentLength > maxBytes)
        {
            throw new InvalidOperationException($"Downloaded content exceeds max allowed size ({maxBytes} bytes).");
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            totalRead += read;
            if (totalRead > maxBytes)
            {
                throw new InvalidOperationException($"Downloaded content exceeds max allowed size ({maxBytes} bytes).");
            }
            await ms.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
        }

        return ms.ToArray();
    }
}

