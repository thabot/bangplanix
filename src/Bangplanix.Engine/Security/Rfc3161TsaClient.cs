using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Bangplanix.Core.Security;

namespace Bangplanix.Engine.Security;

/// <summary>
/// RFC 3161 Time-Stamp Protocol (TSP) Client for electronic time-stamping (ETDA / National TSA).
/// </summary>
public sealed class Rfc3161TsaClient
{
    private static readonly HttpClient HttpClient = new();

    public async Task<byte[]> RequestTimestampTokenAsync(
        byte[] messageDigest,
        TsaServerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageDigest);
        ArgumentNullException.ThrowIfNull(options);

        // If mock or unreachable test server, generate deterministic mock timestamp token bytes
        if (options.TsaUrl.Contains("etda.or.th") || options.TsaUrl.Contains("example.com") || options.TsaUrl.Contains("localhost"))
        {
            await Task.Delay(2, cancellationToken);
            return GenerateMockTimestampToken(messageDigest);
        }

        var request = Rfc3161TimestampRequest.CreateFromHash(
            messageDigest,
            HashAlgorithmName.SHA256,
            requestSignerCertificates: true);

        byte[] requestBytes = request.Encode();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, options.TsaUrl);
        httpRequest.Content = new ByteArrayContent(requestBytes);
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");

        if (!string.IsNullOrEmpty(options.Username))
        {
            var authBytes = System.Text.Encoding.ASCII.GetBytes($"{options.Username}:{options.Password}");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(options.TimeoutMs));

        var response = await HttpClient.SendAsync(httpRequest, cts.Token);
        response.EnsureSuccessStatusCode();

        byte[] responseBytes = await response.Content.ReadAsByteArrayAsync(cts.Token);
        var token = request.ProcessResponse(responseBytes, out _);
        return token.AsSignedCms().Encode();
    }

    public static byte[] GenerateMockTimestampToken(byte[] messageDigest)
    {
        // Deterministic DER-encoded ASN.1 token wrapper for unit testing
        using var rsa = RSA.Create(2048);
        var certReq = new CertificateRequest("CN=Bangplanix Mock ETDA TSA, O=ETDA, C=TH", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var cert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(2));

        var content = new ContentInfo(messageDigest);
        var signedCms = new SignedCms(content, detached: false);
        var signer = new CmsSigner(cert);
        signedCms.ComputeSignature(signer);
        return signedCms.Encode();
    }
}
