using System;


namespace Shiny
{
    public struct DateRange
    {
        public DateRange(DateTimeOffset start, DateTimeOffset end)
        {
            this.Start = start;
            this.End = end;
        }


        public DateTimeOffset Start { get; }
        public DateTimeOffset End { get; }
    }


    public static class DateExtensions
    {
        public static DateRange GetRangeForDate(this DateTimeOffset date) => new DateRange(date.Date, date.GetEndOfDay());


        public static DateTime GetEndOfDay(this DateTimeOffset date)
            => date.Date.AddDays(1);
    }
}
