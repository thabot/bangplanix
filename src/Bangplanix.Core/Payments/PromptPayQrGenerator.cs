using System.Globalization;
using System.Text;

namespace Bangplanix.Core.Payments;

public static class PromptPayQrGenerator
{
    private const string TagPayloadFormat = "00";
    private const string TagPointOfInitiation = "01";
    private const string TagMerchantAccountPromptPay = "29";
    private const string TagMerchantAccountBillPayment = "30";
    private const string TagTransactionCurrency = "53";
    private const string TagTransactionAmount = "54";
    private const string TagCountryCode = "58";
    private const string TagCrc = "63";

    private const string AidPromptPayTransfer = "A000000677010111";
    private const string AidPromptPayBillPayment = "A000000677010112";

    public static string GeneratePromptPayPayload(string targetId, decimal? amount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var cleanedId = targetId.Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).Trim();

        var sb = new StringBuilder();

        // 00: Payload Format Indicator ("01")
        AppendField(sb, TagPayloadFormat, "01");

        // 01: Point of Initiation (11 = Static without amount, 12 = Dynamic with amount)
        AppendField(sb, TagPointOfInitiation, amount.HasValue ? "12" : "11");

        // 29: Merchant Account Info (PromptPay)
        var merchantSb = new StringBuilder();
        AppendField(merchantSb, "00", AidPromptPayTransfer);

        if (cleanedId.Length is 10 or 9 && cleanedId.StartsWith("0", StringComparison.Ordinal)) // Thai Mobile
        {
            var mobile = "0066" + cleanedId[1..];
            AppendField(merchantSb, "01", mobile);
        }
        else if (cleanedId.Length == 13) // Citizen ID / Tax ID
        {
            AppendField(merchantSb, "02", cleanedId);
        }
        else if (cleanedId.Length == 15) // E-Wallet ID
        {
            AppendField(merchantSb, "03", cleanedId);
        }
        else
        {
            // Default sub-tag 01
            AppendField(merchantSb, "01", cleanedId);
        }

        AppendField(sb, TagMerchantAccountPromptPay, merchantSb.ToString());

        // 58: Country Code ("TH")
        AppendField(sb, TagCountryCode, "TH");

        // 53: Currency Code (764 = THB)
        AppendField(sb, TagTransactionCurrency, "764");

        // 54: Amount
        if (amount.HasValue && amount.Value > 0)
        {
            var amountStr = Math.Round(amount.Value, 2).ToString("F2", CultureInfo.InvariantCulture);
            AppendField(sb, TagTransactionAmount, amountStr);
        }

        // 63: CRC-16 Checksum
        sb.Append(TagCrc).Append("04");
        var crc = CalculateCrc16Ccitt(sb.ToString());
        sb.Append(crc);

        return sb.ToString();
    }

    public static string GenerateBillPaymentPayload(string billerId, string ref1, string? ref2 = null, decimal? amount = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(billerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ref1);

        var cleanedBillerId = billerId.Replace("-", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal).Trim();

        var sb = new StringBuilder();
        AppendField(sb, TagPayloadFormat, "01");
        AppendField(sb, TagPointOfInitiation, amount.HasValue ? "12" : "11");

        // 30: Bill Payment
        var merchantSb = new StringBuilder();
        AppendField(merchantSb, "00", AidPromptPayBillPayment);
        AppendField(merchantSb, "01", cleanedBillerId);
        AppendField(merchantSb, "02", ref1.Trim());
        if (!string.IsNullOrWhiteSpace(ref2))
        {
            AppendField(merchantSb, "03", ref2.Trim());
        }

        AppendField(sb, TagMerchantAccountBillPayment, merchantSb.ToString());
        AppendField(sb, TagCountryCode, "TH");
        AppendField(sb, TagTransactionCurrency, "764");

        if (amount.HasValue && amount.Value > 0)
        {
            var amountStr = Math.Round(amount.Value, 2).ToString("F2", CultureInfo.InvariantCulture);
            AppendField(sb, TagTransactionAmount, amountStr);
        }

        sb.Append(TagCrc).Append("04");
        var crc = CalculateCrc16Ccitt(sb.ToString());
        sb.Append(crc);

        return sb.ToString();
    }

    private static void AppendField(StringBuilder sb, string tag, string value)
    {
        var length = value.Length.ToString("D2", CultureInfo.InvariantCulture);
        sb.Append(tag).Append(length).Append(value);
    }

    public static string CalculateCrc16Ccitt(string input)
    {
        var bytes = Encoding.ASCII.GetBytes(input);
        ushort crc = 0xFFFF;
        const ushort polynomial = 0x1021;

        foreach (var b in bytes)
        {
            for (int i = 0; i < 8; i++)
            {
                var bit = ((b >> (7 - i)) & 1) == 1;
                var c15 = ((crc >> 15) & 1) == 1;
                crc <<= 1;
                if (c15 ^ bit)
                {
                    crc ^= polynomial;
                }
            }
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
