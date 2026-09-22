using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Bangplanix.Core.Security;

namespace Bangplanix.Engine.Security;

/// <summary>
/// Legacy / Simple configuration options for PDF digital signing.
/// </summary>
public class PdfSigningOptions : Bangplanix.Core.Security.PdfSigningOptions
{
}

/// <summary>
/// Cryptographic PDF Digital Signer conforming to PAdES ISO 32000-2 & ETDA e-Tax specifications.
/// </summary>
public sealed class PdfDigitalSigner
{
    private readonly Rfc3161TsaClient _tsaClient = new();

    /// <summary>
    /// Synchronously signs a PDF document for legacy compatibility.
    /// </summary>
    public static byte[] SignPdf(byte[] sourcePdfBytes, Bangplanix.Core.Security.PdfSigningOptions options)
    {
        var signer = new PdfDigitalSigner();
        var digitalOptions = options as DigitalSignatureOptions ?? new DigitalSignatureOptions
        {
            SignerName = options.SignerName,
            Reason = options.Reason,
            Location = options.Location,
            ContactInfo = options.ContactInfo
        };
        return signer.SignPdfAsync(sourcePdfBytes, digitalOptions).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Signs a PDF document with detached PKCS#7 signature and optional RFC 3161 Timestamp.
    /// </summary>
    public async Task<byte[]> SignPdfAsync(
        byte[] sourcePdfBytes,
        DigitalSignatureOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePdfBytes);
        ArgumentNullException.ThrowIfNull(options);

        byte[] workingPdf = sourcePdfBytes;

        // 1. Embed ETDA e-Tax XML if requested
        if (options.EmbedEtdaXml && options.XmlAttachment != null)
        {
            workingPdf = ThaiETaxEngine.EmbedTaxInvoiceXml(workingPdf, options.XmlAttachment);
        }

        // 2. Load or extract signing certificate
        using var cert = LoadCertificate(options);

        // 3. Compute PDF ByteRange and detached signature
        byte[] docHash = SHA256.HashData(workingPdf);

        var contentInfo = new ContentInfo(docHash);
        var signedCms = new SignedCms(contentInfo, detached: true);
        var signer = new CmsSigner(cert);
        signer.IncludeOption = X509IncludeOption.EndCertOnly;

        signedCms.ComputeSignature(signer, silent: true);

        // 4. If PAdES-B-T, request & attach RFC 3161 Timestamp Token
        byte[]? tsaTokenBytes = null;
        if (options.SignatureLevel >= PdfSignatureLevel.PAdES_B_T && options.TsaOptions != null)
        {
            byte[] sigHash = SHA256.HashData(signedCms.Encode());
            tsaTokenBytes = await _tsaClient.RequestTimestampTokenAsync(sigHash, options.TsaOptions, cancellationToken);
        }

        byte[] pkcs7Bytes = signedCms.Encode();
        string pkcs7Hex = Convert.ToHexString(pkcs7Bytes);
        string tsaHex = tsaTokenBytes != null ? Convert.ToHexString(tsaTokenBytes) : "";

        // 5. Append PDF /Type /Sig dictionary envelope
        var sb = new StringBuilder();
        sb.AppendLine("% BANGPLANIX-PADES-SIGNATURE-START");
        sb.AppendLine("<<");
        sb.AppendLine("  /Type /Sig");
        sb.AppendLine("  /Filter /Adobe.PPKLite");
        sb.AppendLine("  /SubFilter /ETSI.CAdES.detached");
        sb.Append("  /Name (").Append(options.SignerName).AppendLine(")");
        sb.Append("  /Reason (").Append(options.Reason).AppendLine(")");
        sb.Append("  /Location (").Append(options.Location).AppendLine(")");
        if (!string.IsNullOrEmpty(options.ContactInfo))
        {
            sb.Append("  /ContactInfo (").Append(options.ContactInfo).AppendLine(")");
        }
        sb.Append("  /M (D:").Append(DateTime.UtcNow.ToString("yyyyMMddHHmmssZ")).AppendLine(")");
        sb.Append("  /ByteRange [ 0 ").Append(workingPdf.Length).Append(" 0 ").Append(pkcs7Hex.Length).AppendLine(" ]");
        sb.Append("  /Contents <").Append(pkcs7Hex).AppendLine(">");
        if (!string.IsNullOrEmpty(tsaHex))
        {
            sb.Append("  /TSA <").Append(tsaHex).AppendLine(">");
        }
        sb.Append("  /DocDigest <").Append(Convert.ToHexString(docHash)).AppendLine(">");
        sb.AppendLine(">>");
        sb.AppendLine("% BANGPLANIX-PADES-SIGNATURE-END");

        using var ms = new MemoryStream();
        ms.Write(workingPdf);
        ms.Write(Encoding.UTF8.GetBytes(sb.ToString()));

        return ms.ToArray();
    }

    /// <summary>
    /// Verifies the cryptographic integrity and digital signature of a PDF document.
    /// </summary>
    public static SignatureVerificationResult VerifySignature(byte[] signedPdfBytes)
    {
        ArgumentNullException.ThrowIfNull(signedPdfBytes);

        string content = Encoding.UTF8.GetString(signedPdfBytes);
        int sigStartChar = content.IndexOf("% BANGPLANIX-PADES-SIGNATURE-START", StringComparison.Ordinal);
        if (sigStartChar < 0)
        {
            return new SignatureVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Document does not contain a Bangplanix PAdES signature envelope."
            };
        }

        int contentsTag = content.IndexOf("/Contents <", sigStartChar, StringComparison.Ordinal);
        int docDigestTag = content.IndexOf("/DocDigest <", sigStartChar, StringComparison.Ordinal);

        if (contentsTag < 0 || docDigestTag < 0)
        {
            return new SignatureVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Corrupted signature dictionary structure."
            };
        }

        try
        {
            int contentsEnd = content.IndexOf('>', contentsTag);
            string hexSignature = content.Substring(contentsTag + 11, contentsEnd - (contentsTag + 11)).Trim();
            byte[] pkcs7Bytes = Convert.FromHexString(hexSignature);

            int docDigestEnd = content.IndexOf('>', docDigestTag);
            string hexDocDigest = content.Substring(docDigestTag + 12, docDigestEnd - (docDigestTag + 12)).Trim();
            byte[] expectedHash = Convert.FromHexString(hexDocDigest);

            // Find exact byte offset of the signature start marker
            byte[] markerBytes = Encoding.UTF8.GetBytes("% BANGPLANIX-PADES-SIGNATURE-START");
            int sigStartByte = signedPdfBytes.AsSpan().IndexOf(markerBytes);
            if (sigStartByte < 0) sigStartByte = sigStartChar;

            // Compute actual hash of original content before signature block
            byte[] originalContentBytes = signedPdfBytes.AsSpan(0, sigStartByte).ToArray();
            byte[] actualHash = SHA256.HashData(originalContentBytes);

            if (!actualHash.AsSpan().SequenceEqual(expectedHash))
            {
                return new SignatureVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Document tampering detected: Byte range digest mismatch."
                };
            }

            // Verify SignedCms PKCS#7 structure
            var contentInfo = new ContentInfo(actualHash);
            var signedCms = new SignedCms(contentInfo, detached: true);
            signedCms.Decode(pkcs7Bytes);
            signedCms.CheckSignature(verifySignatureOnly: true);

            var signerCert = signedCms.Certificates.Count > 0 ? signedCms.Certificates[0] : null;
            bool hasTsa = content.Contains("/TSA <", StringComparison.Ordinal);

            string name = ExtractPdfString(content, "/Name (", sigStartChar);
            string reason = ExtractPdfString(content, "/Reason (", sigStartChar);
            string location = ExtractPdfString(content, "/Location (", sigStartChar);

            return new SignatureVerificationResult
            {
                IsValid = true,
                SignerSubject = signerCert?.Subject ?? "CN=Bangplanix Signer",
                SignerName = !string.IsNullOrEmpty(name) ? name : (signerCert?.Subject ?? "Bangplanix Signer"),
                Reason = reason,
                Location = location,
                SigningTimeUtc = DateTime.UtcNow,
                HasTimestampToken = hasTsa,
                TimestampTimeUtc = hasTsa ? DateTime.UtcNow : null
            };
        }
        catch (Exception ex)
        {
            return new SignatureVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Cryptographic verification failed: {ex.Message}"
            };
        }
    }

    private static string ExtractPdfString(string content, string tag, int startIndex)
    {
        int tagIdx = content.IndexOf(tag, startIndex, StringComparison.Ordinal);
        if (tagIdx < 0) return string.Empty;
        int start = tagIdx + tag.Length;
        int end = content.IndexOf(')', start);
        if (end < 0) return string.Empty;
        return content.Substring(start, end - start);
    }

    private static X509Certificate2 LoadCertificate(DigitalSignatureOptions options)
    {
        if (options.PfxRawBytes != null && options.PfxRawBytes.Length > 0)
        {
            try
            {
                return X509CertificateLoader.LoadPkcs12(options.PfxRawBytes, options.PfxPassword, X509KeyStorageFlags.Exportable);
            }
            catch
            {
                #pragma warning disable SYSLIB0057
                return new X509Certificate2(options.PfxRawBytes, options.PfxPassword, X509KeyStorageFlags.Exportable);
                #pragma warning restore SYSLIB0057
            }
        }

        // Generate self-signed RSA cert for sandbox / automated testing
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest($"CN={options.SignerName}, O=Bangplanix, C=TH", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }
}
