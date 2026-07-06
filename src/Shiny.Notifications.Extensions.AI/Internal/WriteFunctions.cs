using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Notifications.Extensions.AI.Internal;

/// <summary>Creates a reminder: sends a local notification now, at a scheduled time, or daily.</summary>
sealed class CreateReminderFunction : NotificationAIFunctionBase
{
    readonly string? channel;

    public CreateReminderFunction(INotificationManager manager, string? channel)
        : base(manager, "create_reminder",
            "Create a reminder (local notification). Omit both scheduleFor and repeatDailyAt to notify immediately; set scheduleFor for a one-time reminder at a specific date/time; set repeatDailyAt (e.g. \"08:30\") for a daily reminder. Returns the reminder id (use it with cancel_reminder).",
            BuildSchema())
        => this.channel = channel;

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["title"] = AiSchema.String("The reminder title."),
                ["message"] = AiSchema.String("The reminder body text."),
                ["scheduleFor"] = AiSchema.Date("When the one-time reminder should fire"),
                ["repeatDailyAt"] = AiSchema.String("Local time-of-day for a daily repeating reminder, as \"HH:mm\" (24-hour, e.g. \"08:30\"). Mutually exclusive with scheduleFor.")
            },
            "message"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var message = GetString(arguments, "message");
        if (string.IsNullOrWhiteSpace(message))
            return new JsonObject { ["error"] = "'message' is required." };

        var scheduleFor = GetDate(arguments, "scheduleFor");
        var repeatDailyAt = GetTimeOfDay(arguments, "repeatDailyAt");

        if (scheduleFor != null && repeatDailyAt != null)
            return new JsonObject { ["error"] = "Provide either scheduleFor or repeatDailyAt, not both." };

        if (scheduleFor != null && scheduleFor.Value <= DateTimeOffset.Now)
            return new JsonObject { ["error"] = "scheduleFor must be in the future." };

        var notification = new Notification
        {
            Title = GetString(arguments, "title"),
            Message = message,
            Channel = this.channel
        };

        if (scheduleFor != null)
            notification.ScheduleDate = scheduleFor;
        else if (repeatDailyAt != null)
            notification.RepeatInterval = new IntervalTrigger { TimeOfDay = repeatDailyAt };

        await this.Manager.Send(notification).ConfigureAwait(false);

        var result = new JsonObject { ["success"] = true, ["id"] = notification.Id };
        if (scheduleFor != null)
            result["scheduledFor"] = Iso(scheduleFor.Value);
        else if (repeatDailyAt != null)
            result["repeatsDailyAt"] = repeatDailyAt.Value.ToString(@"hh\:mm");
        else
            result["sent"] = true;

        return result;
    }
}

/// <summary>Cancels a reminder (displayed or pending) by its id.</summary>
sealed class CancelReminderFunction : NotificationAIFunctionBase
{
    public CancelReminderFunction(INotificationManager manager)
        : base(manager, "cancel_reminder",
            "Cancel a reminder by its id (as returned by list_reminders or create_reminder).",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["id"] = AiSchema.Integer("The reminder id to cancel.")
            },
            "id"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetInt(arguments, "id");
        if (id is null)
            return new JsonObject { ["error"] = "'id' is required." };

        await this.Manager.Cancel(id.Value).ConfigureAwait(false);
        return new JsonObject { ["success"] = true, ["id"] = id.Value };
    }
}
