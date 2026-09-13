using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bangplanix.Adapters.Common;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Ubl;

public sealed class UblInvoiceAdapter : ILegacyReportAdapter
{
    public string FormatName => "Universal Business Language (UBL 2.1 / Peppol)";
    public IReadOnlyList<string> SupportedExtensions => [".ubl", ".ubl.xml", ".peppol.xml", ".inv.xml"];

    public ReportDefinition Convert(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream);
        return Convert(reader.ReadToEnd());
    }

    public ReportDefinition Convert(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var sanitized = LegacyScriptSanitizer.Sanitize(content);
        var doc = XDocument.Parse(sanitized);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid UBL 2.1 XML document.");

        var idVal = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "ID")?.Value ?? "INV-001";
        var issueDate = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "IssueDate")?.Value ?? DateTime.UtcNow.ToString("yyyy-MM-dd");
        var supplierName = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "AccountingSupplierParty")?.Descendants().FirstOrDefault(e => e.Name.LocalName == "RegistrationName" || e.Name.LocalName == "Name")?.Value ?? "Supplier Name";
        var customerName = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "AccountingCustomerParty")?.Descendants().FirstOrDefault(e => e.Name.LocalName == "RegistrationName" || e.Name.LocalName == "Name")?.Value ?? "Customer Name";
        var payableAmount = root.Descendants().FirstOrDefault(e => e.Name.LocalName == "PayableAmount")?.Value ?? "0.00";

        var report = new ReportDefinition
        {
            Version = "1.0",
            Metadata = new ReportMetadata
            {
                Title = $"Invoice {idVal}",
                Author = "UBL 2.1 / Peppol Invoice Converter"
            },
            PageSetup = new PageSetup
            {
                PaperKind = PaperKind.A4,
                Orientation = PageOrientation.Portrait,
                Width = 595.28,
                Height = 841.89,
                Margins = new MarginDefinition { Left = 36, Right = 36, Top = 36, Bottom = 36 }
            }
        };

        var headerBand = new BandDefinition
        {
            Height = 120,
            Elements =
            [
                new ElementDefinition { Type = ElementType.Text, X = 20, Y = 10, Width = 300, Height = 25, Text = "TAX INVOICE / RECEIPT", Style = new StyleDefinition { FontSize = 16, FontWeight = "Bold" } },
                new ElementDefinition { Type = ElementType.Text, X = 350, Y = 10, Width = 200, Height = 20, Text = $"Invoice No: {idVal}", Style = new StyleDefinition { FontWeight = "Bold" } },
                new ElementDefinition { Type = ElementType.Text, X = 350, Y = 35, Width = 200, Height = 20, Text = $"Date: {issueDate}" },
                new ElementDefinition { Type = ElementType.Text, X = 20, Y = 50, Width = 250, Height = 20, Text = $"Supplier: {supplierName}", Style = new StyleDefinition { FontWeight = "Bold" } },
                new ElementDefinition { Type = ElementType.Text, X = 20, Y = 75, Width = 250, Height = 20, Text = $"Customer: {customerName}" },
                new ElementDefinition { Type = ElementType.Shape, ShapeType = ShapeType.Line, X = 20, Y = 110, Width = 540, Height = 1 }
            ]
        };
        report.Bands.ReportHeader = headerBand;

        var invoiceLines = root.Descendants().Where(e => e.Name.LocalName == "InvoiceLine" || e.Name.LocalName == "CreditNoteLine");
        var dataset = new DatasetDefinition
        {
            Name = "InvoiceLines",
            QueryOrUrl = "SELECT * FROM InvoiceLines"
        };
        report.Datasets.Add(dataset);

        // Detail Band for line items
        var detailBand = new BandDefinition
        {
            Height = 30,
            Elements =
            [
                new ElementDefinition { Type = ElementType.Text, X = 20, Y = 5, Width = 250, Height = 20, Expression = "=Fields.ItemName" },
                new ElementDefinition { Type = ElementType.Text, X = 280, Y = 5, Width = 60, Height = 20, Expression = "=Fields.InvoicedQuantity" },
                new ElementDefinition { Type = ElementType.Text, X = 350, Y = 5, Width = 90, Height = 20, Expression = "=Fields.PriceAmount", Style = new StyleDefinition { Format = "N2" } },
                new ElementDefinition { Type = ElementType.Text, X = 450, Y = 5, Width = 100, Height = 20, Expression = "=Fields.LineExtensionAmount", Style = new StyleDefinition { Format = "N2" } }
            ]
        };
        report.Bands.Detail = detailBand;

        // Footer Band (Totals)
        var footerBand = new BandDefinition
        {
            Height = 60,
            Elements =
            [
                new ElementDefinition { Type = ElementType.Shape, ShapeType = ShapeType.Line, X = 20, Y = 5, Width = 540, Height = 1 },
                new ElementDefinition { Type = ElementType.Text, X = 320, Y = 15, Width = 120, Height = 20, Text = "Total Amount Due:", Style = new StyleDefinition { FontWeight = "Bold" } },
                new ElementDefinition { Type = ElementType.Text, X = 450, Y = 15, Width = 100, Height = 20, Text = payableAmount, Style = new StyleDefinition { FontWeight = "Bold" } }
            ]
        };
        report.Bands.ReportFooter = footerBand;

        return report;
    }
}
