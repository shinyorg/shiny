# Shiny.Notifications API Reference

## Installation

```xml
<PackageReference Include="Shiny.Notifications" />
```

**Supported Platforms:** iOS, Mac Catalyst, Android

**Dependencies:** Shiny.Core, Shiny.Locations, Shiny.Support.Repositories

## Namespace

```csharp
using Shiny.Notifications;
```

Registration extension methods are in the `Shiny` namespace.

---

## Registration

### ServiceCollectionExtensions (namespace: Shiny)

```csharp
// All platforms
public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null);
public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services) where TDelegate : INotificationDelegate;

// iOS / Mac Catalyst only
public static IServiceCollection AddNotifications(this IServiceCollection services, Type? delegateType = null, IosConfiguration? configuration = null);
public static IServiceCollection AddNotifications<TDelegate>(this IServiceCollection services, IosConfiguration? configuration = null) where TDelegate : INotificationDelegate;
```

---

## Core Interfaces

### INotificationManager

The primary interface for managing local notifications. Injected via DI.

```csharp
public interface INotificationManager
{
    // Channel management
    void AddChannel(Channel channel);
    void RemoveChannel(string channelId);
    void ClearChannels();
    Channel? GetChannel(string channelId);
    IList<Channel> GetChannels();

    // Permissions
    Task<AccessState> RequestAccess(AccessRequestFlags flags = AccessRequestFlags.Notification);

    // Notification CRUD
    Task<Notification>? GetNotification(int notificationId);
    Task Cancel(int id);
    Task Cancel(CancelScope cancelScope = CancelScope.All);
    Task<IList<Notification>> GetPendingNotifications();
    Task Send(Notification notification);
}
```

### INotificationDelegate

Implement this interface to handle when a user taps or interacts with a notification.

```csharp
public interface INotificationDelegate
{
    Task OnEntry(NotificationResponse response);
}
```

### IChannelManager

Lower-level channel management interface (registered internally, also available via DI).

```csharp
public interface IChannelManager
{
    void Add(Channel channel);
    void Remove(string channelId);
    void Clear();
    Channel? Get(string channelId);
    IList<Channel> GetAll();
}
```

### ICanManageBadge

Extended interface for platforms that support badge management. Check with `is` cast on `INotificationManager`.

```csharp
public interface ICanManageBadge : INotificationManager
{
    Task SetBadge(int value);
    Task<int> GetBadge();
}
```

---

## Models

### Notification

```csharp
public class Notification : IRepositoryEntity
{
    public int Id { get; set; }                                    // Auto-assigned if not set
    public string? Title { get; set; }
    public string? Message { get; set; }                           // Required
    public string? Channel { get; set; }                           // Channel identifier
    public IDictionary<string, string> Payload { get; set; }       // Custom key-value data
    public int? BadgeCount { get; set; }                           // App icon badge (0 to clear)
    public string? Thread { get; set; }                            // iOS: Thread, Android: Group

    // Triggers (only one allowed per notification)
    public DateTimeOffset? ScheduleDate { get; set; }              // One-time scheduled
    public GeofenceTrigger? Geofence { get; set; }                 // Location-triggered
    public IntervalTrigger? RepeatInterval { get; set; }           // Repeating
}
```

### Channel

```csharp
public class Channel : IRepositoryEntity
{
    public static Channel Default { get; }                         // Identifier="Notifications", Importance=Low

    public string Identifier { get; set; }                         // Required
    public string? Description { get; set; }
    public List<ChannelAction> Actions { get; set; }
    public ChannelImportance Importance { get; set; }              // Default: Normal
    public ChannelSound Sound { get; set; }                        // Default: Default
    public string? CustomSoundPath { get; set; }                   // Required when Sound=Custom

    public static Channel Create(string id, params ChannelAction[] actions);
}
```

### ChannelAction

```csharp
public class ChannelAction
{
    public string Identifier { get; set; }                         // Required
    public string Title { get; set; }                              // Required
    public ChannelActionType ActionType { get; set; }              // Default: None

    public static ChannelAction Create(string id, ChannelActionType actionType = ChannelActionType.None);
    public static ChannelAction Create(string id, string title, ChannelActionType actionType = ChannelActionType.None);
}
```

### GeofenceTrigger

```csharp
public class GeofenceTrigger
{
    public bool Repeat { get; set; }
    public Position? Center { get; set; }                          // Required (from Shiny.Locations)
    public Distance? Radius { get; set; }                          // Required (from Shiny.Locations)
}
```

### IntervalTrigger

```csharp
public class IntervalTrigger
{
    public DayOfWeek? DayOfWeek { get; set; }                      // Optional: restrict to a specific day
    public TimeSpan? TimeOfDay { get; set; }                       // Daily/weekly recurrence (max 24h)
    public TimeSpan? Interval { get; set; }                        // Raw interval recurrence

    // Set either TimeOfDay OR Interval, not both
    public DateTime CalculateNextAlarm();
}
```

### NotificationResponse

```csharp
public record NotificationResponse(
    Notification Notification,                                     // The original notification
    string? ActionIdentifier,                                      // Which action was tapped
    string? Text                                                   // Text reply (if TextReply action)
);
```

### IosConfiguration (iOS / Mac Catalyst only)

```csharp
public record IosConfiguration(
    UNAuthorizationOptions UNAuthorizationOptions =
        UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
    UNNotificationPresentationOptions PresentationOptions =
        UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Badge | UNNotificationPresentationOptions.Sound
);
```

---

## Enums

### AccessRequestFlags

```csharp
[Flags]
public enum AccessRequestFlags
{
    Notification = 0,
    LocationAware = 1,
    TimeSensitivity = 2,
    All = Notification | LocationAware | TimeSensitivity
}
```

### CancelScope

```csharp
public enum CancelScope
{
    DisplayedOnly,      // Only notifications currently shown on screen
    Pending,            // Only scheduled/triggered notifications not yet fired
    All                 // Everything (default)
}
```

### ChannelImportance

```csharp
public enum ChannelImportance
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}
```

### ChannelSound

```csharp
public enum ChannelSound
{
    None,
    Default,
    High,
    Custom              // Requires CustomSoundPath on Channel
}
```

### ChannelActionType

```csharp
public enum ChannelActionType
{
    TextReply,          // Shows text input on the notification
    Destructive,        // Displayed with destructive styling
    OpenApp,            // Opens the app when tapped
    None                // Default -- no special behavior
}
```

---

## Platform-Specific Types

### Android

#### AndroidNotification : Notification

```csharp
public class AndroidNotification : Notification
{
    public bool AutoCancel { get; set; }                           // Default: true
    public bool OnGoing { get; set; }                              // Persistent notification
    public string? Ticket { get; set; }
    public string? Category { get; set; }
    public string? SmallIconResourceName { get; set; }             // Android drawable resource name
    public string? LargeIconResourceName { get; set; }
    public string? ColorResourceName { get; set; }
    public bool UseBigTextStyle { get; set; }
    public Type? LaunchActivityType { get; set; }
    public ActivityFlags LaunchActivityFlags { get; set; }         // Default: NewTask | ClearTask
    public string? Ticker { get; set; }
}
```

#### AndroidChannel : Channel

```csharp
public class AndroidChannel : Channel
{
    public bool? Blockable { get; set; }
    public bool? AllowBubbles { get; set; }
    public bool? ShowBadge { get; set; }
    public bool? EnableLights { get; set; }
    public bool? EnableVibration { get; set; }
    public bool? BypassDnd { get; set; }
    public NotificationVisibility? LockscreenVisibility { get; set; }
}
```

### iOS / Mac Catalyst

#### AppleNotification : Notification

```csharp
public class AppleNotification : Notification
{
    public string? FilterCriteria { get; set; }
    public double RelevanceScore { get; set; }
    public string? TargetContentIdentifier { get; set; }
    public string? Subtitle { get; set; }
    public UNNotificationAttachment[] Attachments { get; set; }
}
```

#### AppleChannel : Channel

```csharp
public class AppleChannel : Channel
{
    public string[]? IntentIdentifiers { get; set; }
    public UNNotificationCategoryOptions CategoryOptions { get; set; }  // Default: None
}
```

---

## Extension Methods

### NotificationExtensions (namespace: Shiny.Notifications)

```csharp
public static class NotificationExtensions
{
    // Convenience send with simple parameters
    public static Task Send(
        this INotificationManager notifications,
        string title,
        string message,
        string? channel = null,
        DateTime? scheduleDate = null
    );

    // Automatically determines required AccessRequestFlags from notification properties
    public static Task<AccessState> RequestRequiredAccess(
        this INotificationManager notificationManager,
        Notification notification
    );

    // Validates a notification (called internally before send)
    public static void AssertValid(this Notification notification);
}
```

### FeatureBadges (namespace: Shiny.Notifications)

```csharp
public static class FeatureBadges
{
    // Tries to set badge if platform supports it; returns true on success
    public static Task<bool> TrySetBadge(this INotificationManager manager, int value);

    // Tries to get badge if platform supports it; returns (true, value) or (false, null)
    public static Task<(bool Success, int? Value)> TryGetBadge(this INotificationManager manager);
}
```

### ChannelExtensions (namespace: Shiny.Notifications)

```csharp
public static class ChannelExtensions
{
    // Validates channel configuration (called internally)
    public static void AssertValid(this Channel channel);
    public static void AssertValid(this ChannelAction action);
}
```

---

## Usage Examples

### 1. Basic Immediate Notification

```csharp
public class MyViewModel
{
    readonly INotificationManager notificationManager;

    public MyViewModel(INotificationManager notificationManager)
    {
        this.notificationManager = notificationManager;
    }

    public async Task SendNotification()
    {
        var access = await this.notificationManager.RequestAccess();
        if (access != AccessState.Available)
            return;

        await this.notificationManager.Send("Hello", "This is a test notification");
    }
}
```

### 2. Scheduled Notification

```csharp
var access = await notificationManager.RequestAccess(AccessRequestFlags.TimeSensitivity);
if (access != AccessState.Available)
    return;

await notificationManager.Send(new Notification
{
    Title = "Reminder",
    Message = "Don't forget your appointment!",
    ScheduleDate = DateTimeOffset.Now.AddHours(1)
});
```

### 3. Repeating Notification (Daily at 9 AM)

```csharp
await notificationManager.Send(new Notification
{
    Title = "Daily Reminder",
    Message = "Time to check in!",
    RepeatInterval = new IntervalTrigger
    {
        TimeOfDay = new TimeSpan(9, 0, 0)
    }
});
```

### 4. Repeating Notification (Every Monday at 8 AM)

```csharp
await notificationManager.Send(new Notification
{
    Title = "Weekly Standup",
    Message = "Join the team meeting",
    RepeatInterval = new IntervalTrigger
    {
        DayOfWeek = DayOfWeek.Monday,
        TimeOfDay = new TimeSpan(8, 0, 0)
    }
});
```

### 5. Geofence-Triggered Notification

```csharp
var access = await notificationManager.RequestAccess(AccessRequestFlags.LocationAware);
if (access != AccessState.Available)
    return;

await notificationManager.Send(new Notification
{
    Title = "Welcome!",
    Message = "You have arrived at the office",
    Geofence = new GeofenceTrigger
    {
        Center = new Position(43.6532, -79.3832),
        Radius = Distance.FromMeters(200),
        Repeat = true
    }
});
```

### 6. Notification with Channel and Actions

```csharp
// Create channel with interactive actions
notificationManager.AddChannel(new Channel
{
    Identifier = "messages",
    Description = "Message notifications",
    Importance = ChannelImportance.High,
    Sound = ChannelSound.High,
    Actions = new List<ChannelAction>
    {
        ChannelAction.Create("reply", "Reply", ChannelActionType.TextReply),
        ChannelAction.Create("dismiss", "Dismiss", ChannelActionType.Destructive)
    }
});

// Send notification on that channel
await notificationManager.Send(new Notification
{
    Title = "New Message",
    Message = "You have a new message from John",
    Channel = "messages",
    Payload = new Dictionary<string, string>
    {
        { "senderId", "123" },
        { "conversationId", "456" }
    }
});
```

### 7. Handling Notification Responses

```csharp
public class MyNotificationDelegate : INotificationDelegate
{
    readonly INavigationService nav;

    public MyNotificationDelegate(INavigationService nav)
    {
        this.nav = nav;
    }

    public async Task OnEntry(NotificationResponse response)
    {
        var notification = response.Notification;

        if (response.ActionIdentifier == "reply" && response.Text != null)
        {
            // Handle text reply action
            await SendReply(response.Text, notification.Payload["conversationId"]);
        }
        else if (notification.Payload.TryGetValue("conversationId", out var convoId))
        {
            // Navigate to conversation
            await this.nav.GoTo($"conversations/{convoId}");
        }
    }
}
```

### 8. Badge Management

```csharp
// Try to set badge (returns false if platform doesn't support it)
var success = await notificationManager.TrySetBadge(5);

// Try to get badge
var (hasValue, badge) = await notificationManager.TryGetBadge();
if (hasValue)
{
    Console.WriteLine($"Badge count: {badge}");
}

// Clear badge
await notificationManager.TrySetBadge(0);
```

### 9. Android-Specific Notification

```csharp
#if ANDROID
await notificationManager.Send(new AndroidNotification
{
    Title = "Download Complete",
    Message = "Your file has been downloaded",
    SmallIconResourceName = "ic_download",
    LargeIconResourceName = "ic_download_large",
    ColorResourceName = "notification_color",
    UseBigTextStyle = true,
    AutoCancel = true,
    Category = "download"
});
#endif
```

### 10. iOS-Specific Notification

```csharp
#if IOS || MACCATALYST
await notificationManager.Send(new AppleNotification
{
    Title = "Photo Shared",
    Subtitle = "From Jane",
    Message = "Jane shared a new photo with you",
    RelevanceScore = 0.8,
    TargetContentIdentifier = "photo-123"
});
#endif
```

### 11. Managing Pending Notifications

```csharp
// Get all pending notifications
var pending = await notificationManager.GetPendingNotifications();

// Cancel a specific notification
await notificationManager.Cancel(notificationId);

// Cancel only displayed notifications
await notificationManager.Cancel(CancelScope.DisplayedOnly);

// Cancel only pending/scheduled notifications
await notificationManager.Cancel(CancelScope.Pending);

// Cancel all notifications
await notificationManager.Cancel(CancelScope.All);
```

### 12. Custom Sound Channel

```csharp
notificationManager.AddChannel(new Channel
{
    Identifier = "custom-alerts",
    Importance = ChannelImportance.High,
    Sound = ChannelSound.Custom,
    CustomSoundPath = "alarm_sound"       // platform-specific resource path
});
```

---

## Troubleshooting

### Notification not appearing

1. Ensure `RequestAccess()` returns `AccessState.Available` before sending.
2. On Android 13+, the POST_NOTIFICATIONS permission must be granted at runtime -- `RequestAccess()` handles this.
3. On iOS, check that the user has not disabled notifications in system settings.
4. Ensure the `Notification.Message` property is set (it is required).

### Scheduled notification not firing

1. Ensure `RequestAccess(AccessRequestFlags.TimeSensitivity)` is called and returns `AccessState.Available`.
2. Verify `ScheduleDate` is set in the future -- past dates will throw during validation.
3. Do not mix `ScheduleDate` with `RepeatInterval` or `Geofence` on the same notification.

### Geofence notification not firing

1. Ensure `RequestAccess(AccessRequestFlags.LocationAware)` returns `AccessState.Available`.
2. Verify both `Center` and `Radius` are set on the `GeofenceTrigger`.
3. On iOS, location "Always" permission is required for geofence notifications.

### Channel validation errors

1. `Channel.Identifier` is required and must not be empty.
2. If `Sound` is set to `ChannelSound.Custom`, you must also set `CustomSoundPath`.
3. If `Sound` is NOT `ChannelSound.Custom`, `CustomSoundPath` must be null/empty.
4. `ChannelAction.Identifier` and `ChannelAction.Title` are both required.

### Badge not working

1. Use `TrySetBadge` / `TryGetBadge` extension methods -- they return false if the platform does not support badges.
2. `BadgeCount` on a notification is only valid for immediate (non-triggered) notifications.

### Default channel cannot be removed

The default channel (`Identifier = "Notifications"`) cannot be removed. Attempting to call `RemoveChannel("Notifications")` will throw an `InvalidOperationException`.

### IntervalTrigger validation errors

1. You must set either `TimeOfDay` or `Interval`, but not both.
2. `TimeOfDay` must be within 24 hours (total minutes less than 1440).

### Android small icon not showing

1. Create an Android drawable resource named `notification`, or
2. Set `SmallIconResourceName` on `AndroidNotification` to a valid drawable resource name.
3. If neither is found, an `InvalidOperationException` is thrown.
