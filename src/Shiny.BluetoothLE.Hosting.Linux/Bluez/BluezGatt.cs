using System;
using System.Collections.Generic;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting.Bluez;


/// <summary>
/// Translations between Shiny's GATT model and the one BlueZ's D-Bus API speaks.
/// </summary>
internal static class BluezGatt
{
    /// <summary>
    /// BlueZ wants 128-bit UUIDs in the long lowercase form; callers routinely pass the 16-bit
    /// short form that every other platform accepts.
    /// </summary>
    public static string NormalizeUuid(string uuid)
        => (uuid.Length == 4 ? $"0000{uuid}-0000-1000-8000-00805F9B34FB" : uuid).ToLowerInvariant();


    /// <summary>
    /// The <c>Flags</c> property of an exported <c>GattCharacteristic1</c>.
    /// </summary>
    public static string[] ToFlags(GattCharacteristic characteristic)
    {
        var properties = characteristic.NativeProperties;
        var flags = new List<string>();

        if (properties.HasFlag(CharacteristicProperties.Broadcast))
            flags.Add("broadcast");

        if (properties.HasFlag(CharacteristicProperties.Read))
            flags.Add(characteristic.ReadEncrypted ? "encrypt-read" : "read");

        if (properties.HasFlag(CharacteristicProperties.WriteWithoutResponse))
            flags.Add("write-without-response");

        if (properties.HasFlag(CharacteristicProperties.Write))
            flags.Add(characteristic.WriteOptions.HasFlag(WriteOptions.EncryptionRequired) ? "encrypt-write" : "write");

        if (properties.HasFlag(CharacteristicProperties.AuthenticatedSignedWrites))
            flags.Add("authenticated-signed-writes");

        var encryptedNotify = characteristic.NotifyOptions.HasFlag(NotificationOptions.EncryptionRequired);
        if (properties.HasFlag(CharacteristicProperties.Notify))
            flags.Add(encryptedNotify ? "encrypt-notify" : "notify");

        if (properties.HasFlag(CharacteristicProperties.Indicate))
            flags.Add(encryptedNotify ? "encrypt-indicate" : "indicate");

        return flags.ToArray();
    }


    /// <summary>
    /// BlueZ names device objects <c>/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF</c> - the address is the last segment.
    /// </summary>
    public static string? AddressFromDevicePath(string? devicePath)
    {
        if (devicePath == null)
            return null;

        var index = devicePath.LastIndexOf("/dev_", StringComparison.Ordinal);
        if (index < 0)
            return null;

        var address = devicePath.Substring(index + 5);
        return address.Length == 17 ? address.Replace('_', ':') : null;
    }


    /// <summary>
    /// The BlueZ error a GATT status is reported as - bluetoothd maps these back onto ATT error codes.
    /// </summary>
    public static string ToErrorName(GattState state) => state switch
    {
        GattState.ReadNotPermitted => BluezConstants.ErrorNotPermitted,
        GattState.WriteNotPermitted => BluezConstants.ErrorNotPermitted,
        GattState.InsufficientAuthentication => BluezConstants.ErrorNotAuthorized,
        GattState.RequestNotSupported => BluezConstants.ErrorNotSupported,
        GattState.InvalidOffset => BluezConstants.ErrorInvalidOffset,
        GattState.InvalidAttributeLength => BluezConstants.ErrorInvalidValueLength,
        _ => BluezConstants.ErrorFailed
    };
}


/// <summary>
/// The options dictionary BlueZ passes to <c>ReadValue</c> and <c>WriteValue</c>.
/// </summary>
/// <param name="Offset">The attribute offset for a long read or prepared write.</param>
/// <param name="Mtu">The negotiated ATT MTU (zero when BlueZ did not say).</param>
/// <param name="DevicePath">The requesting central's device object (null on BlueZ versions that do not send it).</param>
/// <param name="Type">For a write: <c>command</c> (without response), <c>request</c> or <c>reliable</c>.</param>
internal readonly record struct GattRequestOptions(ushort Offset, ushort Mtu, string? DevicePath, string? Type)
{
    public static GattRequestOptions From(Dictionary<string, VariantValue> options) => new(
        options.TryGetValue("offset", out var offset) ? offset.GetUInt16() : (ushort)0,
        options.TryGetValue("mtu", out var mtu) ? mtu.GetUInt16() : (ushort)0,
        options.TryGetValue("device", out var device) ? device.GetObjectPathAsString() : null,
        options.TryGetValue("type", out var type) ? type.GetString() : null
    );
}
