namespace Shiny.Push;

/// <summary>
/// Represents the user-facing notification portion of a push payload (alert/title and body text).
/// </summary>
/// <param name="Title">The notification title, if provided by the sender.</param>
/// <param name="Message">The notification body text, if provided by the sender.</param>
public record Notification(
    string? Title,
    string? Message
);
