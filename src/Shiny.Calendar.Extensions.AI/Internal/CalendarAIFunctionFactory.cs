using Microsoft.Extensions.AI;

namespace Shiny.Calendar.Extensions.AI.Internal;

static class CalendarAIFunctionFactory
{
    public static IReadOnlyList<AITool> Build(ICalendarStore store, CalendarAICapabilities capabilities)
    {
        var tools = new List<AITool>();

        if (capabilities.HasFlag(CalendarAICapabilities.Read))
        {
            tools.Add(new ListCalendarsFunction(store));
            tools.Add(new SearchEventsFunction(store));
            tools.Add(new GetEventFunction(store));
        }

        if (capabilities.HasFlag(CalendarAICapabilities.Create))
            tools.Add(new CreateEventFunction(store));

        if (capabilities.HasFlag(CalendarAICapabilities.Update))
            tools.Add(new UpdateEventFunction(store));

        if (capabilities.HasFlag(CalendarAICapabilities.Delete))
            tools.Add(new DeleteEventFunction(store));

        return tools;
    }
}
