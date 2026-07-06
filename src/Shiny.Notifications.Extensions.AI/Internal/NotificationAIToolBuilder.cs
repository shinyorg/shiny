namespace Shiny.Notifications.Extensions.AI.Internal;

sealed class NotificationAIToolBuilder : INotificationAIToolBuilder
{
    public ReminderAICapabilities Capabilities { get; private set; }
    public string? Channel { get; private set; }

    public INotificationAIToolBuilder AddReminders(
        ReminderAICapabilities capabilities = ReminderAICapabilities.Read,
        string? channel = null)
    {
        this.Capabilities |= capabilities;
        if (channel != null)
            this.Channel = channel;
        return this;
    }
}
