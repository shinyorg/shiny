using Microsoft.Extensions.Logging;

namespace Shiny.Gamepad;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Gamepad connected - '{name}' ({kind}), id {id}"
    )]
    public static partial void GamepadConnected(this ILogger logger, string id, string name, GamepadKind kind);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Gamepad disconnected - '{name}', id {id}"
    )]
    public static partial void GamepadDisconnected(this ILogger logger, string id, string name);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "A gamepad input handler threw - gamepad {id}"
    )]
    public static partial void InputHandlerThrew(this ILogger logger, string id, Exception exception);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "A gamepad connection handler threw - gamepad {id}"
    )]
    public static partial void ConnectionHandlerThrew(this ILogger logger, string id, Exception exception);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Gamepad watch started - {count} controller(s) already connected"
    )]
    public static partial void WatchStarted(this ILogger logger, int count);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Reading gamepad {id} failed - the controller will be treated as disconnected"
    )]
    public static partial void ReadFailed(this ILogger logger, string id, Exception exception);


    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Setting vibration on gamepad {id} failed"
    )]
    public static partial void VibrationFailed(this ILogger logger, string id, Exception exception);


    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "Setting the light on gamepad {id} failed"
    )]
    public static partial void LightFailed(this ILogger logger, string id, Exception exception);


    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Motion sensors {state} on gamepad {id}"
    )]
    public static partial void MotionToggled(this ILogger logger, string id, string state);


    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Error,
        Message = "The gamepad poll loop failed - input has stopped"
    )]
    public static partial void PollLoopFailed(this ILogger logger, Exception exception);
}


internal static partial class PlatformLog
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Debug,
        Message = "Gamepad input is now routed through activity '{activity}'"
    )]
    public static partial void WindowCallbackInstalled(this ILogger logger, string activity);


    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "Could not hook the activity window - gamepad buttons and sticks will not be delivered"
    )]
    public static partial void WindowCallbackFailed(this ILogger logger, Exception exception);


    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Debug,
        Message = "Skipping input device {deviceId} - sources {sources} are not a gamepad"
    )]
    public static partial void DeviceSkipped(this ILogger logger, int deviceId, string sources);


    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Warning,
        Message = "The haptic engine for gamepad {id} stopped - {reason}"
    )]
    public static partial void HapticEngineStopped(this ILogger logger, string id, string reason);


    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Warning,
        Message = "Could not start the haptic engine for gamepad {id} - rumble is unavailable"
    )]
    public static partial void HapticEngineFailed(this ILogger logger, string id, Exception exception);
}
