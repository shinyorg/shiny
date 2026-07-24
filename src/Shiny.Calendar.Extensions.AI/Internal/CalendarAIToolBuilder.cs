namespace Shiny.Calendar.Extensions.AI.Internal;

sealed class CalendarAIToolBuilder : ICalendarAIToolBuilder
{
    public CalendarAICapabilities Capabilities { get; private set; }

    public ICalendarAIToolBuilder AddCalendar(CalendarAICapabilities capabilities = CalendarAICapabilities.Read)
    {
        this.Capabilities |= capabilities;
        return this;
    }
}
