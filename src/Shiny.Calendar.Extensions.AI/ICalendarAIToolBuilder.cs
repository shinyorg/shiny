namespace Shiny.Calendar.Extensions.AI;

/// <summary>
/// Opt-in builder for the calendar operations an AI agent is allowed to perform. Nothing is exposed
/// to the LLM unless you add it here. Capabilities can be granted globally (every calendar on the
/// device) and/or per calendar id.
/// </summary>
public interface ICalendarAIToolBuilder
{
    /// <summary>
    /// Allows the AI agent to work with <b>every</b> calendar on the device. Defaults to
    /// <see cref="CalendarAICapabilities.Read"/>; combine flags (e.g. <c>Read | Create</c>,
    /// <see cref="CalendarAICapabilities.Write"/>, or <see cref="CalendarAICapabilities.All"/>) to expose
    /// create/update/delete tools independently. Repeat calls are additive.
    /// </summary>
    /// <param name="capabilities">The operations to allow on all calendars.</param>
    ICalendarAIToolBuilder AddCalendar(CalendarAICapabilities capabilities = CalendarAICapabilities.Read);

    /// <summary>
    /// Allows the AI agent to work with a single calendar. The entry <b>replaces</b> the global set for
    /// that calendar, so it can widen access (e.g. global <c>Read</c> plus <c>All</c> on your work
    /// calendar) or narrow it - passing <see cref="CalendarAICapabilities.None"/> hides the calendar from
    /// the agent entirely even when a global capability was granted.
    /// </summary>
    /// <param name="calendarId">The platform calendar id (see <c>ICalendarStore.GetAll</c>).</param>
    /// <param name="capabilities">The operations to allow on that calendar.</param>
    /// <remarks>
    /// When no global capability is granted, the per-calendar entries act as a strict allow-list: the
    /// agent only ever sees and touches those calendars.
    /// </remarks>
    ICalendarAIToolBuilder AddCalendar(string calendarId, CalendarAICapabilities capabilities = CalendarAICapabilities.Read);

    /// <summary>
    /// Applies <paramref name="capabilities"/> to several calendar ids at once. Equivalent to calling
    /// <see cref="AddCalendar(string, CalendarAICapabilities)"/> for each id.
    /// </summary>
    /// <param name="calendarIds">The platform calendar ids.</param>
    /// <param name="capabilities">The operations to allow on those calendars.</param>
    ICalendarAIToolBuilder AddCalendars(IEnumerable<string> calendarIds, CalendarAICapabilities capabilities = CalendarAICapabilities.Read);
}
