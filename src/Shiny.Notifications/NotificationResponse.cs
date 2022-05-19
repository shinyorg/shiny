namespace Shiny.Notifications;

public record NotificationResponse(
    Notification Notification,
    string? ActionIdentifier,
    string? Text
);