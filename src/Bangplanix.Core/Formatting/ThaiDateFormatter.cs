namespace Bangplanix.Core.Formatting;

public static class ThaiDateFormatter
{
    private static readonly string[] FullMonthNames =
    [
        "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
        "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
    ];

    private static readonly string[] ShortMonthNames =
    [
        "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.",
        "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค."
    ];

    public static string FormatBuddhistDate(DateTime date, string format = "d MMMM yyyy")
    {
        var buddhistYear = date.Year + 543;
        var monthIndex = date.Month - 1;

        if (format.Equals("d MMMM yyyy", StringComparison.OrdinalIgnoreCase))
        {
            return $"{date.Day} {FullMonthNames[monthIndex]} {buddhistYear}";
        }

        if (format.Equals("d MMM yyyy", StringComparison.OrdinalIgnoreCase))
        {
            return $"{date.Day} {ShortMonthNames[monthIndex]} {buddhistYear}";
        }

        if (format.Equals("dd/MM/yyyy", StringComparison.OrdinalIgnoreCase))
        {
            return $"{date.Day:D2}/{date.Month:D2}/{buddhistYear}";
        }

        // Custom replacement
        return format
            .Replace("MMMM", FullMonthNames[monthIndex], StringComparison.OrdinalIgnoreCase)
            .Replace("MMM", ShortMonthNames[monthIndex], StringComparison.OrdinalIgnoreCase)
            .Replace("yyyy", buddhistYear.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("yy", (buddhistYear % 100).ToString("D2"), StringComparison.OrdinalIgnoreCase)
            .Replace("dd", date.Day.ToString("D2"), StringComparison.OrdinalIgnoreCase)
            .Replace("d", date.Day.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
