using System;
using System.Runtime.CompilerServices;
using Android.Bluetooth;
using Java.Util;
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


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "[Native Method - {methodName}] Service: {serviceUuid} - Characteristic: {characteristicUuid} - Status: {status}"
    )]
    public static partial void CharacteristicEvent(this ILogger logger, UUID? serviceUuid, UUID? characteristicUuid, GattStatus? status, [CallerMemberName] string? methodName = null);


    public static void CharacteristicEvent(this ILogger logger, BluetoothGattCharacteristic? native, GattStatus? status, [CallerMemberName] string? methodName = null)
        => logger.CharacteristicEvent(native?.Service?.Uuid, native?.Uuid, status, methodName);
}