namespace Shiny.Calendar.Extensions.AI.Internal;

sealed class CalendarAIToolBuilder : ICalendarAIToolBuilder
{
    readonly Dictionary<string, CalendarAICapabilities> perCalendar = new(StringComparer.Ordinal);
    CalendarAICapabilities global;

    public ICalendarAIToolBuilder AddCalendar(CalendarAICapabilities capabilities = CalendarAICapabilities.Read)
    {
        this.global |= capabilities;
        return this;
    }

    public ICalendarAIToolBuilder AddCalendar(string calendarId, CalendarAICapabilities capabilities = CalendarAICapabilities.Read)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId);

        // Repeat calls for the same id are additive, but a lone None entry still stands
        // (None | None == None) so a calendar can be deliberately excluded from a global grant.
        this.perCalendar[calendarId] = this.perCalendar.TryGetValue(calendarId, out var existing)
            ? existing | capabilities
            : capabilities;

        return this;
    }

    public ICalendarAIToolBuilder AddCalendars(IEnumerable<string> calendarIds, CalendarAICapabilities capabilities = CalendarAICapabilities.Read)
    {
        ArgumentNullException.ThrowIfNull(calendarIds);

        foreach (var id in calendarIds)
            this.AddCalendar(id, capabilities);

        return this;
    }

    public CalendarAccessPolicy BuildPolicy() => new(this.global, this.perCalendar);
}
