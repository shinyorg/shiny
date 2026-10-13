using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.System.Profile;

namespace Shiny;


public class WindowsPlatform : IPlatform
{
    public WindowsPlatform()
    {
        string path;
        try
        {
            path = ApplicationData.Current.LocalFolder.Path;
            this.isPackaged = true;
        }
        catch (InvalidOperationException)
        {
            // Unpackaged WinUI app - no package identity, so ApplicationData.Current is unavailable.
            var appName = AppDomain.CurrentDomain.FriendlyName;
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName
            );
            Directory.CreateDirectory(path);
        }
        this.AppData = new DirectoryInfo(path);
        this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
        this.Public = new DirectoryInfo(Path.Combine(path, "Public"));

        this.globalization = new GlobalizationMonitor(this, WatchCulture, WatchTimeZone);
    }

    readonly bool isPackaged;
    readonly GlobalizationMonitor globalization;
    readonly Lazy<EasClientDeviceInformation> deviceInfo = new(() => new EasClientDeviceInformation());


    public DirectoryInfo AppData { get; }

    public DirectoryInfo Cache { get; }

    public DirectoryInfo Public { get; }

    /// <summary>
    /// Set by the hosting layer (e.g. Shiny.Hosting.Maui) at app launch so that
    /// <see cref="InvokeOnMainThread"/> can marshal to the WinUI 3 dispatcher without
    /// Shiny.Core taking a dependency on Microsoft.UI / WindowsAppSDK / MAUI.
    /// </summary>
    public static Action<Action>? MainThreadHandler { get; set; }

    public void InvokeOnMainThread(Action action)
    {
        var handler = MainThreadHandler;
        if (handler == null)
        {
            // No hosting layer wired this up - best-effort inline execution.
            action();
            return;
        }
        handler(action);
    }


    public Version AppVersion
    {
        get
        {
            if (this.isPackaged)
            {
                var v = Windows.ApplicationModel.Package.Current.Id.Version;
                return new Version(v.Major, v.Minor, v.Build, v.Revision);
            }
            // unpackaged - no package identity to ask, so the head project's own version
            return Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0);
        }
    }

    // unpackaged - no package identity, and the entry assembly name is not a store identifier
    public string? ApplicationIdentifier => this.isPackaged
        ? Windows.ApplicationModel.Package.Current.Id.Name
        : null;

    public string DeviceManufacturer => this.Read(x => x.SystemManufacturer);
    public string DeviceModel => this.Read(x => x.SystemProductName);
    public Version? PlatformVersion => Environment.OSVersion.Version;
    public string Platform => "Windows";

    public DeviceFormFactor DeviceFormFactor => AnalyticsInfo.VersionInfo.DeviceFamily switch
    {
        "Windows.Desktop" => DeviceFormFactor.Desktop,
        "Windows.Mobile" => DeviceFormFactor.Phone,
        "Windows.Xbox" => DeviceFormFactor.TV,
        _ => DeviceFormFactor.Unknown
    };

    public CultureInfo CurrentCulture => this.globalization.CurrentCulture;
    public event EventHandler<CultureInfo>? CultureChanged
    {
        add => this.globalization.AddCultureHandler(value);
        remove => this.globalization.RemoveCultureHandler(value);
    }

    public TimeZoneInfo CurrentTimeZone => this.globalization.CurrentTimeZone;
    public event EventHandler<TimeZoneInfo>? TimeZoneChanged
    {
        add => this.globalization.AddTimeZoneHandler(value);
        remove => this.globalization.RemoveTimeZoneHandler(value);
    }

    // a packaged app has its own page under Apps; an unpackaged one is not listed there
    public void OpenAppSettings() => _ = Windows.System.Launcher.LaunchUriAsync(
        new Uri(this.isPackaged ? "ms-settings:appsfeatures-app" : "ms-settings:appsfeatures")
    );


    string Read(Func<EasClientDeviceInformation, string?> getter)
    {
        try
        {
            var value = getter(this.deviceInfo.Value);
            return String.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }
        catch
        {
            return "Unknown";
        }
    }


    static IDisposable WatchCulture(Action refresh)
    {
        UserPreferenceChangedEventHandler handler = (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.Locale)
                refresh();
        };
        SystemEvents.UserPreferenceChanged += handler;
        return new ActionDisposable(() => SystemEvents.UserPreferenceChanged -= handler);
    }


    static IDisposable WatchTimeZone(Action refresh)
    {
        // TimeChanged fires for a clock or time zone change - the monitor compares and only raises for a new zone
        EventHandler handler = (_, _) => refresh();
        SystemEvents.TimeChanged += handler;
        return new ActionDisposable(() => SystemEvents.TimeChanged -= handler);
    }
}
