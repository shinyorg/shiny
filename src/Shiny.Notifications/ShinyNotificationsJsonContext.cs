using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shiny.Notifications;

[Shiny.ShinyJsonContext]
// Base types are always registered. The Android intent round-trip serializes/deserializes the
// base Notification (see AndroidNotificationManager.PopulateIntent / AndroidNotificationProcessor),
// and Windows persists the base Notification/Channel directly. Registering them on every platform
// keeps the base contract available regardless of the active platform type.
[JsonSerializable(typeof(Notification))]
[JsonSerializable(typeof(List<Notification>))]
[JsonSerializable(typeof(Notification[]))]
[JsonSerializable(typeof(Channel))]
[JsonSerializable(typeof(List<Channel>))]
[JsonSerializable(typeof(Channel[]))]
#if ANDROID
[JsonSerializable(typeof(AndroidNotification))]
[JsonSerializable(typeof(List<AndroidNotification>))]
[JsonSerializable(typeof(AndroidNotification[]))]
[JsonSerializable(typeof(AndroidChannel))]
[JsonSerializable(typeof(List<AndroidChannel>))]
[JsonSerializable(typeof(AndroidChannel[]))]
#elif IOS || MACCATALYST || MACOS
[JsonSerializable(typeof(AppleNotification))]
[JsonSerializable(typeof(List<AppleNotification>))]
[JsonSerializable(typeof(AppleNotification[]))]
[JsonSerializable(typeof(AppleChannel))]
[JsonSerializable(typeof(List<AppleChannel>))]
[JsonSerializable(typeof(AppleChannel[]))]
#endif
internal partial class ShinyNotificationsJsonContext : JsonSerializerContext;
