---
name: shiny-push
description: Guide for implementing push notifications in .NET MAUI apps using Shiny.Push (native FCM/APNs) and Shiny.Push.AzureNotificationHubs
auto_invoke: true
triggers:
  - push notification
  - push notifications
  - push delegate
  - push manager
  - IPushManager
  - IPushDelegate
  - firebase push
  - FCM
  - APNs
  - azure notification hub
  - azure notification hubs
  - ANH
  - remote notification
  - device token
  - registration token
  - push tag
  - push tags
  - AddPush
  - AddPushAzureNotificationHubs
  - PushAccessState
  - PushNotification
  - Shiny.Push
  - shiny push
---

# Shiny Push Notifications

## When to Use This Skill

Use this skill when the user needs to:
- Register for push notifications on iOS (APNs) or Android (FCM)
- Handle incoming push notifications (foreground and background)
- Handle push notification tap/entry events
- Manage push notification tags/topics
- Integrate Azure Notification Hubs as a push provider
- Implement a custom push delegate
- Configure Firebase for Android push
- Request push notification permissions
- Unregister from push notifications
- Customize Apple foreground notification presentation
- Build and display Android notifications from push data

## Library Overview

| Item | Value |
|------|-------|
| **NuGet (Native Push)** | `Shiny.Push` |
| **NuGet (Azure NH)** | `Shiny.Push.AzureNotificationHubs` |
| **Primary Namespace** | `Shiny.Push` |
| **Config Namespace** | `Shiny` (extension methods on `IServiceCollection`) |
| **Platforms** | iOS (APNs), Android (FCM), WebAssembly (experimental) |

## Setup

### Native Push (FCM on Android, APNs on iOS)

Register in your `MauiProgram.cs`:

```csharp
using Shiny;

builder.Services.AddPush<MyPushDelegate>();
```

On Android, this uses `FirebaseConfig` with embedded `google-services.json` by default. To provide Firebase values manually:

```csharp
#if ANDROID
builder.Services.AddPush<MyPushDelegate>(FirebaseConfig.FromValues(
    appId: "your-app-id",
    senderId: "your-sender-id",
    projectId: "your-project-id",
    apiKey: "your-api-key"
));
#else
builder.Services.AddPush<MyPushDelegate>();
#endif
```

### Azure Notification Hubs

```csharp
using Shiny;

builder.Services.AddPushAzureNotificationHubs<MyPushDelegate>(
    "Endpoint=sb://...;SharedAccessKeyName=...;SharedAccessKey=...",
    "your-hub-name"
);
```

On Android with custom Firebase config:

```csharp
#if ANDROID
builder.Services.AddPushAzureNotificationHubs<MyPushDelegate>(
    "Endpoint=sb://...",
    "your-hub-name",
    FirebaseConfig.FromValues("appId", "senderId", "projectId", "apiKey")
);
#endif
```

## Code Generation Instructions and Conventions

1. **Always implement `IPushDelegate`** (or subclass `PushDelegate`) to handle push events. Register it as a generic type parameter on `AddPush<T>()` or `AddPushAzureNotificationHubs<T>()`.

2. **Request access before using push.** Call `IPushManager.RequestAccess()` and check `PushAccessState.Status == AccessState.Available` before assuming push is working.

3. **Use `PushAccessState.Assert()`** when you want to throw on denied/restricted permissions rather than checking the status manually.

4. **Multiple delegates are supported.** You can register multiple `IPushDelegate` implementations; all will be called. Use `AddShinyService<T>()` for additional delegates.

5. **Apple-specific customization:**
   - Cast `IPushManager` to `IApplePushManager` for custom `UNAuthorizationOptions`.
   - Implement `IApplePushDelegate` (extends `IPushDelegate`) to control foreground presentation options and background fetch results.
   - On iOS, `PushNotification` may be an `ApplePushNotification` with access to the raw `NSDictionary` payload.

6. **Android-specific customization:**
   - `PushNotification` received in `OnReceived` may be an `AndroidPushNotification` with access to the native `RemoteMessage`.
   - Use `AndroidPushNotification.CreateBuilder()` to build a `NotificationCompat.Builder` from the push data.
   - Use `AndroidPushNotification.SendDefault(notificationId)` for quick notification display.
   - Configure `FirebaseConfig.DefaultChannel` to set a default `NotificationChannel`.
   - Configure `FirebaseConfig.IntentAction` to set a custom intent action for notification taps.

7. **Tags/Topics:**
   - Check `IPushManager.Tags != null` (or use `pushManager.IsTagsSupport()`) before using tag operations.
   - Native Firebase on Android supports tags via FCM topic subscriptions.
   - Azure Notification Hubs supports tags via installation tags.
   - Use extension methods `TrySetTags`, `TryGetTags`, `TryRequestAccessWithTags` for safe tag operations.

8. **Azure Notification Hubs specifics:**
   - Implement `IPushInstallationEvent` to modify the `Installation` object (add templates, tags) before it is sent to ANH.
   - Use `AzureNotificationConfig.BeforeSendInstallation` callback as an alternative to `IPushInstallationEvent`.
   - `AzureNotificationConfig.ExpirationTime` controls token expiration. Each `RequestAccess` or tag update bumps expiration.
   - `AzureNotificationConfig.AzureAuthenticationWaitTimeMs` (default 1000ms) adds a delay after registration to allow ANH propagation.

9. **Namespace conventions:** Extension methods on `IServiceCollection` live in the `Shiny` namespace. All push types live in `Shiny.Push`.

10. **Do NOT reference platform-specific types** (e.g., `AndroidPushNotification`, `ApplePushNotification`, `IApplePushManager`, `IApplePushDelegate`, `FirebaseConfig`) in shared/cross-platform code. Guard them with `#if ANDROID` / `#if APPLE` preprocessor directives or use runtime platform checks.

## Namespace Ambiguities

- **`Notification`**: Both `Shiny.Push` and `Shiny.Notifications` define a `Notification` type. If both packages are referenced in the same project, do NOT add both namespaces as global usings. Use `Shiny.Push.PushNotification` or FQN to disambiguate.

## Best Practices

- Always handle `OnNewToken` in your delegate to sync the updated token with your backend server.
- Always handle `OnEntry` to navigate the user to the appropriate screen when they tap a notification.
- Use `OnReceived` for silent/data-only notifications and background processing. On iOS, ensure `content-available: 1` is set in the push payload for background delivery.
- Check `PushAccessState.Status` after `RequestAccess()` -- do not assume success.
- On Android 13+, the POST_NOTIFICATIONS runtime permission is requested automatically by Shiny during `RequestAccess()`.
- Prefer `AddPush<TDelegate>()` over `AddPush()` + manual delegate registration to ensure correct service lifetime.
- For Azure Notification Hubs, always test with a sufficient `AzureAuthenticationWaitTimeMs` if you encounter "InstallationId not found" errors.
- The `RegistrationToken` on `IPushManager` is the provider-level token (e.g., ANH InstallationId), while `NativeRegistrationToken` is the raw OS token (FCM token or APNs device token). Use `RegistrationToken` when communicating with your backend.

## Reference Files

- [API Reference](reference/api-reference.md)
