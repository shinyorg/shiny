using System;
using System.Globalization;
using System.IO;

namespace Shiny;


/// <summary>
/// Provides platform-specific operations and directory paths
/// </summary>
public interface IPlatform
{
    //string AppIdentifier { get; }

    /// <summary>
    /// Invokes an action on the platform main/UI thread
    /// </summary>
    /// <param name="action">The action to invoke</param>
    void InvokeOnMainThread(Action action);

    /// <summary>
    /// Gets the application data directory
    /// </summary>
    DirectoryInfo AppData { get; }

    /// <summary>
    /// Gets the cache directory
    /// </summary>
    DirectoryInfo Cache { get; }

    /// <summary>
    /// Gets the public/shared directory
    /// </summary>
    DirectoryInfo Public { get; }

    /// <summary>
    /// The app's version - the bundle short version on Apple, the package version name on Android,
    /// the package version (or the entry assembly's version when unpackaged) on Windows, and the entry
    /// assembly's version everywhere else
    /// </summary>
    Version AppVersion { get; }

    /// <summary>
    /// The device manufacturer, or "Unknown" where the platform does not say
    /// </summary>
    string DeviceManufacturer { get; }

    /// <summary>
    /// The device model - the hardware identifier on Apple (for example "iPhone16,1"), the
    /// <c>Build.MODEL</c> on Android, the system product name on Windows and Linux
    /// </summary>
    string DeviceModel { get; }

    /// <summary>
    /// The operating system version, or null where it cannot be read
    /// </summary>
    Version? PlatformVersion { get; }

    /// <summary>
    /// The operating system the app is running on - Android, iOS, MacCatalyst, tvOS, macOS, Windows,
    /// Linux or Browser
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// The kind of device the app is running on
    /// </summary>
    DeviceIdiom DeviceIdiom { get; }

    /// <summary>
    /// The current culture
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Raised when the user changes the device language or region. The native listener starts with the
    /// first subscriber and stops with the last.
    /// </summary>
    event EventHandler<CultureInfo>? CultureChanged;

    /// <summary>
    /// The current local time zone
    /// </summary>
    TimeZoneInfo CurrentTimeZone { get; }

    /// <summary>
    /// Raised when the device time zone changes. The native listener starts with the first subscriber
    /// and stops with the last.
    /// </summary>
    event EventHandler<TimeZoneInfo>? TimeZoneChanged;

    /// <summary>
    /// Opens the operating system's settings for this app where the platform has such a page (Android,
    /// iOS, Mac Catalyst, packaged Windows apps), otherwise the system settings
    /// </summary>
    void OpenAppSettings();
}
