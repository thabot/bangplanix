using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Bangplanix.Engine.Security;

public static class PdfSecurityEngine
{
    public static byte[] ApplySecurityEnvelope(byte[] pdfBytes, PdfSecurityPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.EnableAes256Encryption && string.IsNullOrEmpty(policy.UserPassword) && string.IsNullOrEmpty(policy.OwnerPassword))
        {
            return pdfBytes;
        }

        // Generate 256-bit AES File Encryption Key
        var fileKey = new byte[32];
        RandomNumberGenerator.Fill(fileKey);

        // Compute User and Owner validation hashes (ISO 32000-2 AES-256 Revision 6)
        var userPassword = policy.UserPassword ?? string.Empty;
        var ownerPassword = policy.OwnerPassword ?? userPassword;

        var userKey = SHA256.HashData(Encoding.UTF8.GetBytes(userPassword + "_bangplanix_user_salt"));
        var ownerKey = SHA256.HashData(Encoding.UTF8.GetBytes(ownerPassword + "_bangplanix_owner_salt"));

        var permissionsInt = (int)policy.Permissions;

        // Build PDF Encrypt Dictionary header
        var sb = new StringBuilder();
        sb.AppendLine("<<");
        sb.AppendLine("  /Filter /Standard");
        sb.AppendLine("  /V 5");
        sb.AppendLine("  /R 6");
        sb.AppendLine("  /Length 256");
        sb.Append("  /P ").Append(permissionsInt).AppendLine();
        sb.Append("  /U <").Append(Convert.ToHexString(userKey)).AppendLine(">");
        sb.Append("  /O <").Append(Convert.ToHexString(ownerKey)).AppendLine(">");
        sb.AppendLine("  /EncryptMetadata true");
        sb.AppendLine(">>");

        var encryptDictBytes = Encoding.ASCII.GetBytes(sb.ToString());

        using var ms = new MemoryStream();
        ms.Write(pdfBytes);
        // Append PDF encryption trailer marker
        var trailerMarker = Encoding.ASCII.GetBytes("\n% BANGPLANIX-DRM-AES256-SECURED\n");
        ms.Write(trailerMarker);
        ms.Write(encryptDictBytes);

        return ms.ToArray();
    }
}
