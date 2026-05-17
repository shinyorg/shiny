using System.Collections.Generic;

namespace Shiny.Push;


/// <summary>
/// A received push notification, containing the raw data payload and the optional user-facing notification portion.
/// </summary>
/// <param name="Data">The custom data key/value pairs included in the push payload.</param>
/// <param name="Notification">The user-facing notification (title/body) if the push included one; otherwise null for silent/data-only pushes.</param>
public record PushNotification(
    IDictionary<string, string> Data,
    Notification? Notification
);
