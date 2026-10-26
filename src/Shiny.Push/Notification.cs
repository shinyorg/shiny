namespace Shiny.Push;

/// <summary>
/// Represents the user-facing notification portion of a push payload (alert/title and body text).
/// </summary>
/// <param name="Title">The notification title, if provided by the sender.</param>
/// <param name="Message">The notification body text, if provided by the sender.</param>
/// <param name="ImageUri">
/// The image URL sent with the push, if any - FCM's notification image on Android, otherwise the first of the
/// <see cref="PushImages.DefaultKeys"/> found in the payload. iOS only shows it when the app builds the notification
/// service extension (ShinyPushServiceExtension); Android shows it for background notification messages, and in the
/// foreground through AndroidPushNotification.SendDefaultAsync.
/// </param>
public record Notification(
    string? Title,
    string? Message,
    string? ImageUri = null
);
