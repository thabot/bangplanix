using System.Text;

namespace Bangplanix.Core.Formatting;

public static class BahtTextFormatter
{
    private static readonly string[] Digits = ["ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า"];
    private static readonly string[] Positions = ["", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน"];

    public static string ToBahtText(decimal amount)
    {
        if (amount == 0m)
        {
            return "ศูนย์บาทถ้วน";
        }

        var isNegative = amount < 0m;
        if (isNegative)
        {
            amount = Math.Abs(amount);
        }

        // Round to 2 decimal places for currency
        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        var integerPart = (long)Math.Truncate(amount);
        var fractionPart = (int)Math.Round((amount - integerPart) * 100m, MidpointRounding.AwayFromZero);

        var sb = new StringBuilder();
        if (isNegative)
        {
            sb.Append("ลบ");
        }

        if (integerPart == 0 && fractionPart > 0)
        {
            // Only satang, no baht
            sb.Append(ConvertChunk(fractionPart));
            sb.Append("สตางค์");
            return sb.ToString();
        }

        sb.Append(ConvertIntegerToThai(integerPart));
        sb.Append("บาท");

        if (fractionPart == 0)
        {
            sb.Append("ถ้วน");
        }
        else
        {
            sb.Append(ConvertChunk(fractionPart));
            sb.Append("สตางค์");
        }

        return sb.ToString();
    }

    private static string ConvertIntegerToThai(long number)
    {
        if (number == 0)
        {
            return Digits[0];
        }

        // Handle millions recursively
        const long OneMillion = 1_000_000L;
        if (number >= OneMillion)
        {
            var millions = number / OneMillion;
            var remainder = number % OneMillion;

            var result = ConvertIntegerToThai(millions) + "ล้าน";
            if (remainder > 0)
            {
                result += ConvertChunk((int)remainder);
            }
            return result;
        }

        return ConvertChunk((int)number);
    }

    private static string ConvertChunk(int number)
    {
        if (number == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var numStr = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var len = numStr.Length;

        for (int i = 0; i < len; i++)
        {
            var digit = numStr[i] - '0';
            var pos = len - i - 1; // 0 = unit, 1 = tens, 2 = hundreds...

            if (digit != 0)
            {
                if (pos == 0 && digit == 1 && len > 1 && numStr[len - 2] != '0')
                {
                    sb.Append("เอ็ด");
                }
                else if (pos == 1 && digit == 1)
                {
                    // "สิบ"
                }
                else if (pos == 1 && digit == 2)
                {
                    sb.Append("ยี่");
                }
                else
                {
                    sb.Append(Digits[digit]);
                }

                sb.Append(Positions[pos]);
            }
        }

        return sb.ToString();
    }
}
