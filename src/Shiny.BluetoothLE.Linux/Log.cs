using System;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "BlueZ adapter state: Powered={powered}, Discovering={discovering}"
    )]
    public static partial void AdapterState(this ILogger logger, bool powered, bool discovering);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "BlueZ device discovered: {devicePath}"
    )]
    public static partial void DeviceDiscovered(this ILogger logger, string devicePath);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "BlueZ device connection state changed: {devicePath} Connected={connected}"
    )]
    public static partial void DeviceConnectionChanged(this ILogger logger, string devicePath, bool connected);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "BlueZ error: {message}"
    )]
    public static partial void BluezError(this ILogger logger, Exception exception, string message);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "BlueZ scan started"
    )]
    public static partial void ScanStarted(this ILogger logger);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "BlueZ scan stopped"
    )]
    public static partial void ScanStopped(this ILogger logger);
}
