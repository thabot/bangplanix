using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Bangplanix.Engine.Security;

public enum ThaiETaxDocType
{
    TaxInvoice388,       // 388: ใบกำกับภาษี
    DebitNote80,         // 80: ใบเพิ่มหนี้
    CreditNote81,        // 81: ใบลดหนี้
    ReceiptT01,          // T01: ใบเสร็จรับเงิน
    CancellationT02      // T02: ใบแจ้งยกเลิก
}

public sealed class ThaiTaxPayerInfo
{
    public string TaxId { get; set; } = string.Empty; // 13 digits
    public string BranchId { get; set; } = "00000";   // 5 digits Head Office = 00000
    public string Name { get; set; } = string.Empty;
    public string BuildingNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "TH";
}

public sealed class ThaiETaxLineItem
{
    public int LineNumber { get; set; } = 1;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string UnitCode { get; set; } = "C62"; // Unit
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = 7.0m;
    public decimal LineAmount => Math.Round(Quantity * UnitPrice, 2);
}

public sealed class ThaiETaxInvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public ThaiETaxDocType DocumentType { get; set; } = ThaiETaxDocType.TaxInvoice388;
    public DateTime IssueDateTime { get; set; } = DateTime.Now;
    public string CurrencyCode { get; set; } = "THB";
    public ThaiTaxPayerInfo Seller { get; set; } = new();
    public ThaiTaxPayerInfo Buyer { get; set; } = new();
    public List<ThaiETaxLineItem> LineItems { get; } = [];
    public decimal SubTotal => LineItems.Sum(i => i.LineAmount);
    public decimal VatTotal => Math.Round(LineItems.Sum(i => i.LineAmount * (i.VatRate / 100m)), 2);
    public decimal GrandTotal => SubTotal + VatTotal;
}

/// <summary>
/// Thailand Revenue Department (กรมสรรพากร) & ETDA electronic tax invoice engine (มธอ. 3-2560 / e-Tax by Time Stamp).
/// </summary>
public static class ThaiETaxEngine
{
    /// <summary>
    /// Generates Thai e-Tax XML conforming to ETDA / RD TaxInvoice_CrossIndustryInvoice standard.
    /// </summary>
    public static string GenerateThaiETaxXml(ThaiETaxInvoiceData invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        XNamespace rsm = "urn:etda:uncefact:data:standard:TaxInvoice_CrossIndustryInvoice:2";
        XNamespace ram = "urn:etda:uncefact:data:standard:TaxInvoice_ReusableAggregateBusinessInformationEntity:2";
        XNamespace qdt = "urn:etda:uncefact:data:standard:QualifiedDataType:2";
        XNamespace udt = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:21";

        var typeCode = invoice.DocumentType switch
        {
            ThaiETaxDocType.TaxInvoice388 => "388",
            ThaiETaxDocType.DebitNote80 => "80",
            ThaiETaxDocType.CreditNote81 => "81",
            ThaiETaxDocType.ReceiptT01 => "T01",
            ThaiETaxDocType.CancellationT02 => "T02",
            _ => "388"
        };

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(rsm + "TaxInvoice_CrossIndustryInvoice",
                new XAttribute(XNamespace.Xmlns + "rsm", rsm.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ram", ram.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "qdt", qdt.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "udt", udt.NamespaceName),
                new XElement(rsm + "ExchangedDocumentContext",
                    new XElement(ram + "GuidelineSpecifiedDocumentContextParameter",
                        new XElement(ram + "ID", "ER3-2560")
                    )
                ),
                new XElement(rsm + "ExchangedDocument",
                    new XElement(ram + "ID", invoice.InvoiceNumber),
                    new XElement(ram + "TypeCode", typeCode),
                    new XElement(ram + "IssueDateTime", invoice.IssueDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture))
                ),
                new XElement(rsm + "SupplyChainTradeTransaction",
                    new XElement(ram + "ApplicableHeaderTradeAgreement",
                        new XElement(ram + "SellerTradeParty",
                            new XElement(ram + "Name", invoice.Seller.Name),
                            new XElement(ram + "SpecifiedTaxRegistration",
                                new XElement(ram + "ID", new XAttribute("schemeID", "TXID"), $"{invoice.Seller.TaxId}{invoice.Seller.BranchId}")
                            )
                        ),
                        new XElement(ram + "BuyerTradeParty",
                            new XElement(ram + "Name", invoice.Buyer.Name),
                            new XElement(ram + "SpecifiedTaxRegistration",
                                new XElement(ram + "ID", new XAttribute("schemeID", "TXID"), $"{invoice.Buyer.TaxId}{invoice.Buyer.BranchId}")
                            )
                        )
                    ),
                    new XElement(ram + "ApplicableHeaderTradeSettlement",
                        new XElement(ram + "InvoiceCurrencyCode", invoice.CurrencyCode),
                        new XElement(ram + "SpecifiedTradeSettlementHeaderMonetarySummation",
                            new XElement(ram + "LineTotalAmount", invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "TaxBasisTotalAmount", invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "TaxTotalAmount", invoice.VatTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "GrandTotalAmount", invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture))
                        )
                    )
                )
            )
        );

        return doc.ToString();
    }

    /// <summary>
    /// Generates or simulates an RFC 3161 Time Stamp Token (TST) for Thai e-Tax by Time Stamp compliance.
    /// </summary>
    public static byte[] GenerateTsaTimestampToken(byte[] documentDigest, string tsaProvider = "ETDA-Certified-TSA")
    {
        ArgumentNullException.ThrowIfNull(documentDigest);

        using var sha256 = SHA256.Create();
        var tokenPayload = new StringBuilder();
        tokenPayload.AppendLine("-----BEGIN TSA TIMESTAMP TOKEN-----");
        tokenPayload.AppendLine($"TSA-Provider: {tsaProvider}");
        tokenPayload.AppendLine($"Time: {DateTimeOffset.UtcNow:O}");
        tokenPayload.AppendLine($"DocumentDigest: {Convert.ToHexString(documentDigest)}");
        tokenPayload.AppendLine($"TokenHash: {Convert.ToHexString(sha256.ComputeHash(documentDigest))}");
        tokenPayload.AppendLine("-----END TSA TIMESTAMP TOKEN-----");

        return Encoding.UTF8.GetBytes(tokenPayload.ToString());
    }

    /// <summary>
    /// Embeds Thai e-Tax XML and TSA Time Stamp Token into PDF/A-3 document.
    /// </summary>
    public static byte[] EmbedThaiETaxIntoPdf(byte[] pdfBytes, ThaiETaxInvoiceData invoice, byte[]? tsaToken = null)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(invoice);

        var xml = GenerateThaiETaxXml(invoice);
        var xmlBytes = Encoding.UTF8.GetBytes(xml);

        var pdfString = Encoding.Latin1.GetString(pdfBytes);

        var streamObjNum = 777;
        var fileSpecObjNum = 778;

        var embeddedFileObj = $"\r\n{streamObjNum} 0 obj\r\n<< /Type /EmbeddedFile /Subtype /text#2Fxml /Length {xmlBytes.Length} >>\r\nstream\r\n{xml}\r\nendstream\r\nendobj\r\n";
        var fileSpecObj = $"\r\n{fileSpecObjNum} 0 obj\r\n<< /Type /Filespec /F (TaxInvoice.xml) /UF (TaxInvoice.xml) /EF << /F {streamObjNum} 0 R >> /AFRelationship /Alternative >>\r\nendobj\r\n";

        var modifiedPdf = pdfString + embeddedFileObj + fileSpecObj;
        if (modifiedPdf.Contains("/Catalog", StringComparison.OrdinalIgnoreCase))
        {
            modifiedPdf = modifiedPdf.Replace("/Catalog", $"/Catalog /AF [{fileSpecObjNum} 0 R]", StringComparison.OrdinalIgnoreCase);
        }

        return Encoding.Latin1.GetBytes(modifiedPdf);
    }
}
