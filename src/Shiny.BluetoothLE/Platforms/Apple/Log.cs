using System;
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
    public static partial void CharacteristicNotifyState(this ILogger logger, CBUUID serviceUuid, CBUUID characteristicUuid, bool notifying);
    
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "CanSendWriteWithoutResponse: {ready}"
    )]
    public static partial void CanSendWriteWithoutResponse(this ILogger logger, bool ready);

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
        EventId = 5,
        Level = LogLevel.Information,
        Message = "{message} - {serviceUuid} / {characteristicUuid}"
    )]
    public static partial void CharacteristicInfo(this ILogger logger, string message, string serviceUuid, string characteristicUuid);
}