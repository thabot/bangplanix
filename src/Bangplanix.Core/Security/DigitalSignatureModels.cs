namespace Bangplanix.Core.Security;

/// <summary>
/// Supported PAdES Digital Signature compliance levels.
/// </summary>
public enum PdfSignatureLevel
{
    PAdES_B_B,  // Basic detached signature
    PAdES_B_T,  // Detached signature with RFC 3161 Timestamp
    PAdES_B_LT, // Long Term Validation with CRL/OCSP
    PAdES_B_LTA // Long Term with Archive Timestamp
}

/// <summary>
/// Thai e-Tax invoice document types.
/// </summary>
public enum ThaiETaxDocType
{
    T01_TaxInvoice,
    T02_DebitNote,
    T03_CreditNote,
    T04_Invoice,
    T05_Receipt
}

/// <summary>
/// Legacy / Simple configuration options for PDF digital signing.
/// </summary>
public class PdfSigningOptions
{
    public string SignerName { get; set; } = "Bangplanix Authority";
    public string Reason { get; set; } = "Digital Signature";
    public string Location { get; set; } = "Bangkok, Thailand";
    public string ContactInfo { get; set; } = "contact@bangplanix.com";
}

public enum PadesSubFilter
{
    AdbePkcs7Detached,
    EtsiCadesDetached
}

/// <summary>
/// Configuration options for PDF digital signing and ETDA e-Tax compliance.
/// </summary>
public sealed class DigitalSignatureOptions : PdfSigningOptions
{
    public PdfSignatureLevel SignatureLevel { get; set; } = PdfSignatureLevel.PAdES_B_B;
    public PadesSubFilter SubFilter { get; set; } = PadesSubFilter.EtsiCadesDetached;
    public byte[]? PfxRawBytes { get; set; }
    public string? PfxPassword { get; set; }
    public string? SignerCertificateThumbprint { get; set; }
    public TsaServerOptions? TsaOptions { get; set; }
    public bool EmbedEtdaXml { get; set; } = false;
    public ETdaInvoiceAttachment? XmlAttachment { get; set; }
    public List<PdfXmlAttachmentPayload> ExtraXmlAttachments { get; set; } = new();
    public VisualSignatureAppearance? VisualAppearance { get; set; }
    public AnchorTagPlacementOptions? AnchorTagOptions { get; set; }
    public AuditTrailReportOptions? AuditTrailOptions { get; set; }
    public InvisibleWatermarkOptions? ForensicWatermark { get; set; }
    public bool EnableQuantumSafeMldsa { get; set; } = false;
    public bool AirGappedMode { get; set; } = false;
    public bool EnableIncrementalMultiSign { get; set; } = true;
}

/// <summary>
/// RFC 3161 Time-Stamp Authority (TSA) server configuration.
/// </summary>
public sealed class TsaServerOptions
{
    public string TsaUrl { get; set; } = "https://tsa.etda.or.th";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string HashAlgorithm { get; set; } = "SHA-256";
    public int TimeoutMs { get; set; } = 5000;
}

/// <summary>
/// e-Tax invoice XML attachment for ETDA PDF/A-3 compliance.
/// </summary>
public sealed class ETdaInvoiceAttachment
{
    public byte[] XmlBytes { get; set; } = Array.Empty<byte>();
    public string XmlFileName { get; set; } = "ETDA-invoice.xml";
    public string Description { get; set; } = "ETDA Standard CrossIndustryInvoice:2 XML Payload";
    public string SchemaVersion { get; set; } = "2.0";
}

/// <summary>
/// Thai Taxpayer party information.
/// </summary>
public sealed class ThaiTaxPayerInfo
{
    public string TaxId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BranchId { get; set; } = "00000";
    public string FullTaxIdentifier => $"{TaxId}{BranchId}";
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; } = "TH";
}

/// <summary>
/// Line item in a Thai e-Tax invoice.
/// </summary>
public sealed class ThaiETaxLineItem
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; } = 0;
    public decimal VatRate { get; set; } = 7.0m;
    public decimal Subtotal => Math.Round(Quantity * UnitPrice, 2);
    public decimal VatAmount => Math.Round(Subtotal * (VatRate / 100m), 2);
    public decimal TotalWithVat => Subtotal + VatAmount;
}

/// <summary>
/// Thai e-Tax Invoice Data model.
/// </summary>
public sealed class ThaiETaxInvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDateTime { get; set; } = DateTime.UtcNow;
    public ThaiETaxDocType DocType { get; set; } = ThaiETaxDocType.T01_TaxInvoice;
    public ThaiTaxPayerInfo Seller { get; set; } = new();
    public ThaiTaxPayerInfo Buyer { get; set; } = new();
    public List<ThaiETaxLineItem> LineItems { get; set; } = new();
    public decimal TotalSubtotal => LineItems.Sum(x => x.Subtotal);
    public decimal TotalVat => LineItems.Sum(x => x.VatAmount);
    public decimal TotalAmount => LineItems.Sum(x => x.TotalWithVat);
}

/// <summary>
/// Verification result of a digital signature inside a PDF.
/// </summary>
public sealed class SignatureVerificationResult
{
    public bool IsValid { get; set; }
    public string SignerSubject { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? SigningTimeUtc { get; set; }
    public bool HasTimestampToken { get; set; }
    public DateTime? TimestampTimeUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PdfXmlAttachmentPayload
{
    public string FileName { get; set; } = "attachment.xml";
    public string Description { get; set; } = "Embedded XML Attachment";
    public string XmlContent { get; set; } = string.Empty;
    public string Relationship { get; set; } = "Alternative";
}

public class VisualSignatureAppearance
{
    public int PageNumber { get; set; } = 1;
    public double X { get; set; } = 400;
    public double Y { get; set; } = 700;
    public double Width { get; set; } = 150;
    public double Height { get; set; } = 60;
    public string? Title { get; set; } = "Authorized Signatory";
    public string? SignatureImageBase64 { get; set; }
    public bool ShowSignerDetails { get; set; } = true;
}

public class AnchorTagPlacementOptions
{
    public string AnchorTagName { get; set; } = "[[SIG:Approver]]";
    public double SignatureBoxWidth { get; set; } = 150;
    public double SignatureBoxHeight { get; set; } = 60;
    public string? SignatureImageBase64 { get; set; }
    public string? Title { get; set; }
}

public class AuditTrailReportOptions
{
    public string? SignerIpAddress { get; set; }
    public string? SignerDeviceUserAgent { get; set; }
    public bool IncludeVerificationQrCode { get; set; } = true;
    public string QrVerificationBaseUrl { get; set; } = "http://localhost:9545/verify?docId=";
}

public class InvisibleWatermarkOptions
{
    public bool EnableTracking { get; set; } = false;
    public string TrackingPayload { get; set; } = string.Empty;
}

public class PdfSecurityOptions
{
    public string? UserPassword { get; set; }
    public string? OwnerPassword { get; set; }
    public bool AllowPrinting { get; set; } = true;
    public bool AllowCopying { get; set; } = false;
    public bool AllowModifications { get; set; } = false;
    public bool EnableAes256Encryption { get; set; } = true;
}

public class ExcelExportOptions
{
    public string? Password { get; set; }
    public bool ProtectSheets { get; set; } = false;
    public bool EnableNativeFormulas { get; set; } = true;
    public bool AutoFitColumnWidth { get; set; } = true;
    public bool FreezeTopRow { get; set; } = true;
    public bool EnableAutoFilter { get; set; } = true;
}

public class CsvExportOptions
{
    public bool IncludeUtf8Bom { get; set; } = true;
    public bool SanitizeFormulas { get; set; } = true;
    public string Delimiter { get; set; } = ",";
    public bool QuoteStrings { get; set; } = true;
}

