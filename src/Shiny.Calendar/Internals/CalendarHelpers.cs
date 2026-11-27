using System.Globalization;

namespace Shiny.Calendar.Internals;

/// <summary>
/// The id of one occurrence of a recurring event. Every occurrence of a series shares the platform's event id, so an
/// occurrence read from a date window carries <c>{eventId}|{original start, unix ms}</c> instead. The platform stores
/// decode it again, which is how <c>UpdateEvent</c>/<c>DeleteEvent</c>/<c>GetEvent</c> reach that one occurrence.
/// </summary>
static class EventOccurrenceId
{
    const char Separator = '|';

    public static string Encode(string eventId, DateTimeOffset originalStart)
        => eventId + Separator + originalStart.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);

    /// <summary>Splits an id into the platform event id and, for an occurrence id, its original start.</summary>
    public static (string EventId, DateTimeOffset? OriginalStart) Decode(string id)
    {
        var index = id.LastIndexOf(Separator);
        if (index <= 0 || index == id.Length - 1)
            return (id, null);

        var ms = id.Substring(index + 1);
        if (!Int64.TryParse(ms, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixMs))
            return (id, null);

        return (id.Substring(0, index), DateTimeOffset.FromUnixTimeMilliseconds(unixMs));
    }
}

/// <summary>RFC 5545 durations as Android's calendar provider stores them for recurring events (e.g. P3600S, PT1H, P1D).</summary>
static class RecurrenceDuration
{
    public static string ToRfc5545(TimeSpan duration, bool allDay)
    {
        if (duration < TimeSpan.Zero)
            duration = TimeSpan.Zero;

        if (allDay)
            return "P" + Math.Max(1, (int)Math.Ceiling(duration.TotalDays)).ToString(CultureInfo.InvariantCulture) + "D";

        return "P" + ((long)duration.TotalSeconds).ToString(CultureInfo.InvariantCulture) + "S";
    }

    public static TimeSpan? Parse(string? value)
    {
        if (String.IsNullOrWhiteSpace(value))
            return null;

        var s = value.Trim().ToUpperInvariant();
        var negative = s.StartsWith('-');
        s = s.TrimStart('+', '-');
        if (!s.StartsWith('P'))
            return null;

        var total = TimeSpan.Zero;
        var number = 0L;
        var hasNumber = false;
        foreach (var c in s.Substring(1))
        {
            if (Char.IsDigit(c))
            {
                number = number * 10 + (c - '0');
                hasNumber = true;
            }
            else if (c != 'T')
            {
                if (!hasNumber)
                    return null;

                TimeSpan? part = c switch
                {
                    'W' => TimeSpan.FromDays(number * 7),
                    'D' => TimeSpan.FromDays(number),
                    'H' => TimeSpan.FromHours(number),
                    'M' => TimeSpan.FromMinutes(number),
                    'S' => TimeSpan.FromSeconds(number),
                    _ => null
                };
                if (part == null)
                    return null;

                total += part.Value;
                number = 0;
                hasNumber = false;
            }
        }
        if (hasNumber)
            return null;

        return negative ? -total : total;
    }
}

/// <summary>
/// All-day events on Android are stored as UTC midnights in the UTC time zone, with an exclusive end. Shiny models
/// them as the device's local midnights (what EventKit hands out), so these convert between the two.
/// </summary>
static class AllDayDates
{
    /// <summary>The UTC-midnight start and exclusive end to store for an all-day event.</summary>
    public static (DateTimeOffset Start, DateTimeOffset End) ToStorage(DateTimeOffset start, DateTimeOffset end)
    {
        // the calendar day as the caller wrote it, in the offset it was written in
        var startDay = new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, TimeSpan.Zero);
        var endDay = new DateTimeOffset(end.Year, end.Month, end.Day, 0, 0, 0, TimeSpan.Zero);

        // an end exactly at midnight after the start is already exclusive; anything else ends with its own day
        var endIsExclusiveMidnight = end.TimeOfDay == TimeSpan.Zero && endDay > startDay;
        if (!endIsExclusiveMidnight)
            endDay = endDay.AddDays(1);

        if (endDay <= startDay)
            endDay = startDay.AddDays(1);

        return (startDay, endDay);
    }

    /// <summary>A stored UTC midnight as the same calendar day's local midnight.</summary>
    public static DateTimeOffset FromStorage(DateTimeOffset utcMidnight, TimeZoneInfo? zone = null)
    {
        zone ??= TimeZoneInfo.Local;
        var local = new DateTime(utcMidnight.UtcDateTime.Year, utcMidnight.UtcDateTime.Month, utcMidnight.UtcDateTime.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}

/// <summary>Edits to RFC 5545 recurrence rules.</summary>
static class RecurrenceRules
{
    /// <summary>
    /// Ends <paramref name="rrule"/> just before <paramref name="occurrence"/> (an UNTIL replacing any COUNT/UNTIL), which
    /// removes that occurrence and every later one. All-day series take a DATE UNTIL, timed ones a UTC DATE-TIME.
    /// </summary>
    public static string EndBefore(string rrule, DateTimeOffset occurrence, bool allDay)
    {
        var parts = rrule
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !p.StartsWith("UNTIL=", StringComparison.OrdinalIgnoreCase) && !p.StartsWith("COUNT=", StringComparison.OrdinalIgnoreCase));

        var until = allDay
            ? occurrence.UtcDateTime.AddDays(-1).ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : occurrence.UtcDateTime.AddSeconds(-1).ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        return String.Join(";", parts.Append("UNTIL=" + until));
    }
}
