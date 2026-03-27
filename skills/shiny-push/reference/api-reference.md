# Shiny.Push API Reference

## Installation

### Native Push (NuGet: Shiny.Push)

```xml
<PackageReference Include="Shiny.Push" Version="4.*" />
```

### Azure Notification Hubs (NuGet: Shiny.Push.AzureNotificationHubs)

```xml
<PackageReference Include="Shiny.Push.AzureNotificationHubs" Version="4.*" />
```

## Namespaces

```csharp
using Shiny;       // Extension methods (AddPush, AddPushAzureNotificationHubs, AccessState, ShinyPushIntents)
using Shiny.Push;  // Core push types (IPushManager, IPushDelegate, PushNotification, etc.)
```

---

## Enums

### AccessState (namespace: Shiny)

```csharp
public enum AccessState
{
    Unknown,
    NotSupported,
    NotSetup,
    Disabled,
    Restricted,
    Denied,
    Available
}
```

---

## Records

### PushAccessState (namespace: Shiny.Push)

```csharp
public record PushAccessState(
    AccessState Status,
    string? RegistrationToken
)
{
    public static PushAccessState Denied { get; }
    public void Assert(); // Throws PermissionException if Status != Available
}
```

### PushNotification (namespace: Shiny.Push)

```csharp
public record PushNotification(
    IDictionary<string, string> Data,
    Notification? Notification
);
```

### Notification (namespace: Shiny.Push)

```csharp
public record Notification(
    string? Title,
    string? Message
);
```

### ApplePushNotification (namespace: Shiny.Push) -- iOS only

```csharp
public record ApplePushNotification(
    IDictionary<string, string> Data,
    NSDictionary? RawPayload,
    Notification? Notification
) : PushNotification(Data, Notification);
```

### AndroidPushNotification (namespace: Shiny.Push) -- Android only

```csharp
public record AndroidPushNotification(
    Notification? Notification,
    RemoteMessage NativeMessage,
    FirebaseConfig Config,
    AndroidPlatform Platform
) : PushNotification(NativeMessage.Data, Notification)
{
    // Creates a NotificationCompat.Builder pre-populated from the RemoteMessage
    public NotificationCompat.Builder CreateBuilder();

    // Posts the notification using the given builder
    public void Notify(int notificationId, NotificationCompat.Builder builder);

    // Creates the default builder and posts the notification
    public void SendDefault(int notificationId);
}
```

### FirebaseConfig (namespace: Shiny.Push) -- Android only

```csharp
public record FirebaseConfig(
    bool UseEmbeddedConfiguration = true,
    string? AppId = null,
    string? SenderId = null,
    string? ProjectId = null,
    string? ApiKey = null,
    NotificationChannel? DefaultChannel = null,
    string? IntentAction = null
)
{
    public static FirebaseConfig Embedded { get; }
    public static FirebaseConfig FromValues(string appId, string senderId, string projectId, string apiKey);
    public void AssertValid();
}
```

### AzureNotificationConfig (namespace: Shiny)

```csharp
public record AzureNotificationConfig(
    string ListenerConnectionString,
    string HubName,
    int AzureAuthenticationWaitTimeMs = 1000,
    TimeSpan? ExpirationTime = null,
    Func<Installation, Task>? BeforeSendInstallation = null
);
```

---

## Interfaces

### IPushManager (namespace: Shiny.Push)

The primary interface for push notification management. Resolved from DI.

```csharp
public interface IPushManager
{
    /// Tag support capability -- null if the provider does not support tags
    IPushTagSupport? Tags { get; }

    /// The current provider-level registration token (e.g., ANH InstallationId or FCM token)
    string? RegistrationToken { get; }

    /// The raw OS-level token (APNs device token string or FCM token). Use for debugging only.
    string? NativeRegistrationToken { get; }

    /// Requests platform permission and registers for push notifications
    Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default);

    /// Unregisters from push notifications
    Task UnRegister();
}
```

### IApplePushManager (namespace: Shiny.Push) -- iOS only

Extends `IPushManager` with Apple-specific authorization options.

```csharp
public interface IApplePushManager : IPushManager
{
    /// Requests access with specific UNAuthorizationOptions (Alert, Badge, Sound, etc.)
    Task<PushAccessState> RequestAccess(
        UNAuthorizationOptions options = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
        CancellationToken cancelToken = default
    );
}
```

### IPushDelegate (namespace: Shiny.Push)

Implement this interface (or subclass `PushDelegate`) to handle push notification lifecycle events.

```csharp
public interface IPushDelegate
{
    /// Called when the user taps/responds to a push notification
    Task OnEntry(PushNotification notification);

    /// Called when a push is received (foreground or background with content-available:1 on iOS)
    Task OnReceived(PushNotification notification);

    /// Called when the registration token changes
    Task OnNewToken(string token);

    /// Called when the user denies permission or UnRegister() is called
    Task OnUnRegistered(string token);
}
```

### PushDelegate (namespace: Shiny.Push) -- Base class

A convenience base class with no-op implementations of all `IPushDelegate` methods. Override only what you need.

```csharp
public class PushDelegate : IPushDelegate
{
    public virtual Task OnEntry(PushNotification notification) => Task.CompletedTask;
    public virtual Task OnReceived(PushNotification notification) => Task.CompletedTask;
    public virtual Task OnNewToken(string token) => Task.CompletedTask;
    public virtual Task OnUnRegistered(string token) => Task.CompletedTask;
}
```

### IApplePushDelegate (namespace: Shiny.Push) -- iOS only

Extends `IPushDelegate` with Apple-specific foreground presentation and background fetch control.

```csharp
public interface IApplePushDelegate : IPushDelegate
{
    /// Return presentation options for foreground notifications.
    /// Return null to use the default (List | Banner) and allow other delegates to respond.
    UNNotificationPresentationOptions? GetPresentationOptions(PushNotification notification);

    /// Return the fetch result for background notifications.
    /// Return null to use the default (NewData) and allow other delegates to respond.
    UIBackgroundFetchResult? GetFetchResult(PushNotification notification);
}
```

### IPushProvider (namespace: Shiny.Push)

Implement this to create a custom push provider that bridges between the native OS token and a third-party service. You typically do NOT need to implement this unless building a custom provider.

```csharp
public interface IPushProvider
{
    // Android signature:
    Task<string> Register(string nativeToken);

    // iOS signature:
    Task<string> Register(NSData nativeToken);

    Task UnRegister();
}
```

### IPushTagSupport (namespace: Shiny.Push)

Extends `IPushProvider` with tag/topic management. Accessed via `IPushManager.Tags`.

```csharp
public interface IPushTagSupport : IPushProvider
{
    /// Clears all current tags and sets the new array
    Task SetTags(params string[]? tags);

    /// Adds a single tag to the current set
    Task AddTag(string tag);

    /// Removes a single tag from the current set
    Task RemoveTag(string tag);

    /// Clears all registered tags
    Task ClearTags();

    /// The current set of registered tags
    string[]? RegisteredTags { get; }
}
```

### IPushInstallationEvent (namespace: Shiny) -- Azure Notification Hubs only

Implement to modify the ANH `Installation` object before it is sent to the server.

```csharp
public interface IPushInstallationEvent
{
    /// Called before the installation is sent to Azure Notification Hubs
    Task OnBeforeSend(Installation installation);
}
```

---

## Extension Methods

### ServiceCollectionExtensions -- Shiny.Push (namespace: Shiny)

```csharp
public static class ServiceCollectionExtensions
{
    // Registers native push (APNs on iOS, FCM with embedded config on Android)
    public static IServiceCollection AddPush(this IServiceCollection services);

    // Registers native push with a typed delegate
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services)
        where TDelegate : class, IPushDelegate;

    // [Android only] Registers native push with explicit FirebaseConfig
    public static IServiceCollection AddPush(this IServiceCollection services, FirebaseConfig config);

    // [Android only] Registers native push with explicit FirebaseConfig and typed delegate
    public static IServiceCollection AddPush<TDelegate>(this IServiceCollection services, FirebaseConfig config)
        where TDelegate : class, IPushDelegate;
}
```

### ServiceCollectionExtensions -- Shiny.Push.AzureNotificationHubs (namespace: Shiny)

```csharp
public static class ServiceCollectionExtensions
{
    // Registers push with Azure Notification Hubs provider
    public static IServiceCollection AddPushAzureNotificationHubs(
        this IServiceCollection services,
        string listenerConnectionString,
        string hubName
    );

    // Registers push with Azure Notification Hubs provider and typed delegate
    public static IServiceCollection AddPushAzureNotificationHubs<TPushDelegate>(
        this IServiceCollection services,
        string listenerConnectionString,
        string hubName
    ) where TPushDelegate : class, IPushDelegate;

    // [Android only] Registers push with Azure Notification Hubs and custom FirebaseConfig
    public static IServiceCollection AddPushAzureNotificationHubs(
        this IServiceCollection services,
        string listenerConnectionString,
        string hubName,
        FirebaseConfig firebaseConfig
    );

    // [Android only] Registers push with Azure Notification Hubs, custom FirebaseConfig, and typed delegate
    public static IServiceCollection AddPushAzureNotificationHubs<TPushDelegate>(
        this IServiceCollection services,
        string listenerConnectionString,
        string hubName,
        FirebaseConfig firebaseConfig
    ) where TPushDelegate : class, IPushDelegate;
}
```

### PushExtensions (namespace: Shiny.Push)

```csharp
public static class PushExtensions
{
    /// Returns true if the push manager's provider supports tags
    public static bool IsTagsSupport(this IPushManager push);

    /// Attempts to set tags; returns false if tags are not supported
    public static Task<bool> TrySetTags(this IPushManager pushManager, params string[] tags);

    /// Returns registered tags, or null if tags are not supported
    public static string[]? TryGetTags(this IPushManager pushManager);

    /// Requests access and then sets tags if supported
    public static Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, params string[] tags);
}
```

### PlatformExtensions (namespace: Shiny.Push) -- iOS only

```csharp
public static class PlatformExtensions
{
    /// Converts an APNs NSData device token to a hex string
    public static string ToPushTokenString(this NSData deviceToken);
}
```

---

## Android Constants

### ShinyPushIntents (namespace: Shiny)

```csharp
public static class ShinyPushIntents
{
    public const string NotificationClickAction = "SHINY_PUSH_NOTIFICATION_CLICK";
}
```

### ShinyFirebaseService (namespace: Shiny.Push) -- Android only

```csharp
[Service(Exported = true)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class ShinyFirebaseService : FirebaseMessagingService
{
    public const string IntentAction = "com.google.firebase.MESSAGING_EVENT";
}
```

---

## Usage Examples

### Basic Push Registration

```csharp
// MauiProgram.cs
builder.Services.AddPush<MyPushDelegate>();

// MyPushDelegate.cs
public class MyPushDelegate : PushDelegate
{
    readonly ILogger<MyPushDelegate> logger;

    public MyPushDelegate(ILogger<MyPushDelegate> logger)
        => this.logger = logger;

    public override Task OnEntry(PushNotification notification)
    {
        this.logger.LogInformation("User tapped notification: {Title}", notification.Notification?.Title);
        // Navigate to appropriate page based on notification.Data
        return Task.CompletedTask;
    }

    public override Task OnReceived(PushNotification notification)
    {
        this.logger.LogInformation("Push received: {Data}", string.Join(", ", notification.Data));
        return Task.CompletedTask;
    }

    public override Task OnNewToken(string token)
    {
        this.logger.LogInformation("New push token: {Token}", token);
        // Send token to your backend server
        return Task.CompletedTask;
    }

    public override Task OnUnRegistered(string token)
    {
        this.logger.LogInformation("Push unregistered for token: {Token}", token);
        // Notify your backend to remove this token
        return Task.CompletedTask;
    }
}
```

### Requesting Access in a ViewModel

```csharp
public class MyViewModel
{
    readonly IPushManager pushManager;

    public MyViewModel(IPushManager pushManager)
        => this.pushManager = pushManager;

    public async Task RegisterForPush()
    {
        var result = await this.pushManager.RequestAccess();
        if (result.Status == AccessState.Available)
        {
            // Successfully registered. result.RegistrationToken has the token.
            Console.WriteLine($"Push Token: {result.RegistrationToken}");
        }
        else
        {
            Console.WriteLine($"Push registration failed: {result.Status}");
        }
    }

    public async Task UnregisterPush()
    {
        await this.pushManager.UnRegister();
    }
}
```

### Using Tags

```csharp
public async Task SetupTagsAsync(IPushManager pushManager)
{
    // Safe approach -- works even if provider does not support tags
    var result = await pushManager.TryRequestAccessWithTags("news", "sports");

    // Check support explicitly
    if (pushManager.IsTagsSupport())
    {
        await pushManager.Tags!.AddTag("breaking-news");
        await pushManager.Tags!.RemoveTag("sports");

        var currentTags = pushManager.Tags!.RegisteredTags;
    }
}
```

### Apple-Specific: Custom Authorization Options

```csharp
#if IOS
public async Task RequestApplePush(IPushManager pushManager)
{
    if (pushManager is IApplePushManager applePush)
    {
        var result = await applePush.RequestAccess(
            UNAuthorizationOptions.Alert |
            UNAuthorizationOptions.Sound |
            UNAuthorizationOptions.Badge |
            UNAuthorizationOptions.Provisional
        );
        result.Assert(); // throws if not Available
    }
}
#endif
```

### Apple-Specific: Foreground Presentation Delegate

```csharp
#if IOS
public class MyApplePushDelegate : PushDelegate, IApplePushDelegate
{
    public UNNotificationPresentationOptions? GetPresentationOptions(PushNotification notification)
    {
        // Show banner and play sound even when app is in foreground
        return UNNotificationPresentationOptions.Banner |
               UNNotificationPresentationOptions.Sound;
    }

    public UIBackgroundFetchResult? GetFetchResult(PushNotification notification)
    {
        return UIBackgroundFetchResult.NewData;
    }
}
#endif
```

### Android-Specific: Custom Notification Display

```csharp
#if ANDROID
public class MyAndroidPushDelegate : PushDelegate
{
    static int notificationId = 0;

    public override Task OnReceived(PushNotification notification)
    {
        if (notification is AndroidPushNotification androidPush)
        {
            // Option 1: Quick default display
            androidPush.SendDefault(++notificationId);

            // Option 2: Customize the notification
            var builder = androidPush.CreateBuilder();
            builder.SetAutoCancel(true);
            builder.SetPriority((int)NotificationPriority.High);
            androidPush.Notify(++notificationId, builder);
        }
        return Task.CompletedTask;
    }
}
#endif
```

### Azure Notification Hubs with Installation Event

```csharp
// MauiProgram.cs
builder.Services.AddPushAzureNotificationHubs<MyPushDelegate>(
    "Endpoint=sb://myhub.servicebus.windows.net/;SharedAccessKeyName=DefaultListenSharedAccessSignature;SharedAccessKey=...",
    "myhub"
);
builder.Services.AddShinyService<MyInstallationEvent>();

// MyInstallationEvent.cs
public class MyInstallationEvent : IPushInstallationEvent
{
    public Task OnBeforeSend(Installation installation)
    {
        // Add custom tags based on user profile
        installation.Tags ??= new List<string>();
        installation.Tags.Add($"userId:{GetCurrentUserId()}");

        // Add a template
        installation.Templates ??= new Dictionary<string, InstallationTemplate>();
        installation.Templates["generic"] = new InstallationTemplate
        {
            Body = "{\"aps\":{\"alert\":\"$(message)\"}}"
        };

        return Task.CompletedTask;
    }

    string GetCurrentUserId() => "user-123";
}
```

### Android Firebase Configuration

```csharp
// Using embedded google-services.json (default)
builder.Services.AddPush<MyPushDelegate>();

// Using explicit values
#if ANDROID
builder.Services.AddPush<MyPushDelegate>(
    FirebaseConfig.FromValues(
        appId: "1:1234567890:android:abcdef",
        senderId: "1234567890",
        projectId: "my-project",
        apiKey: "AIzaSy..."
    )
);
#endif

// With a custom notification channel
#if ANDROID
var channel = new Android.App.NotificationChannel(
    "push_channel",
    "Push Notifications",
    Android.App.NotificationImportance.High
);
builder.Services.AddPush<MyPushDelegate>(new FirebaseConfig(
    DefaultChannel: channel
));
#endif
```

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `PushAccessState.Status` returns `Denied` | User denied the notification permission. On Android 13+, the POST_NOTIFICATIONS permission is required. On iOS, check Settings > Notifications. |
| `PushAccessState.Status` returns `NotSupported` | Running on iOS Simulator, which does not support push notifications. |
| `OnReceived` not firing in background (iOS) | Ensure the push payload includes `content-available: 1` in the `aps` dictionary for silent/data notifications. |
| `OnEntry` not firing on Android | Verify the intent action matches. Default is `SHINY_PUSH_NOTIFICATION_CLICK`. If using a custom `FirebaseConfig.IntentAction`, ensure consistency. |
| `RegistrationToken` is null after `RequestAccess` | Check that Firebase is properly initialized on Android (`google-services.json` or manual config). On iOS, ensure push entitlements are configured. |
| Azure NH "InstallationId not found" | Increase `AzureNotificationConfig.AzureAuthenticationWaitTimeMs` (default 1000ms). Azure needs time to propagate the installation. |
| Tags not working | Check `IPushManager.Tags != null`. Native Firebase uses FCM topic subscriptions. Azure NH uses installation tags. If `Tags` is null, the provider does not support tags. |
| Firebase did not initialize (Android) | Ensure `google-services.json` is in the Android project with build action `GoogleServicesJson`. Alternatively, use `FirebaseConfig.FromValues()`. |
| Multiple delegates not all firing | Ensure each delegate is registered via `AddShinyService<T>()`. The first `IApplePushDelegate` returning a non-null value from `GetPresentationOptions` or `GetFetchResult` wins. |
