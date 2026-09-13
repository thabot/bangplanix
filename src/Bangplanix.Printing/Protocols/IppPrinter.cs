using System.Net.Http.Headers;

namespace Bangplanix.Printing.Protocols;

public sealed class IppPrinter : IPrinterProtocol
{
    public string ProtocolName => "IPP (Internet Printing Protocol / RFC 8011)";
    public int DefaultPort => 631;

    private readonly HttpClient _httpClient;

    public IppPrinter(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<bool> PrintAsync(byte[] data, string host, int port = 631, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(host);

        var printerUri = host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("ipp://", StringComparison.OrdinalIgnoreCase)
            ? host.Replace("ipp://", "http://", StringComparison.OrdinalIgnoreCase)
            : $"http://{host}:{port}/ipp/print";

        var ippPayload = BuildIppPrintJobRequest(printerUri, data);

        using var request = new HttpRequestMessage(HttpMethod.Post, printerUri);
        request.Content = new ByteArrayContent(ippPayload);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ipp");

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    public async Task<PrinterStatusResult> CheckStatusAsync(string host, int port = 631, CancellationToken cancellationToken = default)
    {
        try
        {
            var printerUri = host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || host.StartsWith("ipp://", StringComparison.OrdinalIgnoreCase)
                ? host.Replace("ipp://", "http://", StringComparison.OrdinalIgnoreCase)
                : $"http://{host}:{port}/ipp/print";

            var ippPayload = BuildIppGetAttributesRequest(printerUri);
            using var request = new HttpRequestMessage(HttpMethod.Post, printerUri);
            request.Content = new ByteArrayContent(ippPayload);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/ipp");

            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return PrinterStatusResult.Healthy();
            }

            return PrinterStatusResult.Offline($"HTTP status {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return PrinterStatusResult.Offline(ex.Message);
        }
    }

    public static byte[] BuildIppPrintJobRequest(string printerUri, byte[] documentBytes)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // IPP Header: version 2.0 (0x02, 0x00), OperationId: Print-Job (0x0002), RequestId: 1
        writer.Write((byte)0x02);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x02);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x01);

        // Operation Attributes Tag (0x01)
        writer.Write((byte)0x01);

        // attributes-charset (utf-8)
        WriteIppAttribute(writer, 0x47, "attributes-charset", "utf-8");
        // attributes-natural-language (en-us)
        WriteIppAttribute(writer, 0x48, "attributes-natural-language", "en-us");
        // printer-uri
        WriteIppAttribute(writer, 0x45, "printer-uri", printerUri);
        // requesting-user-name (bangplanix)
        WriteIppAttribute(writer, 0x42, "requesting-user-name", "bangplanix");

        // End-of-attributes tag (0x03)
        writer.Write((byte)0x03);

        // Document content payload
        writer.Write(documentBytes);

        return ms.ToArray();
    }

    public static byte[] BuildIppGetAttributesRequest(string printerUri)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // IPP Header: version 2.0 (0x02, 0x00), OperationId: Get-Printer-Attributes (0x000B), RequestId: 1
        writer.Write((byte)0x02);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x0B);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x00);
        writer.Write((byte)0x01);

        // Operation Attributes Tag (0x01)
        writer.Write((byte)0x01);
        WriteIppAttribute(writer, 0x47, "attributes-charset", "utf-8");
        WriteIppAttribute(writer, 0x48, "attributes-natural-language", "en-us");
        WriteIppAttribute(writer, 0x45, "printer-uri", printerUri);

        // End-of-attributes tag (0x03)
        writer.Write((byte)0x03);

        return ms.ToArray();
    }

    private static void WriteIppAttribute(BinaryWriter writer, byte valueTag, string name, string value)
    {
        writer.Write(valueTag);
        writer.Write((byte)(name.Length >> 8));
        writer.Write((byte)(name.Length & 0xFF));
        writer.Write(System.Text.Encoding.ASCII.GetBytes(name));

        writer.Write((byte)(value.Length >> 8));
        writer.Write((byte)(value.Length & 0xFF));
        writer.Write(System.Text.Encoding.UTF8.GetBytes(value));
    }
}
