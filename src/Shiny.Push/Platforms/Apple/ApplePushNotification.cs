using System.Collections.Generic;

namespace Shiny.Push;

/// <summary>
/// The "native" data for iOS in a strongly typed package
/// </summary>
/// <param name="Data">A dictionary of the custom payload data</param>
/// <param name="Aps">NULL if received in the background</param>
/// <param name="Notification">The generic notification details - NULL if in the background</param>
public record ApplePushNotification(
    IDictionary<string, string> Data, 
    Aps? Aps,
    Notification? Notification
) 
: PushNotification(
    Data,
    Notification
);

public record Aps(IDictionary<string, string> AdditionalPayloadData, int? Badge, ApsAlert? Alert, ApsSound? Sound, bool? ContentAvailable);
public record ApsAlert(string Title, string Body);
public record ApsSound(bool Critical, string Name, double? Volume);