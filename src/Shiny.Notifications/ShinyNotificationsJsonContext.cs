using System.Text.Json.Serialization;

namespace Shiny.Notifications;

#if ANDROID
[JsonSerializable(typeof(AndroidNotification))]
[JsonSerializable(typeof(AndroidChannel))]
#elif IOS || MACCATALYST || MACOS
[JsonSerializable(typeof(AppleNotification))]
[JsonSerializable(typeof(AppleChannel))]
#else
[JsonSerializable(typeof(Notification))]
[JsonSerializable(typeof(Channel))]
#endif
internal partial class ShinyNotificationsJsonContext : JsonSerializerContext;
