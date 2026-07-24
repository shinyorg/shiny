namespace Shiny.Calendar;

/// <summary>
/// A reminder/alarm that fires before an event starts.
/// </summary>
public class EventReminder
{
    public EventReminder() { }

    public EventReminder(TimeSpan offset) => this.Offset = offset;

    /// <summary>
    /// How far before the event's start the reminder fires (e.g. <c>TimeSpan.FromMinutes(15)</c> = 15
    /// minutes before). <see cref="TimeSpan.Zero"/> fires at the event start.
    /// </summary>
    public TimeSpan Offset { get; set; }

    public override string ToString() => $"{this.Offset} before start";
}
