using Shiny.Push;

namespace Sample.tvOS.Services;


/// <summary>
/// On tvOS only <see cref="OnReceived"/> and the token callbacks ever fire. There is no
/// UNNotificationResponse on tvOS - a notification there can only change the app icon badge -
/// so <see cref="OnEntry"/> is dead code on this platform and is left here purely to show that.
/// </summary>
public class SamplePushDelegate(AppLog log) : PushDelegate
{
    public override Task OnNewToken(string token)
    {
        log.Write($"push token: {token}");
        return Task.CompletedTask;
    }

    public override Task OnReceived(PushNotification notification)
    {
        log.Write($"silent push received with {notification.Data.Count} data field(s)");
        return Task.CompletedTask;
    }

    public override Task OnUnRegistered(string token)
    {
        log.Write("push unregistered");
        return Task.CompletedTask;
    }

    // never called on tvOS - nothing there can be tapped
    public override Task OnEntry(PushNotification notification)
    {
        log.Write("OnEntry - which tvOS never raises");
        return Task.CompletedTask;
    }
}
