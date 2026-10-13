using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AppKit;
using CoreFoundation;
using Foundation;

namespace Shiny;


public class MacPlatform : IPlatform
{
    public MacPlatform()
    {
        this.AppData = ToDirectory(NSSearchPathDirectory.LibraryDirectory);
        this.Public = ToDirectory(NSSearchPathDirectory.DocumentDirectory);
        this.Cache = ToDirectory(NSSearchPathDirectory.CachesDirectory);
        this.globalization = AppleSystem.CreateGlobalizationMonitor(this);
    }

    readonly GlobalizationMonitor globalization;


    static DirectoryInfo ToDirectory(NSSearchPathDirectory dir)
        => new DirectoryInfo(NSSearchPath.GetDirectories(dir, NSSearchPathDomain.User).First());


    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }
    public string? ApplicationIdentifier => NSBundle.MainBundle.BundleIdentifier;

    public Version AppVersion => AppleSystem.AppVersion;
    public string DeviceManufacturer => "Apple";
    public string DeviceModel => AppleSystem.GetSysctl("hw.model") ?? "Mac";
    public string Platform => "macOS";
    public DeviceFormFactor DeviceFormFactor => DeviceFormFactor.Desktop;

    public Version? PlatformVersion
    {
        get
        {
            var v = NSProcessInfo.ProcessInfo.OperatingSystemVersion;
            return new Version((int)v.Major, (int)v.Minor, (int)v.PatchVersion);
        }
    }

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

    // macOS has no per-app settings page - System Settings is the closest there is
    public void OpenAppSettings() => this.InvokeOnMainThread(() =>
        NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl("x-apple.systempreferences:"))
    );


    // the main dispatch queue, the same as Shiny.Maui.Shell's macOS MainThread - MAUI Essentials'
    // MainThread has no AppKit implementation, so both marshal through GCD directly
    public void InvokeOnMainThread(Action action)
    {
        if (NSThread.IsMain)
            action();
        else
            DispatchQueue.MainQueue.DispatchAsync(action);
    }
}
