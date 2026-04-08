using Microsoft.Extensions.Logging;
using Shiny.Notifications;

namespace Sample.Shared.Maui.Delegates;

public class SampleNotificationDelegate(ILogger<SampleNotificationDelegate> logger) : INotificationDelegate
{
    public Task OnEntry(NotificationResponse response)
    {
        logger.LogInformation("Notification Entry: {Id}", response.Notification.Id);
        return Task.CompletedTask;
    }
}
