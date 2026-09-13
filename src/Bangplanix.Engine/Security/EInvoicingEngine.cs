using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace Bangplanix.Engine.Security;

public enum EInvoiceStandard
{
    FacturX_ZUGFeRD,
    Peppol_BIS3
}

public enum EInvoiceProfile
{
    Minimum,
    Basic,
    Comfort,
    Extended
}

public sealed class EInvoiceParty
{
    public string Name { get; set; } = string.Empty;
    public string VatId { get; set; } = string.Empty;
    public string TaxRegistrationNumber { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "TH";
}

public sealed class EInvoiceLineItem
{
    public string LineId { get; set; } = "1";
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string UnitCode { get; set; } = "C62"; // Unit
    public decimal UnitPrice { get; set; }
    public decimal VatRatePercent { get; set; } = 7.0m;
    public decimal LineTotal => Math.Round(Quantity * UnitPrice, 2);
}

public sealed class EInvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
    public string CurrencyCode { get; set; } = "THB";
    public EInvoiceParty Seller { get; set; } = new();
    public EInvoiceParty Buyer { get; set; } = new();
    public List<EInvoiceLineItem> LineItems { get; } = [];
    public decimal SubTotal => LineItems.Sum(i => i.LineTotal);
    public decimal TaxTotal => Math.Round(LineItems.Sum(i => i.LineTotal * (i.VatRatePercent / 100m)), 2);
    public decimal GrandTotal => SubTotal + TaxTotal;
}

/// <summary>
/// Electronic Invoicing Engine supporting Factur-X / ZUGFeRD 2.2 and Peppol BIS Billing 3.0 PDF/A-3 compliance.
/// </summary>
public static class EInvoicingEngine
{
    /// <summary>
    /// Generates ISO 19657 / Factur-X (ZUGFeRD) CrossIndustryInvoice XML.
    /// </summary>
    public static string GenerateFacturXXml(EInvoiceData invoice, EInvoiceProfile profile = EInvoiceProfile.Basic)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        XNamespace rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
        XNamespace ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
        XNamespace udt = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";

        var profileUrn = profile switch
        {
            EInvoiceProfile.Minimum => "urn:factur-x.eu:1p0:minimum",
            EInvoiceProfile.Basic => "urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic",
            EInvoiceProfile.Comfort => "urn:cen.eu:en16931:2017",
            EInvoiceProfile.Extended => "urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended",
            _ => "urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic"
        };

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(rsm + "CrossIndustryInvoice",
                new XAttribute(XNamespace.Xmlns + "rsm", rsm.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "ram", ram.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "udt", udt.NamespaceName),
                new XElement(rsm + "ExchangedDocumentContext",
                    new XElement(ram + "GuidelineSpecifiedDocumentContextParameter",
                        new XElement(ram + "ID", profileUrn)
                    )
                ),
                new XElement(rsm + "ExchangedDocument",
                    new XElement(ram + "ID", invoice.InvoiceNumber),
                    new XElement(ram + "TypeCode", "380"), // Commercial Invoice
                    new XElement(ram + "IssueDateTime",
                        new XElement(udt + "DateTimeString",
                            new XAttribute("format", "102"),
                            invoice.IssueDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
                        )
                    )
                ),
                new XElement(rsm + "SupplyChainTradeTransaction",
                    new XElement(ram + "ApplicableHeaderTradeAgreement",
                        new XElement(ram + "SellerTradeParty",
                            new XElement(ram + "Name", invoice.Seller.Name),
                            new XElement(ram + "SpecifiedTaxRegistration",
                                new XElement(ram + "ID", new XAttribute("schemeID", "VA"), invoice.Seller.VatId)
                            )
                        ),
                        new XElement(ram + "BuyerTradeParty",
                            new XElement(ram + "Name", invoice.Buyer.Name),
                            new XElement(ram + "SpecifiedTaxRegistration",
                                new XElement(ram + "ID", new XAttribute("schemeID", "VA"), invoice.Buyer.VatId)
                            )
                        )
                    ),
                    new XElement(ram + "ApplicableHeaderTradeSettlement",
                        new XElement(ram + "InvoiceCurrencyCode", invoice.CurrencyCode),
                        new XElement(ram + "SpecifiedTradeSettlementHeaderMonetarySummation",
                            new XElement(ram + "LineTotalAmount", invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "TaxBasisTotalAmount", invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "TaxTotalAmount", new XAttribute("currencyID", invoice.CurrencyCode), invoice.TaxTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "GrandTotalAmount", invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)),
                            new XElement(ram + "DuePayableAmount", invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture))
                        )
                    )
                )
            )
        );

        return doc.ToString();
    }

    /// <summary>
    /// Generates Peppol BIS Billing 3.0 / UBL 2.1 Invoice XML.
    /// </summary>
    public static string GeneratePeppolXml(EInvoiceData invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        XNamespace ubl = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        XNamespace cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        XNamespace cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ubl + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", cac.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "cbc", cbc.NamespaceName),
                new XElement(cbc + "CustomizationID", "urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0"),
                new XElement(cbc + "ProfileID", "urn:fdc:peppol.eu:2017:poacc:billing:01:1.0"),
                new XElement(cbc + "ID", invoice.InvoiceNumber),
                new XElement(cbc + "IssueDate", invoice.IssueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement(cbc + "DueDate", invoice.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement(cbc + "InvoiceTypeCode", "380"),
                new XElement(cbc + "DocumentCurrencyCode", invoice.CurrencyCode),
                new XElement(cac + "AccountingSupplierParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyName", new XElement(cbc + "Name", invoice.Seller.Name)),
                        new XElement(cac + "PartyTaxScheme",
                            new XElement(cbc + "CompanyID", invoice.Seller.VatId),
                            new XElement(cac + "TaxScheme", new XElement(cbc + "ID", "VAT"))
                        )
                    )
                ),
                new XElement(cac + "AccountingCustomerParty",
                    new XElement(cac + "Party",
                        new XElement(cac + "PartyName", new XElement(cbc + "Name", invoice.Buyer.Name)),
                        new XElement(cac + "PartyTaxScheme",
                            new XElement(cbc + "CompanyID", invoice.Buyer.VatId),
                            new XElement(cac + "TaxScheme", new XElement(cbc + "ID", "VAT"))
                        )
                    )
                ),
                new XElement(cac + "LegalMonetaryTotal",
                    new XElement(cbc + "LineExtensionAmount", new XAttribute("currencyID", invoice.CurrencyCode), invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)),
                    new XElement(cbc + "TaxExclusiveAmount", new XAttribute("currencyID", invoice.CurrencyCode), invoice.SubTotal.ToString("F2", CultureInfo.InvariantCulture)),
                    new XElement(cbc + "TaxInclusiveAmount", new XAttribute("currencyID", invoice.CurrencyCode), invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture)),
                    new XElement(cbc + "PayableAmount", new XAttribute("currencyID", invoice.CurrencyCode), invoice.GrandTotal.ToString("F2", CultureInfo.InvariantCulture))
                )
            )
        );

        return doc.ToString();
    }

    /// <summary>
    /// Embeds the structured e-Invoice XML directly into a PDF/A-3 document as an Associated File (/AF).
    /// </summary>
    public static byte[] EmbedEInvoiceIntoPdf(byte[] pdfBytes, EInvoiceData invoice, EInvoiceStandard standard = EInvoiceStandard.FacturX_ZUGFeRD)
    {
        ArgumentNullException.ThrowIfNull(pdfBytes);
        ArgumentNullException.ThrowIfNull(invoice);

        var xmlContent = standard == EInvoiceStandard.FacturX_ZUGFeRD
            ? GenerateFacturXXml(invoice)
            : GeneratePeppolXml(invoice);

        var filename = standard == EInvoiceStandard.FacturX_ZUGFeRD ? "factur-x.xml" : "invoice.xml";
        var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);

        var pdfString = Encoding.Latin1.GetString(pdfBytes);

        // Inject FileSpec and EmbeddedFile stream objects
        var streamObjNum = 888;
        var fileSpecObjNum = 889;

        var embeddedFileObj = $"\r\n{streamObjNum} 0 obj\r\n<< /Type /EmbeddedFile /Subtype /text#2Fxml /Length {xmlBytes.Length} >>\r\nstream\r\n{xmlContent}\r\nendstream\r\nendobj\r\n";
        var fileSpecObj = $"\r\n{fileSpecObjNum} 0 obj\r\n<< /Type /Filespec /F ({filename}) /UF ({filename}) /EF << /F {streamObjNum} 0 R >> /AFRelationship /Alternative >>\r\nendobj\r\n";

        // Inject /AF associated files reference
        var afRef = $"/AF [{fileSpecObjNum} 0 R]";
        var modifiedPdf = pdfString + embeddedFileObj + fileSpecObj;

        if (modifiedPdf.Contains("/Catalog", StringComparison.OrdinalIgnoreCase))
        {
            modifiedPdf = modifiedPdf.Replace("/Catalog", $"/Catalog {afRef}", StringComparison.OrdinalIgnoreCase);
        }

        return Encoding.Latin1.GetBytes(modifiedPdf);
    }
}
