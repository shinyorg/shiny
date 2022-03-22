using System;


namespace Shiny.Notifications
{
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
    }
}
