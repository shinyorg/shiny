using Microsoft.Extensions.Logging;

namespace Shiny.Net.Vpn;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Saving VPN profile '{name}' for {server} ({authentication})"
    )]
    public static partial void SavingProfile(this ILogger logger, string name, string server, string authentication);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "The VPN profile was not saved - {result}"
    )]
    public static partial void ProfileNotSaved(this ILogger logger, AccessState result);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Removing the VPN profile"
    )]
    public static partial void RemovingProfile(this ILogger logger);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Starting the VPN tunnel"
    )]
    public static partial void Connecting(this ILogger logger);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Information,
        Message = "VPN connected"
    )]
    public static partial void Connected(this ILogger logger);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Information,
        Message = "VPN start requested - this platform cannot report whether it connected"
    )]
    public static partial void ConnectRequested(this ILogger logger);


    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Information,
        Message = "VPN stop requested"
    )]
    public static partial void Disconnected(this ILogger logger);


    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "The VPN had not reported down {wait} after the stop request - leaving the OS to finish"
    )]
    public static partial void DisconnectSlow(this ILogger logger, TimeSpan wait);


    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "VPN error: {message}"
    )]
    public static partial void VpnError(this ILogger logger, Exception exception, string message);


    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Debug,
        Message = "VPN watcher started ({watcher})"
    )]
    public static partial void WatcherStarted(this ILogger logger, string watcher);


    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Warning,
        Message = "The OS reported a VPN failure: {reason}"
    )]
    public static partial void PlatformFailure(this ILogger logger, string reason);
}
