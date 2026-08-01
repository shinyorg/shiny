using Microsoft.Extensions.AI;

namespace Shiny.Calendar.Extensions.AI.Internal;

static class CalendarAIFunctionFactory
{
    public static IReadOnlyList<AITool> Build(ICalendarStore store, CalendarAccessPolicy policy)
    {
        var tools = new List<AITool>();

        // A tool is handed to the LLM when *any* calendar grants the capability - the tool itself then
        // enforces the per-calendar rules on every call.
        var capabilities = policy.Effective;

        if (capabilities.HasFlag(CalendarAICapabilities.Read))
        {
            tools.Add(new ListCalendarsFunction(store, policy));
            tools.Add(new SearchEventsFunction(store, policy));
            tools.Add(new GetEventFunction(store, policy));
        }

        if (capabilities.HasFlag(CalendarAICapabilities.Create))
            tools.Add(new CreateEventFunction(store, policy));

        if (capabilities.HasFlag(CalendarAICapabilities.Update))
            tools.Add(new UpdateEventFunction(store, policy));

        if (capabilities.HasFlag(CalendarAICapabilities.Delete))
            tools.Add(new DeleteEventFunction(store, policy));

        return tools;
    }
}
