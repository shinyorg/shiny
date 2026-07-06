using Microsoft.Extensions.AI;

namespace Shiny.Notifications.Extensions.AI.Internal;

static class NotificationAIFunctionFactory
{
    public static IReadOnlyList<AITool> Build(INotificationManager manager, ReminderAICapabilities capabilities, string? channel)
    {
        var tools = new List<AITool>();

        if (capabilities.HasFlag(ReminderAICapabilities.Read))
            tools.Add(new ListRemindersFunction(manager));

        if (capabilities.HasFlag(ReminderAICapabilities.Write))
        {
            tools.Add(new CreateReminderFunction(manager, channel));
            tools.Add(new CancelReminderFunction(manager));
        }

        return tools;
    }
}
