using System.Text.Json.Nodes;

namespace Shiny.Calendar.Extensions.AI.Internal;

/// <summary>Projects calendar models into LLM-friendly JSON.</summary>
static class CalendarJson
{
    public static JsonObject Calendar(Calendar c) => new()
    {
        ["id"] = c.Id,
        ["name"] = c.Name,
        ["color"] = c.Color,
        ["isReadOnly"] = c.IsReadOnly,
        ["account"] = c.Account
    };

    public static JsonObject EventSummary(CalendarEvent e) => new()
    {
        ["id"] = e.Id,
        ["calendarId"] = e.CalendarId,
        ["title"] = e.Title,
        ["start"] = e.Start.ToString("o"),
        ["end"] = e.End.ToString("o"),
        ["isAllDay"] = e.IsAllDay,
        ["location"] = e.Location
    };

    public static JsonObject EventDetail(CalendarEvent e)
    {
        var o = EventSummary(e);
        o["description"] = e.Description;
        o["availability"] = e.Availability.ToString();
        o["url"] = e.Url;
        o["isRecurring"] = e.IsRecurring;
        o["recurrenceRule"] = e.RecurrenceRule;

        var reminders = new JsonArray();
        foreach (var r in e.Reminders)
            reminders.Add((JsonNode)new JsonObject { ["minutesBefore"] = (int)r.Offset.TotalMinutes });
        o["reminders"] = reminders;

        var attendees = new JsonArray();
        foreach (var a in e.Attendees)
            attendees.Add((JsonNode)new JsonObject
            {
                ["name"] = a.Name,
                ["email"] = a.Email,
                ["role"] = a.Role.ToString(),
                ["status"] = a.Status.ToString(),
                ["isOrganizer"] = a.IsOrganizer
            });
        o["attendees"] = attendees;

        if (e.Organizer != null)
            o["organizer"] = new JsonObject { ["name"] = e.Organizer.Name, ["email"] = e.Organizer.Email };

        return o;
    }
}
