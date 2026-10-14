using Shiny.Notifications;

namespace Shiny.DeviceTests.Tests;


// OnEntry only fires when a person taps a notification - the suites don't, but anything that does lands here
public class TestNotificationDelegate(EventHub events) : INotificationDelegate
{
    public const string EntryTopic = "notifications.entry";

    public Task OnEntry(NotificationResponse response)
    {
        events.Publish(EntryTopic, response);
        return Task.CompletedTask;
    }
}
