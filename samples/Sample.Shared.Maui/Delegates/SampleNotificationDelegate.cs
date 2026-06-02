using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Notifications;

namespace Sample.Shared.Maui.Delegates;

public class SampleNotificationDelegate(
    ILogger<SampleNotificationDelegate> logger,
    IEventStore events
) : INotificationDelegate
{
    public Task OnEntry(NotificationResponse response)
    {
        logger.LogInformation("Notification Entry: {Id}", response.Notification.Id);

        return events.Add(
            "Notification",
            $"Entry: {response.Notification.Title ?? "(no title)"}",
            new Dictionary<string, string?>
            {
                ["Id"] = response.Notification.Id.ToString(),
                ["Title"] = response.Notification.Title,
                ["Message"] = response.Notification.Message,
                ["ActionId"] = response.ActionIdentifier,
                ["Text"] = response.Text
            }
        );
    }
}
