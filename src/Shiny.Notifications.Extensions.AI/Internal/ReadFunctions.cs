using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Notifications.Extensions.AI.Internal;

/// <summary>Lists pending (scheduled, repeating, or geofence-triggered) reminders.</summary>
sealed class ListRemindersFunction : NotificationAIFunctionBase
{
    public ListRemindersFunction(INotificationManager manager)
        : base(manager, "list_reminders",
            "List the reminders (local notifications) that are currently scheduled, repeating, or awaiting a trigger. Returns id, title, message, and when each fires.",
            AiSchema.ToElement(AiSchema.Object(new JsonObject())))
    { }

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var pending = await this.Manager.GetPendingNotifications().ConfigureAwait(false);

        var list = new JsonArray();
        foreach (var n in pending)
        {
            var o = new JsonObject
            {
                ["id"] = n.Id,
                ["title"] = n.Title,
                ["message"] = n.Message
            };
            if (n.ScheduleDate != null)
                o["scheduledFor"] = Iso(n.ScheduleDate.Value);
            if (n.RepeatInterval?.TimeOfDay != null)
                o["repeatsDailyAt"] = n.RepeatInterval.TimeOfDay.Value.ToString(@"hh\:mm");
            if (n.RepeatInterval?.Interval != null)
                o["repeatsEveryMinutes"] = (int)n.RepeatInterval.Interval.Value.TotalMinutes;

            list.Add((JsonNode)o);
        }

        return new JsonObject { ["count"] = list.Count, ["reminders"] = list };
    }
}
