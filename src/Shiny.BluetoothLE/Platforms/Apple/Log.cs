using System;
using System.Runtime.CompilerServices;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


internal static partial class Log
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Critical,
        Message = "{permission} needs to be set - you will likely experience a native crash after this log"
    )] 
    public static partial void MissingIosPermission(this ILogger logger, string permission);


    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "UpdatedNotificationState: {serviceUuid}/{characteristicUuid} - ENABLED: {notifying}"
    )]
    static partial void CharacteristicNotifyState(this ILogger logger, CBUUID serviceUuid, CBUUID characteristicUuid, bool notifying);

    public static void CharacteristicNotifyState(this ILogger logger, CBCharacteristic native)
        => logger.CharacteristicNotifyState(native.Service?.UUID, native.UUID, native.IsNotifying);


    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "CanSendWriteWithoutResponse: {serviceUuid}/{characteristicUuid} - ready: {ready}"
    )]
    static partial void CanSendWriteWithoutResponse(this ILogger logger, CBUUID serviceUuid, CBUUID characteristicUuid, bool ready);

    public static void CanSendWriteWithoutResponse(this ILogger logger, CBCharacteristic native, bool ready)
        => logger.CanSendWriteWithoutResponse(native.Service.UUID, native.UUID, ready);


    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Peripheral: {peripheralIdentifier} - Service Count: {serviceCount}"
    )]
    public static partial void ServiceDiscoveryEvent(this ILogger logger, NSUuid peripheralIdentifier, int serviceCount);


    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Unable to cleanly dispose of characteristic notification - {serviceUuid} / {characteristicUuid}"
    )]
    public static partial void DisableNotificationError(this ILogger logger, Exception exception, string serviceUuid, string characteristicUuid);


    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "{message} - {serviceUuid} / {characteristicUuid}"
    )]
    public static partial void CharacteristicInfo(this ILogger logger, string message, string serviceUuid, string characteristicUuid);


    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "[{methodName}] - {serviceUuid} / {characteristicUuid} - ERROR: {error}"
    )]
    static partial void CharacteristicEvent(this ILogger logger, CBUUID? serviceUuid, CBUUID? characteristicUuid, string? error, [CallerMemberName] string? methodName = null);

    public static void CharacteristicEvent(this ILogger logger, CBCharacteristic native, NSError? error, [CallerMemberName] string? methodName = null)
        => logger.CharacteristicEvent(native.Service?.UUID, native.UUID, error?.LocalizedDescription, methodName);


    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Manager State Change: {state} "
    )]
#if XAMARINIOS
    public static partial void ManagerStateChange(this ILogger logger, CBCentralManagerState state);
#else
    public static partial void ManagerStateChange(this ILogger logger, CBManagerState state);
#endif


    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Peripheral State Change: {peripheralIdentifier} - Connected: {connected} - Error: {error} "
    )]
    public static partial void PeripheralStateChange(this ILogger logger, NSUuid peripheralIdentifier, bool connected, string error);

}