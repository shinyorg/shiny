using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Calendar.Extensions.AI.Internal;

/// <summary>Lists the calendars available on the device.</summary>
sealed class ListCalendarsFunction : CalendarAIFunctionBase
{
    public ListCalendarsFunction(ICalendarStore store)
        : base(store, "list_calendars",
            "List the calendars available on the device (id, name, colour, whether read-only). Use a calendar id when searching or creating events.",
            AiSchema.ToElement(AiSchema.Object(new JsonObject())))
    { }

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var cals = await this.Store.GetAll(cancellationToken).ConfigureAwait(false);
        var results = new JsonArray();
        foreach (var c in cals)
            results.Add((JsonNode)CalendarJson.Calendar(c));
        return new JsonObject { ["count"] = results.Count, ["calendars"] = results };
    }
}

/// <summary>Searches events in a date window, optionally filtered by calendar and free-text query.</summary>
sealed class SearchEventsFunction : CalendarAIFunctionBase
{
    const int DefaultLimit = 50;

    public SearchEventsFunction(ICalendarStore store)
        : base(store, "search_events",
            "Search calendar events within a date range. Optionally filter by calendarId and a free-text query matched against title, location, and description. Dates are ISO-8601 (e.g. 2026-07-24 or 2026-07-24T09:00:00). Returns matching events (id, title, start, end). Use the id with get_event/update_event/delete_event.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["query"] = AiSchema.String("Text to match against title, location, or description. Leave empty to return all events in the range."),
                ["calendarId"] = AiSchema.String("Restrict to a single calendar id (from list_calendars)."),
                ["start"] = AiSchema.String("Window start (ISO-8601). Defaults to one month ago."),
                ["end"] = AiSchema.String("Window end (ISO-8601). Defaults to three months ahead."),
                ["limit"] = AiSchema.Integer($"Maximum number of events to return (default {DefaultLimit}).")
            }));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var query = GetString(arguments, "query")?.Trim();
        var calendarId = GetString(arguments, "calendarId");
        var start = GetDate(arguments, "start");
        var end = GetDate(arguments, "end");
        var limit = GetInt(arguments, "limit") ?? DefaultLimit;
        if (limit < 1)
            limit = DefaultLimit;

        var events = await this.Store.GetEvents(calendarId, start, end, cancellationToken).ConfigureAwait(false);

        IEnumerable<CalendarEvent> matches = events;
        if (!string.IsNullOrWhiteSpace(query))
            matches = events.Where(e => Matches(e, query));

        var results = new JsonArray();
        foreach (var e in matches.OrderBy(e => e.Start).Take(limit))
            results.Add((JsonNode)CalendarJson.EventSummary(e));

        return new JsonObject { ["count"] = results.Count, ["events"] = results };
    }

    static bool Matches(CalendarEvent e, string query)
    {
        bool Has(string? s) => s != null && s.Contains(query, StringComparison.OrdinalIgnoreCase);
        return Has(e.Title) || Has(e.Location) || Has(e.Description);
    }
}

/// <summary>Reads a single event by its platform identifier, including full detail.</summary>
sealed class GetEventFunction : CalendarAIFunctionBase
{
    public GetEventFunction(ICalendarStore store)
        : base(store, "get_event",
            "Read a single calendar event by its platform id (as returned by search_events), including description, attendees, reminders, and recurrence.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["eventId"] = AiSchema.String("The platform event id.")
            },
            "eventId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "eventId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'eventId' is required." };

        var e = await this.Store.GetEvent(id, cancellationToken).ConfigureAwait(false);
        if (e is null)
            return new JsonObject { ["error"] = $"No event found with id '{id}'." };

        return CalendarJson.EventDetail(e);
    }
}
