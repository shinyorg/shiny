using System;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Error disabling notification {serviceUuid} / {characteristicUuid}"
    )] 
    public static partial void DisableNotificationError(this ILogger logger, Exception exception, string serviceUuid, string characteristicUuid);
    
    
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "{state} to Notification Characteristic {serviceUuid} / {characteristicUuid}"
    )] 
    public static partial void HookedCharacteristic(this ILogger logger, string serviceUuid, string characteristicUuid, string state);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "GATT Connection State Change: {status} - {newState}"
    )]
    public static partial void ConnectionStateChange(this ILogger logger, GattStatus status, ProfileState newState);
}