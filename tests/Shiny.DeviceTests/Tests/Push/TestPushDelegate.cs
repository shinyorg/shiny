using Shiny.Push;

namespace Shiny.DeviceTests.Tests;


public class TestPushDelegate(EventHub events) : PushDelegate
{
    public const string ReceivedTopic = "push.received";
    public const string EntryTopic = "push.entry";
    public const string TokenTopic = "push.token";

    public override Task OnReceived(PushNotification notification)
    {
        events.Publish(ReceivedTopic, notification);
        return Task.CompletedTask;
    }

    public override Task OnEntry(PushNotification notification)
    {
        events.Publish(EntryTopic, notification);
        return Task.CompletedTask;
    }

    public override Task OnNewToken(string token)
    {
        events.Publish(TokenTopic, token);
        return Task.CompletedTask;
    }
}
