using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bangplanix.Adapters.Access;
using Bangplanix.Adapters.ActiveReports;
using Bangplanix.Adapters.Adobe;
using Bangplanix.Adapters.BarTender;
using Bangplanix.Adapters.Birt;
using Bangplanix.Adapters.Cognos;
using Bangplanix.Adapters.Crystal;
using Bangplanix.Adapters.DevExpress;
using Bangplanix.Adapters.EscPos;
using Bangplanix.Adapters.FastReport;
using Bangplanix.Adapters.Handlebars;
using Bangplanix.Adapters.Jaspersoft;
using Bangplanix.Adapters.LabelPrinter;
using Bangplanix.Adapters.Office;
using Bangplanix.Adapters.Oracle;
using Bangplanix.Adapters.OracleBip;
using Bangplanix.Adapters.Pentaho;
using Bangplanix.Adapters.Ssrs;
using Bangplanix.Adapters.Stimulsoft;
using Bangplanix.Adapters.Telerik;
using Bangplanix.Adapters.Ubl;
using Bangplanix.Core.Models;

namespace Bangplanix.Adapters.Common;

public static class LegacyAdapterFactory
{
    private static readonly ILegacyReportAdapter[] Adapters =
    [
        new SsrsRdlAdapter(),
        new JaspersoftJrxmlAdapter(),
        new FastReportFrxAdapter(),
        new StimulsoftMrtAdapter(),
        new TelerikTrdxAdapter(),
        new DevExpressRepxAdapter(),
        new OfficeAndHtmlAdapter(),
        new CrystalReportsXmlAdapter(),
        new EclipseBirtAdapter(),
        new OracleReportsAdapter(),
        new ActiveReportsAdapter(),
        new PentahoPrptAdapter(),
        new OracleBipRtfAdapter(),
        new CognosSpecAdapter(),
        new HandlebarsHtmlAdapter(),
        new UblInvoiceAdapter(),
        new ZebraAndEplAdapter(),
        new EscPosReceiptAdapter(),
        new AdobeXdpAdapter(),
        new BarTenderBtwAdapter(),
        new MsAccessReportAdapter()
    ];

    public static IReadOnlyList<ILegacyReportAdapter> RegisteredAdapters => Adapters;

    public static ILegacyReportAdapter GetAdapterByExtension(string filePathOrExtension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePathOrExtension);

        // Check multi-part extensions first (e.g. .rpt.xml, .crystal.xml, .birt.xml, .ar.xml)
        foreach (var adapter in Adapters)
        {
            if (adapter.SupportedExtensions.Any(ext => filePathOrExtension.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                return adapter;
            }
        }

        var ext = Path.HasExtension(filePathOrExtension)
            ? Path.GetExtension(filePathOrExtension)
            : filePathOrExtension.StartsWith('.') ? filePathOrExtension : $".{filePathOrExtension}";

        var matched = Adapters.FirstOrDefault(a => a.SupportedExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)));
        if (matched != null) return matched;

        throw new NotSupportedException($"No legacy adapter found for file extension '{ext}'.");
    }

    public static ILegacyReportAdapter DetectAdapter(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var trimmed = content.Trim();

        // Check JSON (Stimulsoft MRT)
        if (trimmed.StartsWith('{') && (trimmed.Contains("\"ReportVersion\"", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("\"Pages\"", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("\"StiReport\"", StringComparison.OrdinalIgnoreCase)))
        {
            return new StimulsoftMrtAdapter();
        }

        // XML Content Sniffing
        if (trimmed.Contains("<CrystalReport", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<CrystalReports", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("CrystalReportViewer", StringComparison.OrdinalIgnoreCase))
        {
            return new CrystalReportsXmlAdapter();
        }

        if (trimmed.Contains("http://www.eclipse.org/birt/2005/design", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<simple-master-page", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<oda-data-set", StringComparison.OrdinalIgnoreCase))
        {
            return new EclipseBirtAdapter();
        }

        if (trimmed.Contains("<ActiveReportsLayout", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<ActiveReport", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("ActiveReports.SectionReport", StringComparison.OrdinalIgnoreCase))
        {
            return new ActiveReportsAdapter();
        }

        if (trimmed.Contains("<dataTemplate", StringComparison.OrdinalIgnoreCase) || (trimmed.Contains("<report", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("<dataQuery>", StringComparison.OrdinalIgnoreCase)))
        {
            return new OracleReportsAdapter();
        }

        if (trimmed.Contains("<Report ", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<Report>", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("xmlns=\"http://schemas.microsoft.com/sqlserver/reporting/", StringComparison.OrdinalIgnoreCase))
        {
            return new SsrsRdlAdapter();
        }

        if (trimmed.Contains("<jasperReport ", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<jasperReport>", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("http://jasperreports.sourceforge.net/jasperreports", StringComparison.OrdinalIgnoreCase))
        {
            return new JaspersoftJrxmlAdapter();
        }

        if (trimmed.Contains("<Report ", StringComparison.OrdinalIgnoreCase) && (trimmed.Contains("<Dictionary>", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<ReportPage", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("ReportInfo", StringComparison.OrdinalIgnoreCase)))
        {
            return new FastReportFrxAdapter();
        }

        if (trimmed.Contains("<StiSerializer", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<StiReport", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Stimulsoft", StringComparison.OrdinalIgnoreCase))
        {
            return new StimulsoftMrtAdapter();
        }

        if (trimmed.Contains("<XtraReportsLayoutSerializer", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("DevExpress.XtraReports", StringComparison.OrdinalIgnoreCase))
        {
            return new DevExpressRepxAdapter();
        }

        if (trimmed.Contains("<Report ", StringComparison.OrdinalIgnoreCase) && (trimmed.Contains("http://schemas.telerik.com/reporting/", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<PageSettings", StringComparison.OrdinalIgnoreCase)))
        {
            return new TelerikTrdxAdapter();
        }

        if (trimmed.Contains("http://ns.adobe.com/xdp/", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("http://www.xfa.org/schema/xfa-template/", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<xdp:xdp", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<template xmlns=", StringComparison.OrdinalIgnoreCase))
        {
            return new AdobeXdpAdapter();
        }

        if (trimmed.Contains("<BarTenderFormat", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("\"labelSetup\"", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("BarTender", StringComparison.OrdinalIgnoreCase))
        {
            return new BarTenderBtwAdapter();
        }

        if (trimmed.StartsWith("Begin Report", StringComparison.OrdinalIgnoreCase) || (trimmed.Contains("RecordSource =", StringComparison.OrdinalIgnoreCase) && trimmed.Contains("Begin Section", StringComparison.OrdinalIgnoreCase)))
        {
            return new MsAccessReportAdapter();
        }

        if (trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("{{", StringComparison.OrdinalIgnoreCase))
        {
            return new OfficeAndHtmlAdapter();
        }

        throw new NotSupportedException("Unable to detect legacy report format from content.");
    }

    public static ReportDefinition ConvertFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Legacy report file not found: {filePath}", filePath);

        var adapter = GetAdapterByExtension(filePath);
        using var stream = File.OpenRead(filePath);
        return adapter.Convert(stream);
    }
}