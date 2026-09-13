using System.Net.Sockets;

namespace Bangplanix.Printing.Protocols;

public sealed class RawSocketPrinter : IPrinterProtocol
{
    public string ProtocolName => "Raw TCP (Port 9100 / JetDirect)";
    public int DefaultPort => 9100;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(15);

    public async Task<bool> PrintAsync(byte[] data, string host, int port = 9100, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        using var client = new TcpClient();
        using var timeoutCts = new CancellationTokenSource(ConnectTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        await client.ConnectAsync(host, port, linkedCts.Token).ConfigureAwait(false);

        using var stream = client.GetStream();
        stream.WriteTimeout = (int)SendTimeout.TotalMilliseconds;
        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<PrinterStatusResult> CheckStatusAsync(string host, int port = 9100, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        try
        {
            using var client = new TcpClient();
            using var timeoutCts = new CancellationTokenSource(ConnectTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            await client.ConnectAsync(host, port, linkedCts.Token).ConfigureAwait(false);

            if (client.Connected)
            {
                return PrinterStatusResult.Healthy();
            }

            return PrinterStatusResult.Offline("Connection failed");
        }
        catch (Exception ex)
        {
            return PrinterStatusResult.Offline(ex.Message);
        }
    }
}
