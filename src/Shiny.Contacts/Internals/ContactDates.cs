using System.Globalization;

namespace Shiny.Contacts.Internals;

/// <summary>
/// Year-less contact dates (a birthday saved without a year). <see cref="DateOnly"/> always has a year, so a year-less
/// date is stored as year 1 - or year 4, the first leap year, for Feb 29, which year 1 cannot hold.
/// </summary>
static class ContactDates
{
    public const int NoYear = 1;
    public const int NoYearLeap = 4;

    public static bool IsYearless(DateOnly date)
        => date.Year == NoYear || (date.Year == NoYearLeap && date.Month == 2 && date.Day == 29);

    /// <summary>Builds a date from its parts, or null when the parts are not a real date. A null year is year-less.</summary>
    public static DateOnly? Create(int? year, int month, int day)
    {
        if (month < 1 || month > 12 || day < 1)
            return null;

        var y = year is >= 1 and <= 9999 ? year.Value : (month == 2 && day == 29 ? NoYearLeap : NoYear);
        if (day > DateTime.DaysInMonth(y, month))
            return null;

        return new DateOnly(y, month, day);
    }

    /// <summary>Parses the vCard-style values Android stores: yyyy-MM-dd, yyyyMMdd, or --MM-dd for no year.</summary>
    public static DateOnly? Parse(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        var s = value.Trim();
        if (s.StartsWith("--", StringComparison.Ordinal))
        {
            var parts = s.Substring(2).Split('-');
            if (parts.Length == 2 &&
                Int32.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var m) &&
                Int32.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var d))
            {
                return Create(null, m, d);
            }
            return null;
        }

        if (DateOnly.TryParseExact(s, ["yyyy-MM-dd", "yyyyMMdd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
            return exact;

        // some sync adapters store a full timestamp (e.g. 1990-05-06T00:00:00.000Z)
        if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
            return DateOnly.FromDateTime(dto.UtcDateTime);

        return null;
    }

    /// <summary>Formats for Android: yyyy-MM-dd, or --MM-dd for a year-less date.</summary>
    public static string Format(DateOnly date)
        => IsYearless(date)
            ? date.ToString("'--'MM-dd", CultureInfo.InvariantCulture)
            : date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
