using System.Collections.Generic;
using Foundation;

namespace Shiny.Push;

public record ApplePushNotification(
    IDictionary<string, string> Data, 
    NSDictionary? RawPayload,
    Notification? Notification
) 
: PushNotification(
    Data,
    Notification
);