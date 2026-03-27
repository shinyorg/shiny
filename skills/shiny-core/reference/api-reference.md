# Shiny Core API Reference

## Installation

```xml
<!-- Core (required by all Shiny modules) -->
<PackageReference Include="Shiny.Core" />

<!-- MAUI hosting integration -->
<PackageReference Include="Shiny.Hosting.Maui" />

<!-- Native hosting base classes (non-MAUI) -->
<PackageReference Include="Shiny.Hosting.Native" />

<!-- Device monitoring (connectivity + battery) -->
<PackageReference Include="Shiny.Support.DeviceMonitoring" />

<!-- File-based entity repository -->
<PackageReference Include="Shiny.Support.Repositories" />

<!-- Remote configuration + platform preferences -->
<PackageReference Include="Shiny.Extensions.Configuration" />
```

Dependencies: `System.Reactive`, `Microsoft.Extensions.DependencyInjection`, `Microsoft.Extensions.Logging`

## Namespaces

- `Shiny` -- Core types, enums, extension methods, platform types
- `Shiny.Hosting` -- `IHost`, `Host`, lifecycle interfaces
- `Shiny.Stores` -- Key-value stores, serializer, object store binder
- `Shiny.Stores.Impl` -- Default implementations (memory store, default serializer)
- `Shiny.Collections` -- Observable collection types
- `Shiny.Reflection` -- Reflection extension helpers
- `Shiny.Net` -- Connectivity types
- `Shiny.Power` -- Battery types
- `Shiny.Support.Repositories` -- Repository types
- `Shiny.Extensions.Configuration` -- Remote configuration types

---

## Hosting

### IHost

```csharp
namespace Shiny.Hosting;

public interface IHost : IDisposable
{
    IServiceProvider Services { get; }
    ILoggerFactory Logging { get; }
    void Run();
}
```

### Host (Static Access)

```csharp
namespace Shiny.Hosting;

public class Host : IHost
{
    public static IHost Current { get; }
    public static bool IsInitialized { get; }
    public static IServiceProvider ServiceProvider { get; }
    public static ILoggerFactory LoggingFactory { get; }
    public static T? GetService<T>();

    // Android-only
    public static AndroidPlatform Platform { get; }
    public static AndroidLifecycleExecutor Lifecycle { get; }

    // iOS/Mac Catalyst-only
    public static IosPlatform Platform { get; }
    public static IosLifecycleExecutor Lifecycle { get; }
}
```

### HostBuilder

```csharp
namespace Shiny;

public class HostBuilder
{
    public static HostBuilder Create(
        IServiceCollection? services = null,
        ILoggingBuilder? loggingBuilder = null
    );

    public IServiceCollection Services { get; }
    public ILoggingBuilder Logging { get; }
    public Func<IServiceCollection, IServiceProvider>? ConfigureContainer { get; set; }
    public IHost Build();
}
```

### MAUI Extension (Shiny.Hosting.Maui)

```csharp
namespace Shiny;

public static class ShinyExtensions
{
    public static MauiAppBuilder UseShiny(this MauiAppBuilder builder);
}
```

### Native Hosting Base Classes (Shiny.Hosting.Native)

```csharp
namespace Shiny;

// iOS
public abstract class ShinyAppDelegate : UIApplicationDelegate
{
    protected abstract IHost CreateShinyHost();
}

// Android Application
public abstract class ShinyAndroidApplication : Application
{
    protected abstract IHost CreateShinyHost();
}

// Android Activity
public abstract class ShinyAndroidActivity : AppCompatActivity { }
```

---

## Startup and Lifecycle

### IShinyStartupTask

```csharp
namespace Shiny;

public interface IShinyStartupTask
{
    void Start();
}
```

### IShinyComponentStartup

```csharp
namespace Shiny;

public interface IShinyComponentStartup
{
    void ComponentStart();
}
```

### ShinyLifecycleTask

```csharp
namespace Shiny;

// On Android: implements IAndroidLifecycle.IApplicationLifecycle, IShinyStartupTask
// On iOS:     implements IIosLifecycle.IApplicationLifecycle, IShinyStartupTask
public partial class ShinyLifecycleTask
{
    public bool? IsInForeground { get; }
    public virtual void Start();
    protected virtual void OnStateChanged(bool backgrounding);
    public void OnBackground();
    public virtual void OnForeground();
}
```

---

## Enums

### AccessState

```csharp
namespace Shiny;

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

### ActivityState (Android)

```csharp
namespace Shiny;

public enum ActivityState
{
    Created,
    Resumed,
    Paused,
    Destroyed,
    SaveInstanceState,
    Started,
    Stopped
}
```

---

## Records

### ActivityChanged (Android)

```csharp
namespace Shiny;

public record ActivityChanged(
    Activity Activity,
    ActivityState State,
    Bundle? StateBundle
);
```

### PermissionRequestResult (Android)

```csharp
namespace Shiny;

public record PermissionRequestResult(
    int RequestCode,
    string[] Permissions,
    Permission[] GrantResults
)
{
    public bool IsSuccess();
    public int Length { get; }
    public (string, Permission) this[int index] { get; }
    public bool IsGranted(string permission);
}
```

### AndroidPermission (Android)

```csharp
namespace Shiny;

public record AndroidPermission(
    string Permission,
    int? MinSdkVersion,
    int? MaxSdkVersion
);
```

### ItemChanged\<T\>

```csharp
namespace Shiny;

public record ItemChanged<T>(
    T Object,
    string? PropertyName
)
{
    public object? GetValue();
}
```

---

## Exceptions

### PermissionException

```csharp
namespace Shiny;

public class PermissionException : Exception
{
    public PermissionException(string module, AccessState badStatus);
}
```

---

## Platform Abstractions

### IPlatform

```csharp
namespace Shiny;

public interface IPlatform
{
    void InvokeOnMainThread(Action action);
    DirectoryInfo AppData { get; }
    DirectoryInfo Cache { get; }
    DirectoryInfo Public { get; }
}
```

### AndroidPlatform (Android)

Extends `IPlatform` with Android-specific functionality:

```csharp
namespace Shiny;

public partial class AndroidPlatform : IPlatform
{
    public Application AppContext { get; }
    public Activity? CurrentActivity { get; }
    public IObservable<ActivityChanged> WhenActivityChanged();
    public IObservable<ActivityChanged> WhenActivityStatusChanged();

    // Permissions
    public AccessState GetCurrentPermissionStatus(string androidPermission);
    public IObservable<AccessState> RequestAccess(string androidPermissions);
    public IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions);
    public IObservable<PermissionRequestResult> RequestFilteredPermissions(params AndroidPermission[] androidPermissions);
    public Task<AccessState> RequestForegroundServicePermissions();
    public bool IsInManifest(string androidPermission);
    public bool EnsureAllManifestEntries(params AndroidPermission[] androidPermissions);

    // Services
    public void StartService(Type serviceType, bool stopWithTask = true);
    public void StopService(Type serviceType);

    // Resources
    public int GetDrawableByName(string name);
    public int GetNotificationIconResource();
    public int GetColorByName(string colorName);
    public int GetResourceIdByName(string iconName);
    public int GetRawResourceIdByName(string rawName);

    // Utilities
    public T GetSystemService<T>(string key) where T : Java.Lang.Object;
    public Intent CreateIntent<T>(params string[] actions);
    public PendingIntent GetBroadcastPendingIntent<T>(...);
    public PendingIntentFlags GetPendingIntentFlags(PendingIntentFlags flags);
    public void RegisterBroadcastReceiver<T>(bool exported, params string[] actions) where T : BroadcastReceiver, new();
    public T GetIntentValue<T>(string intentAction, Func<Intent, T> transform);
}
```

### IosPlatform (iOS/Mac Catalyst)

```csharp
namespace Shiny;

public class IosPlatform : IPlatform
{
    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }
    public string AppIdentifier { get; }
    public void InvokeOnMainThread(Action action);
}
```

---

## Android Lifecycle Interfaces

```csharp
namespace Shiny.Hosting;

public interface IAndroidLifecycle
{
    public interface IApplicationLifecycle
    {
        void OnForeground();
        void OnBackground();
    }

    public interface IOnActivityOnCreate
    {
        void ActivityOnCreate(Activity activity, Bundle? savedInstanceState);
    }

    public interface IOnActivityRequestPermissionsResult
    {
        void Handle(Activity activity, int requestCode, string[] permissions, Permission[] grantResults);
    }

    public interface IOnActivityNewIntent
    {
        void Handle(Activity activity, Intent intent);
    }

    public interface IOnActivityResult
    {
        void Handle(Activity activity, int requestCode, Result resultCode, Intent data);
    }
}
```

---

## iOS Lifecycle Interfaces

```csharp
namespace Shiny.Hosting;

public interface IIosLifecycle
{
    public interface IApplicationLifecycle
    {
        void OnForeground();
        void OnBackground();
    }

    public interface IOnFinishedLaunching
    {
        void Handle(UIApplicationLaunchEventArgs args);
    }

    public interface IRemoteNotifications
    {
        void OnRegistered(NSData deviceToken);
        void OnFailedToRegister(NSError error);
        void OnDidReceive(NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler);
    }

    public interface INotificationHandler
    {
        void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler);
        void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler);
    }

    public interface IHandleEventsForBackgroundUrl
    {
        bool Handle(string sessionIdentifier, Action completionHandler);
    }

    public interface IContinueActivity
    {
        bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler);
    }
}
```

---

## Key-Value Stores

### IKeyValueStore

```csharp
namespace Shiny.Stores;

public interface IKeyValueStore
{
    string Alias { get; }
    bool IsReadOnly { get; }
    bool Remove(string key);
    void Clear();
    bool Contains(string key);
    object? Get(Type type, string key);
    void Set(string key, object value);
}
```

Built-in store aliases:
- `"settings"` -- Platform preferences (SharedPreferences on Android, NSUserDefaults on iOS)
- `"secure"` -- Secure storage (Android Keystore, iOS Keychain)
- `"memory"` -- In-memory store (not persisted)

### IKeyValueStoreFactory

```csharp
namespace Shiny.Stores;

public interface IKeyValueStoreFactory
{
    string[] AvailableStores { get; }
    bool HasStore(string aliasName);
    IKeyValueStore GetStore(string aliasName);
    IKeyValueStore DefaultStore { get; }
    void SetDefaultStore(string aliasName);
}
```

### ISerializer

```csharp
namespace Shiny.Stores;

public interface ISerializer
{
    T Deserialize<T>(string value);
    object Deserialize(Type objectType, string value);
    string Serialize(object value);
}
```

Default implementation uses `System.Text.Json`.

### IObjectStoreBinder

```csharp
namespace Shiny.Stores;

public interface IObjectStoreBinder
{
    void Bind(INotifyPropertyChanged npc, string? keyValueStoreAlias = null);
    void Bind(INotifyPropertyChanged npc, IKeyValueStore store);
    void UnBind(INotifyPropertyChanged npc);
    void UnBindAll();
}
```

### ObjectStoreBinderAttribute

```csharp
namespace Shiny.Stores;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ObjectStoreBinderAttribute : Attribute
{
    public ObjectStoreBinderAttribute(string storeAlias);
    public string StoreAlias { get; }
}
```

---

## Store Extension Methods

```csharp
namespace Shiny;

public static class StoreExtensions
{
    // Type-safe get with default value
    public static T Get<T>(this IKeyValueStore store, string key, T defaultValue = default);

    // Get that throws if key is missing
    public static T GetRequired<T>(this IKeyValueStore store, string key);

    // Set value or remove if null/default
    public static void SetOrRemove(this IKeyValueStore store, string key, object? value);

    // Thread-safe integer incrementor
    public static int IncrementValue(this IKeyValueStore store, string key = "NextId");

    // Set only if key does not already exist
    public static bool SetDefault<T>(this IKeyValueStore store, string key, T value);

    // Check if object is null or default for its type
    public static bool IsNullOrDefault(this object? obj);
}
```

---

## Collections

### INotifyReadOnlyCollection\<T\>

```csharp
namespace Shiny;

public interface INotifyReadOnlyCollection<T> : INotifyCollectionChanged, IReadOnlyList<T> { }
```

### INotifyCollectionChanged\<T\>

```csharp
namespace Shiny.Collections;

public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, IList<T>, INotifyReadOnlyCollection<T>
{
    void AddRange(params IEnumerable<T> items);
    void RemoveRange(params IEnumerable<T> items);
    void ReplaceAll(params IEnumerable<T> items);
}
```

### BindingList\<T\>

Thread-safe `ObservableCollection<T>` implementing `INotifyCollectionChanged<T>`:

```csharp
namespace Shiny.Collections;

public class BindingList<T> : ObservableCollection<T>, INotifyCollectionChanged<T>
{
    public void AddRange(params IEnumerable<T> items);
    public void RemoveRange(params IEnumerable<T> items);
    public void ReplaceAll(params IEnumerable<T> newItems);
}
```

---

## Base Classes

### NotifyPropertyChanged

```csharp
namespace Shiny;

public class NotifyPropertyChanged : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool HasSubscribers { get; }
    protected virtual void OnNpcHookChanged(bool hasSubscribers);
    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null);
    protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> expression);
    protected virtual bool Set<T>(ref T property, T value, [CallerMemberName] string? propertyName = null);
}
```

### ShinySubject\<T\>

Error-safe `ISubject<T>` with logging support:

```csharp
namespace Shiny;

public class ShinySubject<T> : ISubject<T>
{
    public ShinySubject(ILogger? logger = null);
    public ILogger? Logger { get; set; }
    public void OnCompleted();
    public void OnError(Exception error);
    public void OnNext(T value);
    public IDisposable Subscribe(IObserver<T> observer);
}
```

---

## Observable Extension Methods

```csharp
namespace Shiny;

public static class ObservableExtensions
{
    // Monitor a specific property on an INPC object
    public static IObservable<TRet?> WhenAnyProperty<TSender, TRet>(
        this TSender This, Expression<Func<TSender, TRet>> expression
    ) where TSender : INotifyPropertyChanged;

    // Monitor all property changes on an INPC object
    public static IObservable<ItemChanged<TSender>> WhenAnyProperty<TSender>(
        this TSender This
    ) where TSender : INotifyPropertyChanged;

    // SwitchMap equivalent
    public static IObservable<U> SelectSwitch<T, U>(
        this IObservable<T> current, Func<T, IObservable<U>> next
    );

    // Async select (Task<U>)
    public static IObservable<U> SelectAsync<T, U>(
        this IObservable<T> observable, Func<Task<U>> task
    );

    // Async select (CancellationToken)
    public static IObservable<U> SelectAsync<T, U>(
        this IObservable<T> observable, Func<CancellationToken, Task<U>> task
    );

    // Conditional ObserveOn
    public static IObservable<T> ObserveOnIf<T>(
        this IObservable<T> ob, IScheduler? scheduler
    );

    // Execute action only on first emission
    public static IObservable<T> DoOnce<T>(this IObservable<T> obs, Action<T> action);

    // Add to CompositeDisposable
    public static T DisposedBy<T>(
        this T @this, CompositeDisposable compositeDisposable
    ) where T : IDisposable;

    // OnNext + OnCompleted in one call
    public static void Respond<T>(this IObserver<T> ob, T value);

    // Async subscribe (sequential)
    public static IDisposable SubscribeAsync<T>(
        this IObservable<T> observable, Func<T, Task> onNextAsync
    );
    public static IDisposable SubscribeAsync<T>(
        this IObservable<T> observable, Func<T, Task> onNextAsync, Action<Exception> onError
    );
    public static IDisposable SubscribeAsync<T>(
        this IObservable<T> observable, Func<T, Task> onNextAsync,
        Action<Exception> onError, Action onComplete
    );

    // Async subscribe (concurrent)
    public static IDisposable SubscribeAsyncConcurrent<T>(
        this IObservable<T> observable, Func<T, Task> onNextAsync
    );
    public static IDisposable SubscribeAsyncConcurrent<T>(
        this IObservable<T> observable, Func<T, Task> onNextAsync, int maxConcurrent
    );
}
```

---

## General Extension Methods

```csharp
namespace Shiny;

public static class GeneralExtensions
{
    // Conditional Where filter (skips if expression is null)
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> en, Expression<Func<T, bool>>? expression
    );

    // String.IsNullOrWhiteSpace shorthand
    public static bool IsEmpty(this string? s);

    // Assert AccessState is Available (throws if not)
    public static void Assert(
        this AccessState state, string? message = null, bool allowRestricted = false
    );
}
```

---

## Service Provider Extension Methods

```csharp
namespace Shiny;

public static class ServiceProviderExtensions
{
    // Check if a service type is registered
    public static bool HasService<TService>(this IServiceCollection services);
    public static bool HasService(this IServiceCollection services, Type serviceType);

    // Check if an implementation type is registered
    public static bool HasImplementation<TImpl>(this IServiceCollection services);
    public static bool HasImplementation(this IServiceCollection services, Type implementationType);

    // Lazy service resolution
    public static Lazy<T> GetLazyService<T>(this IServiceProvider services, bool required = false);

    // Register implementation for all its interfaces with auto-binding
    public static IServiceCollection AddShinyService<TImpl>(this IServiceCollection services) where TImpl : class;
    public static IServiceCollection AddShinyService(this IServiceCollection services, Type implementationType);

    // Run async delegates on all registered services of a type
    public static Task RunDelegates<T>(this IServiceProvider services, Func<T, Task> execute, ILogger logger);
    public static Task RunDelegates<T>(this IEnumerable<T> services, Func<T, Task> execute, ILogger logger);
}
```

---

## Platform Extension Methods

```csharp
namespace Shiny;

public static class PlatformExtensions
{
    // Invoke async Task on main thread
    public static Task<T> InvokeTaskOnMainThread<T>(
        this IPlatform platform, Func<Task<T>> func, CancellationToken cancelToken = default
    );
    public static Task InvokeTaskOnMainThread(
        this IPlatform platform, Func<Task> func, CancellationToken cancelToken = default
    );

    // Invoke sync function on main thread, return Task
    public static Task<T> InvokeOnMainThreadAsync<T>(
        this IPlatform platform, Func<T> func, CancellationToken cancelToken = default
    );
    public static Task InvokeOnMainThreadAsync(
        this IPlatform platform, Action action, CancellationToken cancelToken = default
    );

    // Extract embedded resource to file
    public static string ResourceToFilePath(
        this IPlatform platform, Assembly assembly, string resourceName
    );
}
```

---

## Device Monitoring (Shiny.Support.DeviceMonitoring)

### IConnectivity

```csharp
namespace Shiny.Net;

public interface IConnectivity
{
    IObservable<IConnectivity> WhenChanged();
    ConnectionTypes ConnectionTypes { get; }
    NetworkAccess Access { get; }
}
```

### ConnectionTypes (Flags)

```csharp
namespace Shiny.Net;

[Flags]
public enum ConnectionTypes
{
    None = 0,
    Unknown = 1,
    Bluetooth = 2,
    Wired = 4,
    Wifi = 8,
    Cellular = 16
}
```

### NetworkAccess

```csharp
namespace Shiny.Net;

public enum NetworkAccess
{
    Unknown,
    None,
    Local,
    ConstrainedInternet,
    Internet
}
```

### InternetAccess

```csharp
namespace Shiny.Net;

public enum InternetAccess
{
    None = 0,
    Any = 1,
    Unmetered = 2
}
```

### Connectivity Extension Methods

```csharp
namespace Shiny;

public static class ConnectivityExtensions
{
    public static bool IsInternetAvailable(this IConnectivity connectivity, bool allowConstrained = true);
    public static IObservable<bool> WhenInternetStatusChanged(this IConnectivity connectivity, bool allowConstrained = true);
}
```

### IBattery

```csharp
namespace Shiny.Power;

public interface IBattery
{
    IObservable<IBattery> WhenChanged();
    BatteryState Status { get; }
    double Level { get; }
}
```

### BatteryState

```csharp
public enum BatteryState
{
    Unknown,
    None,
    Charging,
    Full,
    NotCharging,
    Discharging
}
```

### Battery Extension Methods

```csharp
namespace Shiny;

public static class BatteryExtensions
{
    public static IObservable<bool> WhenChargingChanged(this IBattery battery);
    public static bool IsPluggedIn(this IBattery battery);
}
```

### Registration

```csharp
namespace Shiny;

public static class ServiceCollectionExtensions
{
    public static void AddConnectivity(this IServiceCollection services);
    public static void AddBattery(this IServiceCollection services);
}
```

---

## Repository (Shiny.Support.Repositories)

### IRepositoryEntity

```csharp
namespace Shiny.Support.Repositories;

public interface IRepositoryEntity
{
    string Identifier { get; }
}
```

### IRepository

```csharp
namespace Shiny.Support.Repositories;

public interface IRepository
{
    bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity;
    TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity;
    IList<TEntity> GetList<TEntity>(Expression<Func<TEntity, bool>>? expression = null) where TEntity : IRepositoryEntity;
    bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;
    bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity;
    void Clear<TEntity>() where TEntity : IRepositoryEntity;
    void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;
    void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity;
    IObservable<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)> WhenActionOccurs();
}
```

### RepositoryAction

```csharp
namespace Shiny.Support.Repositories;

public enum RepositoryAction
{
    Remove,
    Add,
    Update,
    Clear
}
```

### RepositoryException

```csharp
namespace Shiny.Support.Repositories;

public class RepositoryException : Exception
{
    public RepositoryException(string message);
}
```

### Repository Extension Methods

```csharp
namespace Shiny;

public static class RepositoryExtensions
{
    // Register default file-system backed repository (platform-only)
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services);

    // Remove by entity instance
    public static bool Remove<T>(this IRepository repository, T item) where T : IRepositoryEntity;

    // Create an observable count watcher for an entity type
    public static IObservable<int> CreateCountWatcher<T>(this IRepository repository) where T : IRepositoryEntity;
}
```

---

## Remote Configuration (Shiny.Extensions.Configuration)

### IRemoteConfigurationProvider

```csharp
namespace Shiny.Extensions.Configuration;

public interface IRemoteConfigurationProvider : IConfigurationProvider
{
    DateTimeOffset? LastLoaded { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
}
```

### RemoteConfig

```csharp
namespace Shiny.Extensions.Configuration;

public record RemoteConfig(
    string Uri,
    IConfiguration CurrentConfiguration,
    bool WaitForLoadOnStartup = false,
    string ConfigurationFilePath = "remotesettings.json"
);
```

### Configuration Builder Extensions

```csharp
namespace Microsoft.Extensions.Configuration;

public static partial class ConfigurationBuilderExtensions
{
    // Add JSON from platform bundle (iOS bundle / Android assets)
    public static IConfigurationBuilder AddJsonPlatformBundle(
        this IConfigurationBuilder builder,
        string? environment = null,
        bool optional = true,
        bool addPlatformSpecific = true
    );

    // Add platform-specific preferences (NSUserDefaults / SharedPreferences)
    public static IConfigurationBuilder AddPlatformPreferences(this IConfigurationBuilder builder);

    // Add remote configuration (platform shorthand)
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder,
        string configurationUri,
        string configurationFileName = "remotesettings.json",
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null
    );

    // Add remote configuration (full control)
    public static IConfigurationBuilder AddRemote(
        this IConfigurationBuilder builder,
        string configurationFilePath,
        string configurationUri,
        Func<RemoteConfig, CancellationToken, Task<object>>? getData = null,
        bool waitForRemoteLoad = true
    );

    // Get the remote provider from a built configuration
    public static IRemoteConfigurationProvider GetRemoteConfigurationProvider(
        this IConfigurationRoot configuration
    );
}
```

---

## Utility Types

### OperatingSystemShim

```csharp
namespace System;

public static class OperatingSystemShim
{
    public static bool IsAndroidVersionAtLeast(int apiLevel);
    public static bool IsAppleVersionAtleast(int osMajor, int osMinor = 0);
    public static bool IsIOSVersionAtLeast(int osMajor, int osMinor = 0);
    public static bool IsMacCatalystVersionAtLeast(int osMajor, int osMinor = 0);
}
```

### IAndroidForegroundServiceDelegate (Android)

```csharp
namespace Shiny;

public interface IAndroidForegroundServiceDelegate
{
    void Configure(NotificationCompat.Builder builder);
}
```

### AndroidPermissions Constants (Android)

```csharp
namespace Shiny;

public static class AndroidPermissions
{
    public const string ForegroundServiceLocation = "android.permission.FOREGROUND_SERVICE_LOCATION";
}
```

---

## Usage Examples

### Persisting Settings with INotifyPropertyChanged

```csharp
using Shiny;
using Shiny.Stores;

[ObjectStoreBinder("settings")]
public class AppSettings : NotifyPropertyChanged
{
    string _userName = string.Empty;
    public string UserName
    {
        get => this._userName;
        set => this.Set(ref this._userName, value);
    }

    bool _isDarkMode;
    public bool IsDarkMode
    {
        get => this._isDarkMode;
        set => this.Set(ref this._isDarkMode, value);
    }
}

// Registration in MauiProgram.cs:
builder.Services.AddShinyService<AppSettings>();

// Usage via DI:
public class MyViewModel
{
    public MyViewModel(AppSettings settings)
    {
        // Properties are automatically loaded from "settings" store
        var name = settings.UserName;

        // Setting a property automatically persists it
        settings.UserName = "Allan";
    }
}
```

### Implementing a Startup Task

```csharp
using Shiny;

public class AppInitTask : IShinyStartupTask
{
    readonly ILogger<AppInitTask> _logger;

    public AppInitTask(ILogger<AppInitTask> logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        _logger.LogInformation("App initialized");
    }
}

// Registration:
builder.Services.AddSingleton<IShinyStartupTask, AppInitTask>();
```

### Using ShinyLifecycleTask for Foreground/Background

```csharp
using Shiny;

public class MyLifecycleTask : ShinyLifecycleTask
{
    readonly ILogger<MyLifecycleTask> _logger;

    public MyLifecycleTask(ILogger<MyLifecycleTask> logger)
    {
        _logger = logger;
    }

    public override void Start()
    {
        _logger.LogInformation("Lifecycle task started");
    }

    protected override void OnStateChanged(bool backgrounding)
    {
        if (backgrounding)
            _logger.LogInformation("App went to background");
        else
            _logger.LogInformation("App came to foreground");
    }
}

// Registration:
builder.Services.AddShinyService<MyLifecycleTask>();
```

### Using IKeyValueStore Directly

```csharp
using Shiny.Stores;

public class CacheService
{
    readonly IKeyValueStore _store;

    public CacheService(IKeyValueStoreFactory factory)
    {
        _store = factory.GetStore("settings");
    }

    public void SaveToken(string token) => _store.Set("auth_token", token);
    public string? GetToken() => _store.Get<string>("auth_token");
    public void ClearToken() => _store.Remove("auth_token");

    public int GetNextId() => _store.IncrementValue("LastId");
}
```

### Monitoring Connectivity

```csharp
using Shiny;
using Shiny.Net;

public class NetworkAwareService
{
    readonly IConnectivity _connectivity;

    public NetworkAwareService(IConnectivity connectivity)
    {
        _connectivity = connectivity;
    }

    public void MonitorNetwork(CompositeDisposable disposer)
    {
        _connectivity
            .WhenInternetStatusChanged()
            .Subscribe(hasInternet =>
            {
                if (hasInternet)
                    Console.WriteLine("Internet available");
                else
                    Console.WriteLine("No internet");
            })
            .DisposedBy(disposer);
    }
}
```

### Using the Repository

```csharp
using Shiny.Support.Repositories;

public record TodoItem : IRepositoryEntity
{
    public string Identifier { get; init; } = Guid.NewGuid().ToString();
    public string Title { get; init; } = string.Empty;
    public bool IsComplete { get; init; }
}

public class TodoService
{
    readonly IRepository _repository;

    public TodoService(IRepository repository)
    {
        _repository = repository;
    }

    public void AddItem(TodoItem item) => _repository.Insert(item);
    public IList<TodoItem> GetPending() => _repository.GetList<TodoItem>(x => !x.IsComplete);
    public void Complete(string id)
    {
        var item = _repository.Get<TodoItem>(id);
        if (item != null)
            _repository.Set(item with { IsComplete = true });
    }

    public IObservable<int> WatchCount() => _repository.CreateCountWatcher<TodoItem>();
}
```

### Reactive Property Monitoring

```csharp
using Shiny;

public class SettingsWatcher
{
    public SettingsWatcher(AppSettings settings, CompositeDisposable disposer)
    {
        // Watch a specific property
        settings
            .WhenAnyProperty(x => x.IsDarkMode)
            .Subscribe(isDark => Console.WriteLine($"Dark mode: {isDark}"))
            .DisposedBy(disposer);

        // Watch all property changes
        settings
            .WhenAnyProperty()
            .Subscribe(change => Console.WriteLine($"{change.PropertyName} changed to {change.GetValue()}"))
            .DisposedBy(disposer);
    }
}
```

### Android Lifecycle Hook

```csharp
using Shiny.Hosting;
using Android.App;
using Android.Content;

public class DeepLinkHandler : IAndroidLifecycle.IOnActivityNewIntent
{
    public void Handle(Activity activity, Intent intent)
    {
        var data = intent?.DataString;
        if (data != null)
            Console.WriteLine($"Deep link received: {data}");
    }
}

// Registration:
builder.Services.AddSingleton<IAndroidLifecycle.IOnActivityNewIntent, DeepLinkHandler>();
```

### iOS Lifecycle Hook

```csharp
using Shiny.Hosting;
using Foundation;
using UIKit;

public class UniversalLinkHandler : IIosLifecycle.IContinueActivity
{
    public bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler)
    {
        if (activity.ActivityType == NSUserActivityType.BrowsingWeb)
        {
            var url = activity.WebPageUrl?.ToString();
            Console.WriteLine($"Universal link: {url}");
            return true;
        }
        return false;
    }
}

// Registration:
builder.Services.AddSingleton<IIosLifecycle.IContinueActivity, UniversalLinkHandler>();
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `InvalidOperationException: ServiceProvider is not initialized` | Ensure `UseShiny()` is called in MAUI setup, or `host.Run()` is called in native app startup before accessing `Host.Current`. |
| Properties not persisting on `NotifyPropertyChanged` objects | Use `AddShinyService<T>()` for registration, not `AddSingleton<T>()`. The auto-binding only works with `AddShinyService`. |
| `ArgumentException: No key/value store named X` | Verify the store alias matches a registered `IKeyValueStore`. Built-in aliases are `"settings"`, `"secure"`. |
| Android permissions always returning `Denied` | Ensure permissions are declared in `AndroidManifest.xml`. Use `IsInManifest()` to verify at runtime. |
| `TimeoutException` on Android permission request | A current activity must be available. Ensure `ShinyAndroidActivity` is used or `Host.Lifecycle.OnActivityOnCreate` is called. |
| iOS lifecycle hooks not firing | Ensure you inherit `ShinyAppDelegate` or call `UseShiny()` in MAUI. The `IosLifecycleExecutor` dispatches events to registered handlers. |
| `RepositoryException` on Insert | The entity `Identifier` already exists. Use `Set` for upsert behavior, or check `Exists` first. |
| Remote configuration not loading | Check that `AddRemote()` is called on the `IConfigurationBuilder` and the URI is accessible. Use `GetRemoteConfigurationProvider()` to force a reload with `LoadAsync()`. |
| `ObjectStoreBinder` skipping properties | Only properties with both public get and set accessors are bound. Ensure properties are not read-only. |
