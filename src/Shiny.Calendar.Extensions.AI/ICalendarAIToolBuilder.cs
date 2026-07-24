namespace Shiny.Calendar.Extensions.AI;

/// <summary>
/// Opt-in builder for the calendar operations an AI agent is allowed to perform. Nothing is exposed
/// to the LLM unless you add it here.
/// </summary>
public interface ICalendarAIToolBuilder
{
    /// <summary>
    /// Allows the AI agent to work with device calendars/events. Defaults to
    /// <see cref="CalendarAICapabilities.Read"/>; combine flags (e.g. <c>Read | Create</c>,
    /// <see cref="CalendarAICapabilities.Write"/>, or <see cref="CalendarAICapabilities.All"/>) to expose
    /// create/update/delete tools independently.
    /// </summary>
    /// <param name="capabilities">The operations to allow.</param>
    ICalendarAIToolBuilder AddCalendar(CalendarAICapabilities capabilities = CalendarAICapabilities.Read);
}
