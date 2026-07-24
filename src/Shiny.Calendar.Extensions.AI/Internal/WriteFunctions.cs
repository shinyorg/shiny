using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Calendar.Extensions.AI.Internal;

/// <summary>Creates a new calendar event.</summary>
sealed class CreateEventFunction : CalendarAIFunctionBase
{
    public CreateEventFunction(ICalendarStore store)
        : base(store, "create_event",
            "Create a new calendar event. Requires a title, start, and end (ISO-8601). If calendarId is omitted the device default calendar is used. Returns the new event's id.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["title"] = AiSchema.String("Event title / subject."),
                ["start"] = AiSchema.String("Start date-time (ISO-8601, e.g. 2026-07-24T09:00:00)."),
                ["end"] = AiSchema.String("End date-time (ISO-8601)."),
                ["calendarId"] = AiSchema.String("Target calendar id (from list_calendars). Optional."),
                ["description"] = AiSchema.String("Event description / notes."),
                ["location"] = AiSchema.String("Event location."),
                ["isAllDay"] = AiSchema.Boolean("True for an all-day event."),
                ["reminderMinutesBefore"] = AiSchema.Integer("If set, adds a reminder this many minutes before the start.")
            },
            "title", "start", "end"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var title = GetString(arguments, "title");
        var start = GetDate(arguments, "start");
        var end = GetDate(arguments, "end");

        if (string.IsNullOrWhiteSpace(title))
            return new JsonObject { ["error"] = "'title' is required." };
        if (start is null || end is null)
            return new JsonObject { ["error"] = "'start' and 'end' are required and must be ISO-8601 date-times." };
        if (end < start)
            return new JsonObject { ["error"] = "'end' must be on or after 'start'." };

        var evt = new CalendarEvent
        {
            CalendarId = GetString(arguments, "calendarId"),
            Title = title,
            Description = GetString(arguments, "description"),
            Location = GetString(arguments, "location"),
            Start = start.Value,
            End = end.Value,
            IsAllDay = GetBool(arguments, "isAllDay") ?? false
        };

        var minutes = GetInt(arguments, "reminderMinutesBefore");
        if (minutes is > 0)
            evt.Reminders.Add(new EventReminder(TimeSpan.FromMinutes(minutes.Value)));

        var id = await this.Store.CreateEvent(evt, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id, ["title"] = evt.Title };
    }
}

/// <summary>Updates fields on an existing calendar event.</summary>
sealed class UpdateEventFunction : CalendarAIFunctionBase
{
    public UpdateEventFunction(ICalendarStore store)
        : base(store, "update_event",
            "Update an existing event (found by id). Only the fields you supply are changed.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["eventId"] = AiSchema.String("The id of the event to update."),
                ["title"] = AiSchema.String("New title."),
                ["start"] = AiSchema.String("New start date-time (ISO-8601)."),
                ["end"] = AiSchema.String("New end date-time (ISO-8601)."),
                ["description"] = AiSchema.String("New description / notes."),
                ["location"] = AiSchema.String("New location."),
                ["isAllDay"] = AiSchema.Boolean("Whether the event is all-day.")
            },
            "eventId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "eventId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'eventId' is required." };

        var evt = await this.Store.GetEvent(id, cancellationToken).ConfigureAwait(false);
        if (evt is null)
            return new JsonObject { ["error"] = $"No event found with id '{id}'." };

        var title = GetString(arguments, "title");
        if (title != null)
            evt.Title = title;

        var description = GetString(arguments, "description");
        if (description != null)
            evt.Description = description;

        var location = GetString(arguments, "location");
        if (location != null)
            evt.Location = location;

        var start = GetDate(arguments, "start");
        if (start.HasValue)
            evt.Start = start.Value;

        var end = GetDate(arguments, "end");
        if (end.HasValue)
            evt.End = end.Value;

        var allDay = GetBool(arguments, "isAllDay");
        if (allDay.HasValue)
            evt.IsAllDay = allDay.Value;

        if (evt.End < evt.Start)
            return new JsonObject { ["error"] = "The resulting 'end' is before 'start'." };

        await this.Store.UpdateEvent(evt, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id, ["title"] = evt.Title };
    }
}

/// <summary>Deletes a calendar event by id.</summary>
sealed class DeleteEventFunction : CalendarAIFunctionBase
{
    public DeleteEventFunction(ICalendarStore store)
        : base(store, "delete_event",
            "Delete a calendar event by its id. This is irreversible - confirm with the user before calling.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["eventId"] = AiSchema.String("The id of the event to delete.")
            },
            "eventId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "eventId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'eventId' is required." };

        await this.Store.DeleteEvent(id, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id };
    }
}
