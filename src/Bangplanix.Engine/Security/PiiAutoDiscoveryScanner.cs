using Bangplanix.Core.Models;

namespace Bangplanix.Engine.Security;

public enum PiiCategory
{
    ThaiNationalId,
    CreditCard,
    Email,
    PhoneNumber,
    SalaryFinancial,
    PersonalName,
    Address
}

public sealed class PiiDiscoveryItem
{
    public string FieldName { get; set; } = string.Empty;
    public PiiCategory Category { get; set; }
    public double Confidence { get; set; } = 1.0;
    public DataMaskingType RecommendedMasking { get; set; }
    public string LocationContext { get; set; } = string.Empty;
}

public sealed class PiiDiscoveryReport
{
    public int TotalFieldsScanned { get; set; }
    public int PiiFieldsCount => Items.Count;
    public List<PiiDiscoveryItem> Items { get; } = [];
}

/// <summary>
/// Automated PII (Personally Identifiable Information) scanner for PDPA & GDPR compliance enforcement.
/// </summary>
public static class PiiAutoDiscoveryScanner
{
    private static readonly Dictionary<string, (PiiCategory Category, DataMaskingType Masking)> PiiPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["citizen_id"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["citizenid"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["national_id"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["id_card"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["idcard"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["identification_number"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["ssn"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),
        ["passport"] = (PiiCategory.ThaiNationalId, DataMaskingType.ThaiNationalId),

        ["credit_card"] = (PiiCategory.CreditCard, DataMaskingType.CreditCard),
        ["creditcard"] = (PiiCategory.CreditCard, DataMaskingType.CreditCard),
        ["card_number"] = (PiiCategory.CreditCard, DataMaskingType.CreditCard),
        ["cardno"] = (PiiCategory.CreditCard, DataMaskingType.CreditCard),
        ["pan"] = (PiiCategory.CreditCard, DataMaskingType.CreditCard),

        ["email"] = (PiiCategory.Email, DataMaskingType.Email),
        ["email_address"] = (PiiCategory.Email, DataMaskingType.Email),
        ["e_mail"] = (PiiCategory.Email, DataMaskingType.Email),

        ["phone"] = (PiiCategory.PhoneNumber, DataMaskingType.PhoneNumber),
        ["phone_number"] = (PiiCategory.PhoneNumber, DataMaskingType.PhoneNumber),
        ["mobile"] = (PiiCategory.PhoneNumber, DataMaskingType.PhoneNumber),
        ["telephone"] = (PiiCategory.PhoneNumber, DataMaskingType.PhoneNumber),
        ["tel"] = (PiiCategory.PhoneNumber, DataMaskingType.PhoneNumber),

        ["salary"] = (PiiCategory.SalaryFinancial, DataMaskingType.Salary),
        ["wage"] = (PiiCategory.SalaryFinancial, DataMaskingType.Salary),
        ["compensation"] = (PiiCategory.SalaryFinancial, DataMaskingType.Salary),
        ["bonus"] = (PiiCategory.SalaryFinancial, DataMaskingType.Salary),
        ["bank_account"] = (PiiCategory.SalaryFinancial, DataMaskingType.BankAccount),
        ["account_number"] = (PiiCategory.SalaryFinancial, DataMaskingType.BankAccount),
        ["promptpay"] = (PiiCategory.SalaryFinancial, DataMaskingType.PhoneNumber)
    };

    /// <summary>
    /// Scans a report definition and its datasets/elements to discover potential PII.
    /// </summary>
    public static PiiDiscoveryReport ScanReport(ReportDefinition report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var result = new PiiDiscoveryReport();
        var scannedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Scan Datasets
        foreach (var ds in report.Datasets)
        {
            if (ds.StaticData is IEnumerable<Dictionary<string, object?>> rows)
            {
                var firstRow = rows.FirstOrDefault();
                if (firstRow != null)
                {
                    foreach (var key in firstRow.Keys)
                    {
                        if (scannedFields.Add(key))
                        {
                            result.TotalFieldsScanned++;
                            CheckAndAddField(key, $"Dataset: {ds.Name}", result);
                        }
                    }
                }
            }
        }

        // 2. Scan Parameters
        foreach (var param in report.Parameters)
        {
            if (scannedFields.Add(param.Name))
            {
                result.TotalFieldsScanned++;
                CheckAndAddField(param.Name, "Report Parameter", result);
            }
        }

        // 3. Scan Bands & Elements
        if (report.Bands != null)
        {
            var allElements = new List<(string BandName, ElementDefinition Element)>();
            if (report.Bands.ReportHeader != null) allElements.AddRange(report.Bands.ReportHeader.Elements.Select(e => ("ReportHeader", e)));
            if (report.Bands.PageHeader != null) allElements.AddRange(report.Bands.PageHeader.Elements.Select(e => ("PageHeader", e)));
            foreach (var gh in report.Bands.GroupHeaders) allElements.AddRange(gh.Elements.Select(e => ("GroupHeader", e)));
            if (report.Bands.Detail != null) allElements.AddRange(report.Bands.Detail.Elements.Select(e => ("Detail", e)));
            foreach (var gf in report.Bands.GroupFooters) allElements.AddRange(gf.Elements.Select(e => ("GroupFooter", e)));
            if (report.Bands.PageFooter != null) allElements.AddRange(report.Bands.PageFooter.Elements.Select(e => ("PageFooter", e)));
            if (report.Bands.ReportFooter != null) allElements.AddRange(report.Bands.ReportFooter.Elements.Select(e => ("ReportFooter", e)));

            foreach (var (bandName, elem) in allElements)
            {
                if (!string.IsNullOrEmpty(elem.Expression))
                {
                    var expr = elem.Expression;
                    foreach (var (keyword, (cat, mask)) in PiiPatterns)
                    {
                        if (expr.Contains(keyword, StringComparison.OrdinalIgnoreCase) && scannedFields.Add($"Expr_{keyword}"))
                        {
                            result.TotalFieldsScanned++;
                            result.Items.Add(new PiiDiscoveryItem
                            {
                                FieldName = keyword,
                                Category = cat,
                                Confidence = 0.95,
                                RecommendedMasking = mask,
                                LocationContext = $"Band: {bandName}, Element: {elem.Id ?? "Anon"}"
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Automatically converts a PiiDiscoveryReport into a collection of DataMaskingRules.
    /// </summary>
    public static List<DataMaskingRule> GenerateAutoMaskingRules(PiiDiscoveryReport report, IEnumerable<string>? exemptRoles = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var exempt = exemptRoles?.ToList() ?? ["Admin", "ComplianceOfficer", "Auditor"];
        var rules = new List<DataMaskingRule>();

        foreach (var item in report.Items)
        {
            var rule = new DataMaskingRule
            {
                FieldName = item.FieldName,
                MaskingType = item.RecommendedMasking,
                MaskChar = '*'
            };
            foreach (var role in exempt)
            {
                rule.ExemptRoles.Add(role);
            }
            rules.Add(rule);
        }

        return rules;
    }

    private static void CheckAndAddField(string fieldName, string location, PiiDiscoveryReport report)
    {
        foreach (var (keyword, (cat, mask)) in PiiPatterns)
        {
            if (fieldName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                report.Items.Add(new PiiDiscoveryItem
                {
                    FieldName = fieldName,
                    Category = cat,
                    Confidence = 1.0,
                    RecommendedMasking = mask,
                    LocationContext = location
                });
                return;
            }
        }
    }
}
