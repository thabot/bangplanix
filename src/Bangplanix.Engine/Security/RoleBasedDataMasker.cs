using System.Text.RegularExpressions;

namespace Bangplanix.Engine.Security;

public enum DataMaskingType
{
    ThaiNationalId,
    CreditCard,
    Email,
    PhoneNumber,
    BankAccount,
    Salary,
    CustomRegex,
    FullMask
}

public sealed class DataMaskingRule
{
    public string FieldName { get; set; } = string.Empty;
    public DataMaskingType MaskingType { get; set; } = DataMaskingType.FullMask;
    public char MaskChar { get; set; } = '*';
    public HashSet<string> ExemptRoles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? CustomPattern { get; set; }
    public string? CustomReplacement { get; set; }
}

public sealed class UserSecurityContext
{
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public HashSet<string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> Claims { get; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Dynamic Role-Based Data Masking Engine for securing PII, financial, and confidential data at render time.
/// </summary>
public static class RoleBasedDataMasker
{
    public static string MaskThaiNationalId(string value, char maskChar = 'X')
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 13) return new string(maskChar, value.Length);

        // Standard Thai ID: 1-2345-67890-12-3 -> 1-XXXX-XXXXX-12-3
        return $"{digits[0]}-{new string(maskChar, 4)}-{new string(maskChar, 5)}-{digits[10..12]}-{digits[12]}";
    }

    public static string MaskCreditCard(string value, char maskChar = '*')
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length < 12) return new string(maskChar, value.Length);

        var last4 = digits[^4..];
        return $"****-****-****-{last4}";
    }

    public static string MaskEmail(string value, char maskChar = '*')
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@')) return value;

        var parts = value.Split('@');
        var name = parts[0];
        var domain = parts[1];

        if (name.Length <= 2)
        {
            return $"{name[0]}{maskChar}@{domain}";
        }

        var maskedName = $"{name[0]}{new string(maskChar, Math.Min(name.Length - 2, 4))}{name[^1]}";
        return $"{maskedName}@{domain}";
    }

    public static string MaskPhoneNumber(string value, char maskChar = 'X')
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            // 0812345678 -> 081-XXX-5678
            return $"{digits[..3]}-{new string(maskChar, 3)}-{digits[6..]}";
        }
        if (digits.Length == 9)
        {
            // 021234567 -> 02-XXX-4567
            return $"{digits[..2]}-{new string(maskChar, 3)}-{digits[5..]}";
        }
        return new string(maskChar, value.Length);
    }

    public static string MaskSalary(object? value, string mask = "***,***.00")
    {
        return mask;
    }

    /// <summary>
    /// Masks a single field value based on policy and user credentials.
    /// </summary>
    public static object? MaskValue(string fieldName, object? value, DataMaskingRule rule, UserSecurityContext? userContext)
    {
        if (value is null || rule is null) return value;

        // If user holds any exempt role, return original value unmasked
        if (userContext != null && rule.ExemptRoles.Count > 0 && rule.ExemptRoles.Overlaps(userContext.Roles))
        {
            return value;
        }

        var strVal = value.ToString() ?? string.Empty;

        return rule.MaskingType switch
        {
            DataMaskingType.ThaiNationalId => MaskThaiNationalId(strVal, rule.MaskChar),
            DataMaskingType.CreditCard => MaskCreditCard(strVal, rule.MaskChar),
            DataMaskingType.Email => MaskEmail(strVal, rule.MaskChar),
            DataMaskingType.PhoneNumber => MaskPhoneNumber(strVal, rule.MaskChar),
            DataMaskingType.Salary => MaskSalary(value),
            DataMaskingType.CustomRegex when !string.IsNullOrEmpty(rule.CustomPattern) =>
                Regex.Replace(strVal, rule.CustomPattern, rule.CustomReplacement ?? new string(rule.MaskChar, 4)),
            _ => new string(rule.MaskChar, strVal.Length)
        };
    }

    /// <summary>
    /// Applies role-based masking rules across a collection of data rows.
    /// </summary>
    public static List<Dictionary<string, object?>> MaskRows(
        IEnumerable<Dictionary<string, object?>> rows,
        IEnumerable<DataMaskingRule> rules,
        UserSecurityContext? userContext)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(rules);

        var ruleMap = rules.ToDictionary(r => r.FieldName, r => r, StringComparer.OrdinalIgnoreCase);
        var result = new List<Dictionary<string, object?>>();

        foreach (var row in rows)
        {
            var newRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in row)
            {
                if (ruleMap.TryGetValue(k, out var rule))
                {
                    newRow[k] = MaskValue(k, v, rule, userContext);
                }
                else
                {
                    newRow[k] = v;
                }
            }
            result.Add(newRow);
        }

        return result;
    }
}
