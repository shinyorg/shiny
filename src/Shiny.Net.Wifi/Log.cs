using Microsoft.Extensions.Logging;

namespace Shiny.Net.Wifi;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Wi-Fi change watcher started ({watcher})"
    )]
    public static partial void WatcherStarted(this ILogger logger, string watcher);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Wi-Fi scan returned {count} access point(s)"
    )]
    public static partial void ScanCompleted(this ILogger logger, int count);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Joining Wi-Fi network '{ssid}'"
    )]
    public static partial void Connecting(this ILogger logger, string ssid);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Hotspot started - SSID '{ssid}'"
    )]
    public static partial void HotspotStarted(this ILogger logger, string ssid);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "Hotspot stopped"
    )]
    public static partial void HotspotStopped(this ILogger logger);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Wi-Fi error: {message}"
    )]
    public static partial void WifiError(this ILogger logger, Exception exception, string message);


    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "The OS throttled the scan request and served cached results instead"
    )]
    public static partial void ScanThrottled(this ILogger logger);


    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Wi-Fi radio set to {enabled}"
    )]
    public static partial void RadioToggled(this ILogger logger, bool enabled);


    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "The requested hotspot SSID/passphrase was ignored - this platform chooses its own"
    )]
    public static partial void HotspotConfigurationIgnored(this ILogger logger);


    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "Forgetting saved Wi-Fi network '{id}'"
    )]
    public static partial void Forgetting(this ILogger logger, string id);


    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Debug,
        Message = "Found {count} saved Wi-Fi network(s)"
    )]
    public static partial void KnownNetworksRead(this ILogger logger, int count);


    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Information,
        Message = "Suggested Wi-Fi network '{ssid}' to the OS - it joins once the user approves"
    )]
    public static partial void Suggested(this ILogger logger, string ssid);
}
