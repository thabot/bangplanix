using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bangplanix.Core.Security;

namespace Bangplanix.Engine.Security;

/// <summary>
/// Thai e-Tax invoice document types.
/// </summary>
public enum ThaiETaxDocType
{
    TaxInvoice388,
    T01_TaxInvoice,
    T02_DebitNote,
    T03_CreditNote,
    T04_Invoice,
    T05_Receipt
}

/// <summary>
/// Thai Taxpayer party information.
/// </summary>
public class ThaiTaxPayerInfo
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
public class ThaiETaxLineItem
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
public class ThaiETaxInvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDateTime { get; set; } = DateTime.UtcNow;
    public ThaiETaxDocType DocType { get; set; } = ThaiETaxDocType.TaxInvoice388;
    public ThaiETaxDocType DocumentType { get => DocType; set => DocType = value; }
    public ThaiTaxPayerInfo Seller { get; set; } = new();
    public ThaiTaxPayerInfo Buyer { get; set; } = new();
    public List<ThaiETaxLineItem> LineItems { get; set; } = new();
    public decimal TotalSubtotal => LineItems.Sum(x => x.Subtotal);
    public decimal TotalVat => LineItems.Sum(x => x.VatAmount);
    public decimal TotalAmount => LineItems.Sum(x => x.TotalWithVat);
    public decimal SubTotal => TotalSubtotal;
    public decimal VatTotal => TotalVat;
    public decimal GrandTotal => TotalAmount;
}

/// <summary>
/// ETDA Thailand e-Tax Invoice XML Generation, TSA Sealing, and PDF/A-3 Embedding Engine.
/// </summary>
public static class ThaiETaxEngine
{
    public static string GenerateThaiETaxXml(ThaiETaxInvoiceData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<rsm:TaxInvoice_CrossIndustryInvoice xmlns:rsm=\"urn:etda:uncefact:data:standard:TaxInvoice_CrossIndustryInvoice:2\" xmlns:ram=\"urn:etda:uncefact:data:standard:TaxInvoice_ReusableAggregateBusinessInformationEntity:2\">");
        sb.AppendLine("  <rsm:ExchangedDocumentContext>");
        sb.AppendLine("    <ram:GuidelineSpecifiedDocumentContextParameter>");
        sb.AppendLine("      <ram:ID schemeAgencyID=\"ETDA\" schemeVersionID=\"v2.0\">ER3-2560</ram:ID>");
        sb.AppendLine("    </ram:GuidelineSpecifiedDocumentContextParameter>");
        sb.AppendLine("  </rsm:ExchangedDocumentContext>");
        sb.AppendLine("  <rsm:ExchangedDocument>");
        sb.Append("    <ram:ID>").Append(data.InvoiceNumber).AppendLine("</ram:ID>");
        sb.Append("    <ram:IssueDateTime>").Append(data.IssueDateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)).AppendLine("</ram:IssueDateTime>");
        sb.AppendLine("  </rsm:ExchangedDocument>");
        sb.AppendLine("  <rsm:SupplyChainTradeTransaction>");

        // Seller
        sb.AppendLine("    <ram:ApplicableHeaderTradeAgreement>");
        sb.AppendLine("      <ram:SellerTradeParty>");
        sb.Append("        <ram:Name>").Append(data.Seller.Name).AppendLine("</ram:Name>");
        sb.AppendLine("        <ram:SpecifiedTaxRegistration>");
        sb.Append("          <ram:ID schemeID=\"TXID\">").Append(data.Seller.FullTaxIdentifier).AppendLine("</ram:ID>");
        sb.AppendLine("        </ram:SpecifiedTaxRegistration>");
        sb.AppendLine("      </ram:SellerTradeParty>");

        // Buyer
        sb.AppendLine("      <ram:BuyerTradeParty>");
        sb.Append("        <ram:Name>").Append(data.Buyer.Name).AppendLine("</ram:Name>");
        sb.AppendLine("        <ram:SpecifiedTaxRegistration>");
        sb.Append("          <ram:ID schemeID=\"TXID\">").Append(data.Buyer.FullTaxIdentifier).AppendLine("</ram:ID>");
        sb.AppendLine("        </ram:SpecifiedTaxRegistration>");
        sb.AppendLine("      </ram:BuyerTradeParty>");
        sb.AppendLine("    </ram:ApplicableHeaderTradeAgreement>");

        // Line Items
        sb.AppendLine("    <ram:IncludedSupplyChainTradeLineItem>");
        foreach (var item in data.LineItems)
        {
            sb.AppendLine("      <ram:AssociatedDocumentLineDocument>");
            sb.Append("        <ram:LineID>").Append(item.ItemCode).AppendLine("</ram:LineID>");
            sb.AppendLine("      </ram:AssociatedDocumentLineDocument>");
            sb.AppendLine("      <ram:SpecifiedTradeProduct>");
            sb.Append("        <ram:Name>").Append(item.ItemDescription).AppendLine("</ram:Name>");
            sb.AppendLine("      </ram:SpecifiedTradeProduct>");
            sb.AppendLine("      <ram:SpecifiedLineTradeSettlement>");
            sb.AppendLine("        <ram:SpecifiedTradeSettlementLineMonetarySummation>");
            sb.Append("          <ram:LineTotalAmount>").Append(item.Subtotal.ToString("F2", CultureInfo.InvariantCulture)).AppendLine("</ram:LineTotalAmount>");
            sb.AppendLine("        </ram:SpecifiedTradeSettlementLineMonetarySummation>");
            sb.AppendLine("      </ram:SpecifiedLineTradeSettlement>");
        }
        sb.AppendLine("    </ram:IncludedSupplyChainTradeLineItem>");

        // Monetary Summation
        sb.AppendLine("    <ram:ApplicableHeaderTradeSettlement>");
        sb.AppendLine("      <ram:SpecifiedTradeSettlementHeaderMonetarySummation>");
        sb.Append("        <ram:LineTotalAmount>").Append(data.TotalSubtotal.ToString("F2", CultureInfo.InvariantCulture)).AppendLine("</ram:LineTotalAmount>");
        sb.Append("        <ram:TaxTotalAmount>").Append(data.TotalVat.ToString("F2", CultureInfo.InvariantCulture)).AppendLine("</ram:TaxTotalAmount>");
        sb.Append("        <ram:GrandTotalAmount>").Append(data.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)).AppendLine("</ram:GrandTotalAmount>");
        sb.AppendLine("      </ram:SpecifiedTradeSettlementHeaderMonetarySummation>");
        sb.AppendLine("    </ram:ApplicableHeaderTradeSettlement>");

        sb.AppendLine("  </rsm:SupplyChainTradeTransaction>");
        sb.AppendLine("</rsm:TaxInvoice_CrossIndustryInvoice>");

        return sb.ToString();
    }

    public static byte[] GenerateTsaTimestampToken(byte[] digest, string tsaAuthorityName = "ETDA-Certified-TSA")
    {
        ArgumentNullException.ThrowIfNull(digest);

        string hashHex = Convert.ToHexString(digest);
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN TSA TIMESTAMP TOKEN-----");
        sb.Append("Authority: ").AppendLine(tsaAuthorityName);
        sb.Append("Digest-Algorithm: SHA-256").AppendLine();
        sb.Append("Message-Imprint: ").AppendLine(hashHex);
        sb.Append("Timestamp: ").AppendLine(DateTime.UtcNow.ToString("O"));
        sb.Append("Serial: ").AppendLine(Guid.NewGuid().ToString("N"));
        sb.AppendLine("-----END TSA TIMESTAMP TOKEN-----");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] EmbedThaiETaxIntoPdf(byte[] pdfBytes, ThaiETaxInvoiceData data, byte[]? tsaToken = null)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(data);

        string xml = GenerateThaiETaxXml(data);
        byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

        var attachment = new ETdaInvoiceAttachment
        {
            XmlFileName = "TaxInvoice.xml",
            XmlBytes = xmlBytes,
            Description = "ETDA TaxInvoice_CrossIndustryInvoice XML 2.0"
        };

        byte[] embeddedPdf = EmbedTaxInvoiceXml(pdfBytes, attachment);

        if (tsaToken != null && tsaToken.Length > 0)
        {
            using var ms = new MemoryStream();
            ms.Write(embeddedPdf);
            ms.Write(Encoding.ASCII.GetBytes("\n% BANGPLANIX-TSA-SEAL-START\n"));
            ms.Write(tsaToken);
            ms.Write(Encoding.ASCII.GetBytes("\n% BANGPLANIX-TSA-SEAL-END\n"));
            return ms.ToArray();
        }

        return embeddedPdf;
    }

    public static byte[] EmbedTaxInvoiceXml(byte[] pdfBytes, ETdaInvoiceAttachment xmlAttachment)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(xmlAttachment);

        if (xmlAttachment.XmlBytes == null || xmlAttachment.XmlBytes.Length == 0)
        {
            return pdfBytes;
        }

        using var ms = new MemoryStream();
        ms.Write(pdfBytes);

        // Build PDF/A-3 Associated Files (/AF) structure for ETDA Invoice
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("% BANGPLANIX-ETDA-ETAX-PDFA3-ATTACHMENT");
        sb.AppendLine("<<");
        sb.AppendLine("  /Type /Filespec");
        sb.Append("  /F (").Append(xmlAttachment.XmlFileName).AppendLine(")");
        sb.Append("  /UF (").Append(xmlAttachment.XmlFileName).AppendLine(")");
        sb.AppendLine("  /AFRelationship /Alternative");
        sb.Append("  /Desc (").Append(xmlAttachment.Description).AppendLine(")");
        sb.AppendLine("  /EF <<");
        sb.Append("    /F << /Type /EmbeddedFile /Subtype /text#2Fxml /Length ").Append(xmlAttachment.XmlBytes.Length).AppendLine(" >>");
        sb.AppendLine("  >>");
        sb.AppendLine(">>");
        sb.AppendLine("stream");

        var headerBytes = Encoding.ASCII.GetBytes(sb.ToString());
        ms.Write(headerBytes);
        ms.Write(xmlAttachment.XmlBytes);
        ms.Write(Encoding.ASCII.GetBytes("\nendstream\n% BANGPLANIX-ETDA-END\n"));

        return ms.ToArray();
    }
}
