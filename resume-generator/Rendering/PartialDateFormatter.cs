using System.Globalization;

namespace resume_generator.Rendering;

internal static class PartialDateFormatter
{
    private static readonly string[] DutchMonths =
    [
        "januari", "februari", "maart", "april", "mei", "juni",
        "juli", "augustus", "september", "oktober", "november", "december"
    ];

    public static string Format(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Trim().Split('-');

        switch (parts.Length)
        {
            case 1 when int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year):
                return year.ToString(CultureInfo.InvariantCulture);
            case 2 when int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year2)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
                && month >= 1 && month <= 12:
                return $"{DutchMonths[month - 1]} {year2.ToString(CultureInfo.InvariantCulture)}";
            case 3 when int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year3)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month3)
                && month3 >= 1 && month3 <= 12
                && int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day):
                return $"{day.ToString(CultureInfo.InvariantCulture)} {DutchMonths[month3 - 1]} {year3.ToString(CultureInfo.InvariantCulture)}";
            default:
                return value;
        }
    }

    public static string FormatRange(string? startDate, string? endDate)
    {
        var start = Format(startDate);
        var end = string.IsNullOrWhiteSpace(endDate) ? "heden" : Format(endDate);

        if (start.Length == 0)
        {
            return end;
        }

        return $"{start} – {end}";
    }
}
