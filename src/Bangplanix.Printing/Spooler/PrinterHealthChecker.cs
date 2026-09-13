using Bangplanix.Printing.Protocols;

namespace Bangplanix.Printing.Spooler;

public sealed class PrinterHealthChecker
{
    private readonly Dictionary<string, IPrinterProtocol> _protocols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tcp"] = new RawSocketPrinter(),
        ["raw"] = new RawSocketPrinter(),
        ["ipp"] = new IppPrinter(),
        ["lpr"] = new LprPrinter()
    };

    public async Task<PrinterStatusResult> CheckPrinterAsync(string protocol, string host, int port, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        if (_protocols.TryGetValue(protocol, out var printerProtocol))
        {
            return await printerProtocol.CheckStatusAsync(host, port, cancellationToken).ConfigureAwait(false);
        }

        // Fallback to RawSocket
        return await _protocols["tcp"].CheckStatusAsync(host, port, cancellationToken).ConfigureAwait(false);
    }
}
