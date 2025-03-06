using System;

namespace Shiny.Notifications;


public class IntervalTrigger
{
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? TimeOfDay { get; set; }


    public TimeSpan? Interval { get; set; }


    public void AssertValid()
    {
        if (this.Interval == null && this.TimeOfDay == null)
            throw new InvalidOperationException("You must set TimeOfDay or Interval");

        if (this.Interval != null && this.TimeOfDay != null)
            throw new InvalidOperationException("TimeOfDay and Interval cannot be set");

        if (this.TimeOfDay!.Value.TotalMinutes > (24 * 60))
            throw new InvalidOperationException("TimeOfDay must be within 24 hours");
    }


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
