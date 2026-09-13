namespace Bangplanix.Printing.Protocols;

public interface IPrinterProtocol
{
    string ProtocolName { get; }
    Task<bool> PrintAsync(byte[] data, string host, int port, CancellationToken cancellationToken = default);
    Task<PrinterStatusResult> CheckStatusAsync(string host, int port, CancellationToken cancellationToken = default);
}

public sealed class PrinterStatusResult
{
    public bool IsOnline { get; set; }
    public bool IsPaperOut { get; set; }
    public bool IsCoverOpen { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public static PrinterStatusResult Healthy() => new() { IsOnline = true, StatusMessage = "Ready" };
    public static PrinterStatusResult Offline(string reason) => new() { IsOnline = false, StatusMessage = reason };
}
