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
    /// Throws if both <see cref="Interval"/> and <see cref="TimeOfDay"/> are set, neither is set, or <see cref="TimeOfDay"/> exceeds 24 hours.
    /// </summary>
    public void AssertValid()
    {
        if (this.Interval == null && this.TimeOfDay == null)
            throw new InvalidOperationException("You must set TimeOfDay or Interval");

        if (this.Interval != null && this.TimeOfDay != null)
            throw new InvalidOperationException("TimeOfDay and Interval cannot be set");

        if (this.TimeOfDay!.Value.TotalMinutes > (24 * 60))
            throw new InvalidOperationException("TimeOfDay must be within 24 hours");
    }


    /// <summary>
    /// Calculates the next UTC time the trigger should fire based on the configured interval or time-of-day (and optional day-of-week).
    /// </summary>
    /// <returns>The next fire time in UTC.</returns>
    public DateTime CalculateNextAlarm()
    {
        if (this.Interval != null)
            return DateTime.UtcNow.Add(this.Interval.Value);

        var now = DateTime.UtcNow;
        var time = this.TimeOfDay!.Value;

        var dt = new DateTime(
            now.Year,
            now.Month,
            now.Day + 1,
            time.Hours,
            time.Minutes,
            time.Seconds
        );

        if (this.DayOfWeek != null)
        {
            var day = this.DayOfWeek!.Value;
            while (dt.DayOfWeek != day)
                dt = dt.AddDays(1);
        }
        return dt;
    }
}
