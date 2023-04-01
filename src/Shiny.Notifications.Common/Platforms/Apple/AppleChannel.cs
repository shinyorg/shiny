using UserNotifications;

namespace Shiny.Notifications;


public class AppleChannel : Channel
{
    public string[]? IntentIdentifiers { get; set; }
    public UNNotificationCategoryOptions CategoryOptions { get; set; } = UNNotificationCategoryOptions.None;
}