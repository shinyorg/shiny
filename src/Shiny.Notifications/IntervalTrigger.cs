using System;

namespace Shiny.Notifications;


/// <summary>
/// A repeating trigger that fires a notification on a recurring interval, daily at a fixed time, or weekly on a chosen day.
/// </summary>
public class IntervalTrigger
{
    /// <summary>
    /// When combined with <see cref="TimeOfDay"/>, restricts firing to this day of the week.
    /// </summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// The local time-of-day at which the notification fires. Mutually exclusive with <see cref="Interval"/>.
    /// </summary>
    public TimeSpan? TimeOfDay { get; set; }

    /// <summary>
    /// The repeating interval between fires. Mutually exclusive with <see cref="TimeOfDay"/>.
    /// </summary>
    public TimeSpan? Interval { get; set; }

    /// <summary>
    /// Clock source used by <see cref="CalculateNextAlarm()"/>. Defaults to <see cref="TimeProvider.System"/>;
    /// tests can substitute a <see cref="FakeTimeProvider"/> for deterministic alarm calculation.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;


    /// <summary>
    /// Throws if both <see cref="Interval"/> and <see cref="TimeOfDay"/> are set, neither is set, or <see cref="TimeOfDay"/> exceeds 24 hours.
    /// </summary>
    public void AssertValid()
    {
        if (this.Interval == null && this.TimeOfDay == null)
            throw new InvalidOperationException("You must set TimeOfDay or Interval");

        if (this.Interval != null && this.TimeOfDay != null)
            throw new InvalidOperationException("TimeOfDay and Interval cannot be set");

        if (this.TimeOfDay != null && this.TimeOfDay.Value.TotalMinutes > (24 * 60))
            throw new InvalidOperationException("TimeOfDay must be within 24 hours");
    }


    /// <summary>
    /// Calculates the next UTC time the trigger should fire based on the configured interval or time-of-day (and optional day-of-week).
    /// <see cref="TimeOfDay"/> is a local time in <see cref="TimeProvider"/>'s local time zone - today when it is still ahead,
    /// otherwise the next day (or the next matching <see cref="DayOfWeek"/>).
    /// </summary>
    /// <returns>The next fire time in UTC.</returns>
    public DateTime CalculateNextAlarm()
        => this.CalculateNextAlarm(this.TimeProvider.GetUtcNow()).UtcDateTime;


    /// <summary>
    /// The first time the trigger fires strictly after <paramref name="after"/>.
    /// </summary>
    internal DateTimeOffset CalculateNextAlarm(DateTimeOffset after)
    {
        if (this.Interval != null)
            return after.ToUniversalTime().Add(this.Interval.Value);

        var tz = this.TimeProvider.LocalTimeZone;
        var time = this.TimeOfDay!.Value;
        var day = TimeZoneInfo.ConvertTime(after, tz).Date;
        var next = AtLocal(day, time, tz);

        while (next <= after || (this.DayOfWeek != null && day.DayOfWeek != this.DayOfWeek.Value))
        {
            day = day.AddDays(1);
            next = AtLocal(day, time, tz);
        }
        return next.ToUniversalTime();
    }


    static DateTimeOffset AtLocal(DateTime day, TimeSpan time, TimeZoneInfo tz)
    {
        var local = DateTime.SpecifyKind(day.Add(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, tz.GetUtcOffset(local));
    }
}
