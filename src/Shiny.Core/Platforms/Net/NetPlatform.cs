using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace Shiny;


/// <summary>
/// The <see cref="IPlatform"/> for hosts that are just .NET - a GTK desktop app on Linux, a console
/// app, a service - where there is no platform SDK to ask for the directories.
/// </summary>
/// <remarks>
/// <para>
/// It exists because the base target framework had no <see cref="IPlatform"/> at all, which meant
/// anything built on Shiny.Core could not be compiled for it: a Linux desktop head cannot ask for a
/// net10.0-linux build, because there is no such target framework, so whatever it gets has to come
/// from the plain net10.0 one.
/// </para>
/// <para>
/// Modelled on <c>WindowsPlatform</c>'s unpackaged branch rather than on anything new, and that is
/// deliberate - the question "where does an app with no container put its files" has the same
/// answer on both, and two answers to it would be two layouts to support.
/// </para>
/// </remarks>
public partial class NetPlatform : IPlatform
{
    public NetPlatform()
    {
        // LocalApplicationData is already the XDG answer on Linux: .NET resolves it to
        // $XDG_DATA_HOME, falling back to ~/.local/share. So an app folder underneath it is
        // XDG-correct without this class having to know what XDG is.
        //
        // Named after the entry assembly, because everything sharing this root would otherwise
        // write straight into a directory the whole desktop shares.
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDomain.CurrentDomain.FriendlyName
        );
        Directory.CreateDirectory(path);

        this.globalization = new GlobalizationMonitor(this, GlobalizationMonitor.Poll, WatchTimeZone);
        this.AppData = new DirectoryInfo(path);
        this.Cache = new DirectoryInfo(Path.Combine(path, "Cache"));
        this.Public = new DirectoryInfo(Path.Combine(path, "Public"));
    }


    public DirectoryInfo AppData { get; }

    public DirectoryInfo Cache { get; }

    public DirectoryInfo Public { get; }


    /// <summary>
    /// Set by the hosting layer at app launch so that <see cref="InvokeOnMainThread"/> can marshal
    /// onto whatever that host calls its main thread - the GTK main loop, for instance - without
    /// Shiny.Core taking a dependency on a UI toolkit it otherwise knows nothing about.
    /// </summary>
    /// <remarks>
    /// The same hook, spelled the same way, as <c>WindowsPlatform.MainThreadHandler</c>. A host that
    /// does not set it is not broken: plenty of things built on this have no main thread to marshal
    /// to, which is why the fallback below runs the action rather than throwing.
    /// </remarks>
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


    readonly GlobalizationMonitor globalization;

    // DMI is the BIOS's own answer on PCs and most VMs; a Raspberry Pi has no DMI and names itself in
    // the device tree instead. Neither exists off Linux.
    const string DmiVendor = "/sys/devices/virtual/dmi/id/sys_vendor";
    const string DmiProduct = "/sys/devices/virtual/dmi/id/product_name";
    const string DeviceTreeModel = "/proc/device-tree/model";


    public Version AppVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0);

    public string? ApplicationIdentifier
    {
        get
        {
            if (!OperatingSystem.IsLinux())
                return null;

            var id = Environment.GetEnvironmentVariable("FLATPAK_ID");
            if (String.IsNullOrWhiteSpace(id))
                id = Environment.GetEnvironmentVariable("SNAP_NAME");

            return String.IsNullOrWhiteSpace(id) ? null : id;
        }
    }

    public string DeviceManufacturer => OperatingSystem.IsLinux()
        ? ReadFirstLine(DmiVendor) ?? "Unknown"
        : "Unknown";

    public string DeviceModel => OperatingSystem.IsLinux()
        ? ReadFirstLine(DmiProduct) ?? ReadFirstLine(DeviceTreeModel) ?? "Unknown"
        : "Unknown";

    public Version? PlatformVersion => OperatingSystem.IsBrowser() ? null : Environment.OSVersion.Version;

    public string Platform
    {
        get
        {
            if (OperatingSystem.IsBrowser())
                return "Browser";

            if (OperatingSystem.IsLinux())
                return "Linux";

            if (OperatingSystem.IsWindows())
                return "Windows";

            if (OperatingSystem.IsMacOS())
                return "macOS";

            return "Unknown";
        }
    }

    public DeviceFormFactor DeviceFormFactor => OperatingSystem.IsBrowser() ? DeviceFormFactor.Unknown : DeviceFormFactor.Desktop;


    readonly Lazy<string?> userAgent = new(ReadUserAgent);

    /// <summary>
    /// The browser's <c>navigator.userAgent</c> on Blazor WebAssembly, null everywhere else (including
    /// Blazor Server, whose code runs on the server). Read once - it is fixed for the page's lifetime.
    /// </summary>
    public string? UserAgent => this.userAgent.Value;

    /// <summary>
    /// A best-effort browser version parsed from <see cref="UserAgent"/>, or null when it cannot be read.
    /// </summary>
    public Version? UserAgentVersion => ParseBrowserVersion(this.UserAgent);

    /// <remarks>
    /// .NET reads the culture from LANG/LC_ALL (or the browser language) at startup, and a running process
    /// never sees those change - a Linux locale switch takes effect at the next login. The change event is
    /// a slow poll so a culture the app sets itself still raises it.
    /// </remarks>
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


    public void OpenAppSettings()
    {
        if (OperatingSystem.IsBrowser())
            return;

        if (OperatingSystem.IsWindows())
        {
            TryStart(new ProcessStartInfo("ms-settings:") { UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            TryStart(new ProcessStartInfo("open", "x-apple.systempreferences:"));
        }
        else if (OperatingSystem.IsLinux())
        {
            // no freedesktop standard for "open settings" - try GNOME's, then KDE Plasma's
            if (!TryStart(new ProcessStartInfo("gnome-control-center")))
                TryStart(new ProcessStartInfo("systemsettings"));
        }
    }


    static bool TryStart(ProcessStartInfo info)
    {
        try
        {
            Process.Start(info)?.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }


    static string? ReadFirstLine(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            // device-tree strings are NUL terminated
            var value = File.ReadAllText(path).Trim().TrimEnd('\0').Trim();
            return value.Length == 0 ? null : value;
        }
        catch
        {
            return null;
        }
    }


    static IDisposable WatchTimeZone(Action refresh)
    {
        if (!OperatingSystem.IsLinux())
            return GlobalizationMonitor.Poll(refresh);

        // Unlike the locale, the time zone does change under a running process: systemd-timedated replaces
        // the /etc/localtime symlink. Watch the directory, because the symlink is replaced rather than edited.
        FileSystemWatcher? watcher = null;
        try
        {
            watcher = new FileSystemWatcher("/etc", "localtime")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };
            FileSystemEventHandler handler = (_, _) => refresh();
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += (_, _) => refresh();
            watcher.EnableRaisingEvents = true;
            return watcher;
        }
        catch
        {
            // no inotify watches left, or /etc unreadable (a tight Flatpak sandbox) - poll instead
            watcher?.Dispose();
            return GlobalizationMonitor.Poll(refresh);
        }
    }


    static string? ReadUserAgent()
    {
        if (!OperatingSystem.IsBrowser())
            return null;

        try
        {
            return ReadBrowserUserAgent();
        }
        catch
        {
            // the JS interop runtime is not up (a worker without it, a unit test host) - no user agent
            return null;
        }
    }


    // straight off globalThis through the runtime's own JS interop - no script file and no IJSRuntime,
    // which this class (registered with no dependencies) could not get anyway
    [SupportedOSPlatform("browser")]
    static string? ReadBrowserUserAgent()
    {
        using var navigator = JSHost.GlobalThis.GetPropertyAsJSObject("navigator");
        return navigator?.GetPropertyAsString("userAgent");
    }


    // Tried in specificity order, because the more specific browsers also carry the less specific
    // tokens - Edge and Opera both include "Chrome/", and Safari reports its own version under "Version/"
    internal static Version? ParseBrowserVersion(string? userAgent)
    {
        if (String.IsNullOrWhiteSpace(userAgent))
            return null;

        foreach (var regex in new[] { EdgeRegex(), OperaRegex(), FirefoxRegex(), ChromeRegex(), SafariRegex() })
        {
            var match = regex.Match(userAgent);
            if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
                return version;
        }
        return null;
    }

    [GeneratedRegex(@"Edg(?:[A-Z]+)?/(\d+(?:\.\d+){1,3})")]
    private static partial Regex EdgeRegex();

    [GeneratedRegex(@"OPR/(\d+(?:\.\d+){1,3})")]
    private static partial Regex OperaRegex();

    [GeneratedRegex(@"Firefox/(\d+(?:\.\d+){1,3})")]
    private static partial Regex FirefoxRegex();

    [GeneratedRegex(@"Chrome/(\d+(?:\.\d+){1,3})")]
    private static partial Regex ChromeRegex();

    [GeneratedRegex(@"Version/(\d+(?:\.\d+){1,3})")]
    private static partial Regex SafariRegex();
}
