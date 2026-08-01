using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Calendar.Extensions.AI.Internal;

/// <summary>Creates a new calendar event.</summary>
sealed class CreateEventFunction : CalendarAIFunctionBase
{
    public CreateEventFunction(ICalendarStore store, CalendarAccessPolicy policy)
        : base(store, policy, "create_event",
            "Create a new calendar event. Requires a title, start, and end (ISO-8601). If calendarId is omitted the device default calendar is used - when you are limited to specific calendars you must pass a calendarId whose allowedOperations include create. Returns the new event's id.",
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
        var calendarId = GetString(arguments, "calendarId");

        if (string.IsNullOrWhiteSpace(title))
            return new JsonObject { ["error"] = "'title' is required." };
        if (start is null || end is null)
            return new JsonObject { ["error"] = "'start' and 'end' are required and must be ISO-8601 date-times." };
        if (end < start)
            return new JsonObject { ["error"] = "'end' must be on or after 'start'." };

        if (calendarId is null)
        {
            // Without an id the platform writes to the default calendar, which we cannot vet ahead of
            // time - only allow that when creating is permitted everywhere.
            if (!this.Policy.Global.HasFlag(CalendarAICapabilities.Create))
                return new JsonObject
                {
                    ["error"] = "'calendarId' is required - you may only create events in specific calendars. Call list_calendars and pick one whose allowedOperations include 'create'."
                };
        }
        else if (!this.Policy.Allows(calendarId, CalendarAICapabilities.Create))
        {
            return Denied(calendarId);
        }

        var evt = new CalendarEvent
        {
            CalendarId = calendarId,
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
    public UpdateEventFunction(ICalendarStore store, CalendarAccessPolicy policy)
        : base(store, policy, "update_event",
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

        if (!this.Policy.Allows(evt.CalendarId, CalendarAICapabilities.Update))
            return Denied(evt.CalendarId);

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
    public DeleteEventFunction(ICalendarStore store, CalendarAccessPolicy policy)
        : base(store, policy, "delete_event",
            "Delete a calendar event by its id. This is irreversible - confirm with the user before calling.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["eventId"] = AiSchema.String("The id of the event to delete."),
                ["deleteSeries"] = AiSchema.Boolean(
                    "For a recurring event, true deletes this occurrence and all future ones in the series, "
                    + "false deletes only this occurrence. Defaults to false. Ask the user which they meant "
                    + "before calling on a recurring event.")
            },
            "eventId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "eventId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'eventId' is required." };

        var deleteSeries = GetBool(arguments, "deleteSeries") ?? false;

        // Only pay for the lookup when the delete rules differ between calendars.
        if (!this.Policy.AppliesToAll(CalendarAICapabilities.Delete))
        {
            var evt = await this.Store.GetEvent(id, cancellationToken).ConfigureAwait(false);
            if (evt is null)
                return new JsonObject { ["error"] = $"No event found with id '{id}'." };

            if (!this.Policy.Allows(evt.CalendarId, CalendarAICapabilities.Delete))
                return Denied(evt.CalendarId);
        }

        await this.Store.DeleteEvent(id, deleteSeries, cancellationToken).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id, ["deletedSeries"] = deleteSeries };
    }
}
