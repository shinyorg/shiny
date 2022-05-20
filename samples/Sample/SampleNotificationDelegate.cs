using Shiny.Notifications;

namespace Sample;


public class SampleNotificationDelegate : INotificationDelegate
{
    public Task OnEntry(NotificationResponse response) => Task.CompletedTask;
}
