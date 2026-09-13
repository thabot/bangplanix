using System.Net.Sockets;
using System.Text;

namespace Bangplanix.Printing.Protocols;

public sealed class LprPrinter : IPrinterProtocol
{
    public string ProtocolName => "LPR/LPD (RFC 1179)";
    public int DefaultPort => 515;
    public string QueueName { get; set; } = "raw";

    public async Task<bool> PrintAsync(byte[] data, string host, int port = 515, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        using var stream = client.GetStream();

        // 1. Receive job command: \x02<queue>\n
        var cmd = $"\x02{QueueName}\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(cmd), cancellationToken).ConfigureAwait(false);
        await ReadAckAsync(stream, cancellationToken).ConfigureAwait(false);

        // 2. Send Control File
        var jobNumber = Random.Shared.Next(100, 999);
        var hostName = Environment.MachineName;
        var controlFileContent = $"H{hostName}\nPbangplanix\nfdfA{jobNumber}{hostName}\n";
        var controlFileBytes = Encoding.ASCII.GetBytes(controlFileContent);

        var ctrlCmd = $"\x02{controlFileBytes.Length} cfA{jobNumber}{hostName}\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(ctrlCmd), cancellationToken).ConfigureAwait(false);
        await ReadAckAsync(stream, cancellationToken).ConfigureAwait(false);

        await stream.WriteAsync(controlFileBytes, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00 }, cancellationToken).ConfigureAwait(false); // null ack
        await ReadAckAsync(stream, cancellationToken).ConfigureAwait(false);

        // 3. Send Data File
        var dataCmd = $"\x03{data.Length} dfA{jobNumber}{hostName}\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(dataCmd), cancellationToken).ConfigureAwait(false);
        await ReadAckAsync(stream, cancellationToken).ConfigureAwait(false);

        await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00 }, cancellationToken).ConfigureAwait(false);
        await ReadAckAsync(stream, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public async Task<PrinterStatusResult> CheckStatusAsync(string host, int port = 515, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            using var stream = client.GetStream();

            // Check queue state: \x04<queue>\n
            var cmd = $"\x04{QueueName}\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(cmd), cancellationToken).ConfigureAwait(false);

            var buffer = new byte[256];
            var read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            var response = Encoding.ASCII.GetString(buffer, 0, read);

            return new PrinterStatusResult
            {
                IsOnline = true,
                StatusMessage = string.IsNullOrWhiteSpace(response) ? "Queue ready" : response.Trim()
            };
        }
        catch (Exception ex)
        {
            return PrinterStatusResult.Offline(ex.Message);
        }
    }

    private static async Task ReadAckAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var ack = new byte[1];
        var read = await stream.ReadAsync(ack, cancellationToken).ConfigureAwait(false);
        if (read == 0 || ack[0] != 0x00)
        {
            throw new InvalidOperationException($"LPR server rejected command with response byte: {(read > 0 ? ack[0] : -1)}");
        }
    }
}
