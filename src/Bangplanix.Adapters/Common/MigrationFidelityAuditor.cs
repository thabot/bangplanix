using System.Xml.Linq;

namespace Bangplanix.Adapters.Common;

/// <summary>
/// Fidelity assessment details for an individual legacy report file.
/// </summary>
public sealed class FileFidelityReport
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ReportFormat { get; set; } = "Unknown";
    public double FidelityScore { get; set; } = 100.0;
    public int TotalElements { get; set; }
    public int SupportedElements { get; set; }
    public int UnsupportedElements { get; set; }
    public int SubreportsCount { get; set; }
    public int DataSourcesCount { get; set; }
    public int FormulasCount { get; set; }
    public List<string> Warnings { get; } = [];
    public List<string> ActionItems { get; } = [];
}

/// <summary>
/// Comprehensive migration audit summary for an entire directory of legacy reports.
/// </summary>
public sealed class MigrationAuditReport
{
    public int TotalFilesAudited { get; set; }
    public double AverageFidelityScore { get; set; } = 100.0;
    public List<string> DetectedEngines { get; set; } = [];
    public int TotalElementsScanned { get; set; }
    public int SupportedElementsCount { get; set; }
    public int UnsupportedElementsCount { get; set; }
    public int SubreportsCount { get; set; }
    public int DataSourcesCount { get; set; }
    public List<FileFidelityReport> FileReports { get; } = [];
}

/// <summary>
/// Tool for pre-migration fidelity scoring, complexity analysis, and readiness auditing.
/// </summary>
public static class MigrationFidelityAuditor
{
    /// <summary>
    /// Audits an individual legacy report file's content and returns a detailed fidelity report.
    /// </summary>
    public static FileFidelityReport AuditFile(string filePath, string content)
    {
        var report = new FileFidelityReport
        {
            FilePath = filePath ?? string.Empty,
            FileName = string.IsNullOrEmpty(filePath) ? "Unknown" : Path.GetFileName(filePath)
        };

        if (string.IsNullOrWhiteSpace(content))
        {
            report.Warnings.Add("File is empty or contains whitespace only.");
            report.FidelityScore = 0.0;
            return report;
        }

        var upperName = report.FileName.ToUpperInvariant();
        if (upperName.EndsWith(".RPT.XML", StringComparison.Ordinal) || upperName.EndsWith(".CRYSTAL.XML", StringComparison.Ordinal) || content.Contains("<CrystalReport", StringComparison.OrdinalIgnoreCase))
        {
            report.ReportFormat = "SAP Crystal Reports";
        }
        else if (upperName.EndsWith(".RPTDESIGN", StringComparison.Ordinal) || content.Contains("<report xmlns=\"http://www.eclipse.org/birt", StringComparison.OrdinalIgnoreCase))
        {
            report.ReportFormat = "Eclipse BIRT";
        }
        else if (upperName.EndsWith(".REX", StringComparison.Ordinal) || content.Contains("<dataTemplate", StringComparison.OrdinalIgnoreCase) || content.Contains("<reportContext", StringComparison.OrdinalIgnoreCase))
        {
            report.ReportFormat = "Oracle Reports / BI Publisher";
        }
        else if (upperName.EndsWith(".RPX", StringComparison.Ordinal) || upperName.EndsWith(".RDLX", StringComparison.Ordinal) || content.Contains("<ActiveReportsLayout", StringComparison.OrdinalIgnoreCase))
        {
            report.ReportFormat = "ActiveReports";
        }
        else if (upperName.EndsWith(".RDL", StringComparison.Ordinal) || upperName.EndsWith(".RDLC", StringComparison.Ordinal))
        {
            report.ReportFormat = "SSRS / Power BI Paginated";
        }
        else if (upperName.EndsWith(".JRXML", StringComparison.Ordinal))
        {
            report.ReportFormat = "Jaspersoft";
        }
        else if (upperName.EndsWith(".FRX", StringComparison.Ordinal))
        {
            report.ReportFormat = "FastReport";
        }
        else if (upperName.EndsWith(".MRT", StringComparison.Ordinal))
        {
            report.ReportFormat = "Stimulsoft";
        }
        else if (upperName.EndsWith(".TRDX", StringComparison.Ordinal) || upperName.EndsWith(".TRDP", StringComparison.Ordinal))
        {
            report.ReportFormat = "Progress Telerik";
        }
        else if (upperName.EndsWith(".REPX", StringComparison.Ordinal))
        {
            report.ReportFormat = "DevExpress";
        }

        try
        {
            var doc = XDocument.Parse(content);
            var elements = doc.Descendants().ToList();
            report.TotalElements = elements.Count;

            // Check for Subreports
            var subreports = SubreportLinkageEngine.ExtractSubreports(doc);
            report.SubreportsCount = subreports.Count;

            // Check for Data sources / queries
            var queries = elements.Where(e =>
                e.Name.LocalName.Contains("Query", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("DataSet", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("DataSource", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("sqlStatement", StringComparison.OrdinalIgnoreCase)).ToList();
            report.DataSourcesCount = queries.Count;

            // Check for Formulas / Scripts
            var formulas = elements.Where(e =>
                e.Name.LocalName.Contains("Formula", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("Expression", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("Script", StringComparison.OrdinalIgnoreCase)).ToList();
            report.FormulasCount = formulas.Count;

            // Detect potential unsupported / high complexity elements (e.g. OLE, ActiveX, Unmanaged Code)
            var unsupported = elements.Where(e =>
                e.Name.LocalName.Contains("OLEObject", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("ActiveX", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Contains("CustomControl", StringComparison.OrdinalIgnoreCase)).ToList();

            report.UnsupportedElements = unsupported.Count;
            report.SupportedElements = Math.Max(0, report.TotalElements - report.UnsupportedElements);

            if (report.UnsupportedElements > 0)
            {
                report.Warnings.Add($"Found {report.UnsupportedElements} unsupported legacy elements (OLE / ActiveX).");
                report.ActionItems.Add("Manually review custom third-party controls and replace with Bangplanix native elements.");
            }

            if (report.SubreportsCount > 0)
            {
                report.ActionItems.Add($"Verify that {report.SubreportsCount} linked subreport file(s) are placed in the same directory.");
            }

            // Calculate fidelity score
            if (report.TotalElements > 0)
            {
                var baseScore = ((double)report.SupportedElements / report.TotalElements) * 100.0;
                // Penalize slightly if heavy custom scripts are present
                var scriptPenalty = content.Contains("<Script>", StringComparison.OrdinalIgnoreCase) ? 2.0 : 0.0;
                report.FidelityScore = Math.Round(Math.Clamp(baseScore - scriptPenalty, 10.0, 100.0), 1);
            }
            else
            {
                report.FidelityScore = 100.0;
            }
        }
        catch (System.Xml.XmlException ex)
        {
            report.Warnings.Add($"XML Parsing Warning: {ex.Message}");
            report.FidelityScore = 80.0;
        }

        return report;
    }

    /// <summary>
    /// Audits all report files within a directory and produces a comprehensive MigrationAuditReport.
    /// </summary>
    public static MigrationAuditReport AuditDirectory(string directoryPath, string searchPattern = "*.*", bool recursive = true)
    {
        var result = new MigrationAuditReport();
        if (!Directory.Exists(directoryPath)) return result;

        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directoryPath, searchPattern, opt);

        var engines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                var fileReport = AuditFile(file, content);

                result.FileReports.Add(fileReport);
                result.TotalFilesAudited++;
                result.TotalElementsScanned += fileReport.TotalElements;
                result.SupportedElementsCount += fileReport.SupportedElements;
                result.UnsupportedElementsCount += fileReport.UnsupportedElements;
                result.SubreportsCount += fileReport.SubreportsCount;
                result.DataSourcesCount += fileReport.DataSourcesCount;

                if (!string.Equals(fileReport.ReportFormat, "Unknown", StringComparison.OrdinalIgnoreCase))
                {
                    engines.Add(fileReport.ReportFormat);
                }
            }
            catch (IOException ex)
            {
                var errReport = new FileFidelityReport
                {
                    FilePath = file,
                    FileName = Path.GetFileName(file),
                    FidelityScore = 0.0
                };
                errReport.Warnings.Add($"Error reading file: {ex.Message}");
                result.FileReports.Add(errReport);
            }
        }

        result.DetectedEngines = engines.OrderBy(e => e).ToList();
        if (result.TotalFilesAudited > 0)
        {
            result.AverageFidelityScore = Math.Round(result.FileReports.Average(r => r.FidelityScore), 1);
        }

        return result;
    }
}
