---
name: shiny-notifications
description: Cross-platform local notification management for .NET MAUI apps using Shiny, supporting scheduled, repeating, and geofence-triggered notifications with channels, badges, and interactive actions.
auto_invoke: true
triggers:
  - local notifications
  - notification manager
  - notification channel
  - notification delegate
  - push notification
  - scheduled notification
  - geofence notification
  - repeating notification
  - badge count
  - notification actions
  - Shiny.Notifications
  - INotificationManager
  - INotificationDelegate
  - notification permission
  - notification sound
  - reminders AI tools
  - Shiny.Notifications.Extensions.AI
  - AddNotificationAITools
  - NotificationAITools
  - ReminderAICapabilities
  - channel importance
---

# Shiny Notifications

## When to Use This Skill

Use this skill when the user needs to:

- Send local notifications (immediate, scheduled, repeating, or geofence-triggered)
- Manage notification channels with importance levels, sounds, and actions
- Handle notification tap responses and interactive action buttons
- Request notification permissions on iOS and Android
- Manage app icon badge counts
- Configure platform-specific notification behavior (Android ongoing, iOS subtitles, etc.)

## Library Overview

| Item | Value |
|---|---|
| **NuGet Package** | `Shiny.Notifications` (iOS, Mac Catalyst, Android, macOS, Windows); `Shiny.Notifications.Linux` (Linux) |
| **Primary Namespace** | `Shiny.Notifications` |
| **Registration Namespace** | `Shiny` (extension methods on `IServiceCollection`) |
| **Platforms** | iOS, Mac Catalyst, Android, macOS, Windows, Linux |
| **Dependencies** | `Shiny.Core`, `Shiny.Locations`, `Shiny.Support.Repositories` |

### Linux

Linux notifications ship in a separate package, `Shiny.Notifications.Linux`. They are delivered via the freedesktop `org.freedesktop.Notifications` D-Bus service (GNOME, KDE, XFCE, etc.) and support the same `INotificationManager` API surface as the other platforms. Scheduled notifications are tracked **in-process only** — there is no OS-level scheduler like BGTaskScheduler or WorkManager, so the host process must be running for a scheduled notification to fire. Channels are exposed but only a subset of freedesktop hints (urgency, category, image) are actually honoured by most daemons. Geofence triggers and time-sensitive flags are not applicable.

Register with `services.AddNotifications<TDelegate>();` from the `Shiny` namespace — the same call site as the other platforms.

## Setup

Register the notification services in your `MauiProgram.cs`:

```csharp
using Shiny;

// Without a delegate (fire-and-forget notifications)
services.AddNotifications();

// With a delegate to handle notification taps
services.AddNotifications<MyNotificationDelegate>();
```

On iOS, you can optionally pass an `IosConfiguration` to control authorization and presentation options:

```csharp
#if IOS || MACCATALYST
services.AddNotifications<MyNotificationDelegate>(new IosConfiguration(
    UNAuthorizationOptions: UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
    PresentationOptions: UNNotificationPresentationOptions.Banner | UNNotificationPresentationOptions.Badge | UNNotificationPresentationOptions.Sound
));
#endif
```

## Code Generation Instructions

When generating code that uses Shiny Notifications, follow these conventions:

1. **Always request access before sending notifications:**
   ```csharp
   var access = await notificationManager.RequestAccess();
   if (access != AccessState.Available)
   {
       // Handle denied permission
       return;
   }
   ```

2. **Use `AccessRequestFlags` when the notification uses triggers:**
   - `AccessRequestFlags.TimeSensitivity` for scheduled or repeating notifications.
   - `AccessRequestFlags.LocationAware` for geofence-triggered notifications.
   - Or use the `RequestRequiredAccess` extension method that infers flags from the notification object.

3. **A `Notification` must have a `Message` set** -- validation will throw otherwise.

4. **Only one trigger type per notification** -- you cannot mix `ScheduleDate`, `RepeatInterval`, and `Geofence` on the same notification.

5. **Implement `INotificationDelegate` for handling user taps:**
   ```csharp
   public class MyNotificationDelegate : INotificationDelegate
   {
       public async Task OnEntry(NotificationResponse response)
       {
           // response.Notification -- the original notification
           // response.ActionIdentifier -- which action button was pressed
           // response.Text -- text reply if action was TextReply type
       }
   }
   ```

6. **Create channels before sending notifications that reference them:**
   ```csharp
   notificationManager.AddChannel(new Channel
   {
       Identifier = "alerts",
       Importance = ChannelImportance.High,
       Sound = ChannelSound.High
   });
   ```

7. **Use the convenience `Send` extension for simple notifications:**
   ```csharp
   await notificationManager.Send("Title", "Message body");
   ```

8. **For platform-specific properties, use the native subclasses:**
   - Android: `AndroidNotification` and `AndroidChannel`
   - iOS: `AppleNotification` and `AppleChannel`

9. **Always inject `INotificationManager`** via constructor injection -- never create instances directly.

10. **Use `CancelScope` wisely when cancelling:**
    - `CancelScope.DisplayedOnly` -- clears only shown notifications.
    - `CancelScope.Pending` -- clears only scheduled/triggered notifications.
    - `CancelScope.All` -- clears everything (default).

## Namespace Ambiguities

- **`Notification`**: Both `Shiny.Notifications` and `Shiny.Push` define a `Notification` type. If both packages are referenced in the same project, do NOT add both namespaces as global usings. Use `Shiny.Notifications.Notification` FQN or a file-level `using Shiny.Notifications;` directive to disambiguate.

## Best Practices

- Always check the `AccessState` result before attempting to send notifications.
- Use channels to group notifications by category (e.g., "alerts", "reminders", "messages").
- The default channel (`Channel.Default`) always exists with `Identifier = "Notifications"` and `ChannelImportance.Low`.
- Do not remove the default channel -- the library will throw an `InvalidOperationException`.
- Set `BadgeCount` only on immediate notifications (not triggered ones) -- validation will fail otherwise.
- Use `IntervalTrigger` with either `Interval` (raw TimeSpan) or `TimeOfDay` (daily/weekly recurring), never both.
- For geofence notifications, ensure `Center` and `Radius` are both set on `GeofenceTrigger`.
- On Android, create a drawable resource named `notification` for the default small icon, or set `SmallIconResourceName` on `AndroidNotification`.
- Prefer the `RequestRequiredAccess` extension method to automatically determine needed permission flags from a `Notification` object.
- Use `Payload` dictionary on `Notification` to pass custom data that you can read back in your `INotificationDelegate.OnEntry`.

## AI Tool Integration (Shiny.Notifications.Extensions.AI)

The optional `Shiny.Notifications.Extensions.AI` package exposes `INotificationManager` as **reminder-framed** `Microsoft.Extensions.AI` tool functions (`AIFunction`s) for LLM agents. You opt-in exactly which operations the model can see — a read/write allow-list you control on behalf of the agent (**not** an OS permission prompt; the platform notification permission must already be granted). Read-only by default; write is opt-in. AOT-compatible (hand-built schemas, `JsonNode` results — no reflection).

```csharp
using Shiny.Notifications;
using Shiny.Notifications.Extensions.AI;

builder.Services.AddNotifications();                             // registers INotificationManager
builder.Services.AddNotificationAITools(tools => tools
    .AddReminders(ReminderAICapabilities.ReadWrite, channel: "reminders")   // channel is optional
);

// resolve the bundle and pass the tools to any IChatClient
var tools = sp.GetRequiredService<NotificationAITools>().Tools;
var response = await chatClient.GetResponseAsync(
    messages,
    new ChatOptions { Tools = [.. tools] }
);
```

Key types:
- `AddNotificationAITools(Action<INotificationAIToolBuilder>)` — DI extension; throws if nothing is added.
- `INotificationAIToolBuilder` — `AddReminders(ReminderAICapabilities, string? channel = null)`. The channel (if supplied) must already be registered via `AddChannel`.
- `ReminderAICapabilities` `[Flags]` — `None`, `Read` (default), `Write`, `ReadWrite`.
- `NotificationAITools` — resolve from DI; `.Tools` is `IReadOnlyList<AITool>`.

Generated tools (only for opted-in capabilities): `list_reminders` (pending/scheduled), `create_reminder` (omit both `scheduleFor`/`repeatDailyAt` to send now, `scheduleFor` for a one-time reminder, `repeatDailyAt` "HH:mm" for a daily one), `cancel_reminder` (by id). `scheduleFor` and `repeatDailyAt` are mutually exclusive; dates are ISO-8601.

> The AI tools assume permissions are already granted — they do **not** trigger the platform permission UI (needs a foreground activity). Call `INotificationManager.RequestAccess(...)` from the app before invoking the agent.

## Reference Files

- [API Reference](reference/api-reference.md)
