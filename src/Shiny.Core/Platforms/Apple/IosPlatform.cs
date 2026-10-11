using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CoreFoundation;
using Foundation;
using UIKit;

namespace Shiny;


public class IosPlatform : IPlatform
{
    public IosPlatform()
    {
        this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
        this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
        this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
        this.globalization = AppleSystem.CreateGlobalizationMonitor(this);
    }

    readonly GlobalizationMonitor globalization;

    static DirectoryInfo ToDirectory(NSSearchPathDirectory dir) => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());
    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }
    public string AppIdentifier => NSBundle.MainBundle.BundleIdentifier;

    public Version AppVersion => AppleSystem.AppVersion;
    public string DeviceManufacturer => "Apple";
    public Version? PlatformVersion => PlatformVersionParser.Parse(UIDevice.CurrentDevice.SystemVersion);

#if MACCATALYST
    public string Platform => "MacCatalyst";
    public DeviceIdiom DeviceIdiom => DeviceIdiom.Desktop;

    // hw.machine is the CPU architecture on a Mac - hw.model is the machine ("Mac15,3")
    public string DeviceModel => AppleSystem.GetSysctl("hw.model") ?? UIDevice.CurrentDevice.Model;
#else
#if TVOS
    public string Platform => "tvOS";
#else
    public string Platform => "iOS";
#endif

    // the simulator reports the host Mac's hw.machine, so it publishes what it is simulating separately
    public string DeviceModel
        => Environment.GetEnvironmentVariable("SIMULATOR_MODEL_IDENTIFIER")
           ?? AppleSystem.GetSysctl("hw.machine")
           ?? UIDevice.CurrentDevice.Model;

    public DeviceIdiom DeviceIdiom => UIDevice.CurrentDevice.UserInterfaceIdiom switch
    {
        UIUserInterfaceIdiom.Phone => DeviceIdiom.Phone,
        UIUserInterfaceIdiom.Pad => DeviceIdiom.Tablet,
        UIUserInterfaceIdiom.TV => DeviceIdiom.TV,
        UIUserInterfaceIdiom.CarPlay => DeviceIdiom.Car,
        UIUserInterfaceIdiom.Mac => DeviceIdiom.Desktop,
        _ => DeviceIdiom.Unknown
    };
#endif

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

    public void OpenAppSettings() => this.InvokeOnMainThread(() =>
        UIApplication.SharedApplication.OpenUrl(
            new NSUrl(UIApplication.OpenSettingsUrlString),
            new UIApplicationOpenUrlOptions(),
            null
        )
    );


    // the main dispatch queue, the same as MacPlatform and Shiny.Maui.Shell's macOS MainThread
    public void InvokeOnMainThread(Action action)
    {
        if (NSThread.IsMain)
            action();
        else
            DispatchQueue.MainQueue.DispatchAsync(action);
    }
}
