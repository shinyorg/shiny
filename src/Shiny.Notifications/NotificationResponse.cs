namespace Shiny.Notifications;

/// <summary>
/// Represents a user interaction with a notification, delivered to <see cref="INotificationDelegate.OnEntry"/>.
/// </summary>
/// <param name="Notification">The originating notification the user interacted with.</param>
/// <param name="ActionIdentifier">The identifier of the channel action the user selected, or null if they simply tapped the notification.</param>
/// <param name="Text">The user's typed reply when the action is a text reply input; otherwise null.</param>
public record NotificationResponse(
    Notification Notification,
    string? ActionIdentifier,
    string? Text
);
