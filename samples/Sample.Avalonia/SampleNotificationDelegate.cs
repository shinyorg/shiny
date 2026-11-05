#if PLATFORM
using Shiny.Notifications;

namespace Sample.Avalonia;


/// <summary>
/// Reached on Android only because MainActivity derives from ShinyAvaloniaMainActivity (OnCreate / OnNewIntent
/// forwarding); on iOS Shiny's UNUserNotificationCenter delegate delivers it with no app code.
/// </summary>
public class SampleNotificationDelegate(SampleState state) : INotificationDelegate
{
    public Task OnEntry(NotificationResponse response)
    {
        state.LastNotificationTap = $"{DateTime.Now:T} - {response.Notification.Title}";
        return Task.CompletedTask;
    }
}
#endif
