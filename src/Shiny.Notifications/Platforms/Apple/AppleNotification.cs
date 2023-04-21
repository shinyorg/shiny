using UserNotifications;

namespace Shiny.Notifications;


public class AppleNotification : Notification
{
    public string? FilterCriteria { get; set; }
    public double RelevanceScore { get; set; }
    public string? TargetContentIdentifier { get; set; }
    public string? Subtitle { get; set; }
    
    public UNNotificationAttachment[] Attachments { get; set; }
}
