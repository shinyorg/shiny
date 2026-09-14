using System.Collections.Generic;
using Shiny.BluetoothLE.Hosting.Bluez;
using Tmds.DBus.Protocol;
using Xunit;

namespace Shiny.BluetoothLE.Hosting.Linux.Tests;


public class BluezGattTests
{
    [Fact]
    public void Flags_MapReadWriteAndNotify()
    {
        var characteristic = new GattCharacteristic("2A37");
        characteristic.SetRead(_ => null!);
        characteristic.SetWrite(_ => null!, WriteOptions.Write);
        characteristic.SetNotification();

        var flags = BluezGatt.ToFlags(characteristic);

        Assert.Contains("read", flags);
        Assert.Contains("write", flags);
        Assert.Contains("notify", flags);
        Assert.DoesNotContain("indicate", flags);
    }


    [Fact]
    public void Flags_UseTheEncryptedFormsWhenRequired()
    {
        var characteristic = new GattCharacteristic("2A37");
        characteristic.SetRead(_ => null!, encrypted: true);
        characteristic.SetNotification(null, NotificationOptions.Indicate);

        var flags = BluezGatt.ToFlags(characteristic);

        Assert.Contains("encrypt-read", flags);
        Assert.DoesNotContain("read", flags);
        Assert.Contains("indicate", flags);
    }


    [Theory]
    [InlineData("/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF", "AA:BB:CC:DD:EE:FF")]
    [InlineData("/org/bluez/hci1/dev_01_02_03_04_05_06", "01:02:03:04:05:06")]
    [InlineData("/org/bluez/hci0", null)]
    [InlineData(null, null)]
    public void AddressFromDevicePath(string? path, string? expected)
        => Assert.Equal(expected, BluezGatt.AddressFromDevicePath(path));


    [Theory]
    [InlineData("180D", "0000180d-0000-1000-8000-00805f9b34fb")]
    [InlineData("6E400001-B5A3-F393-E0A9-E50E24DCCA9E", "6e400001-b5a3-f393-e0a9-e50e24dcca9e")]
    public void NormalizeUuid(string uuid, string expected)
        => Assert.Equal(expected, BluezGatt.NormalizeUuid(uuid));


    [Theory]
    [InlineData(GattState.ReadNotPermitted, "org.bluez.Error.NotPermitted")]
    [InlineData(GattState.WriteNotPermitted, "org.bluez.Error.NotPermitted")]
    [InlineData(GattState.InsufficientAuthentication, "org.bluez.Error.NotAuthorized")]
    [InlineData(GattState.RequestNotSupported, "org.bluez.Error.NotSupported")]
    [InlineData(GattState.InvalidOffset, "org.bluez.Error.InvalidOffset")]
    [InlineData(GattState.InvalidAttributeLength, "org.bluez.Error.InvalidValueLength")]
    public void ErrorNames(GattState state, string expected)
        => Assert.Equal(expected, BluezGatt.ToErrorName(state));


    [Fact]
    public void RequestOptions_ReadEveryKeyBluezSends()
    {
        var options = GattRequestOptions.From(new Dictionary<string, VariantValue>
        {
            ["offset"] = VariantValue.UInt16(12),
            ["mtu"] = VariantValue.UInt16(247),
            ["device"] = VariantValue.ObjectPath("/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF"),
            ["type"] = VariantValue.String("command")
        });

        Assert.Equal(12, options.Offset);
        Assert.Equal(247, options.Mtu);
        Assert.Equal("/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF", options.DevicePath);
        Assert.Equal("command", options.Type);
    }


    [Fact]
    public void RequestOptions_DefaultWhenBluezOmitsThem()
    {
        var options = GattRequestOptions.From(new Dictionary<string, VariantValue>());

        Assert.Equal(0, options.Offset);
        Assert.Equal(0, options.Mtu);
        Assert.Null(options.DevicePath);
        Assert.Null(options.Type);
    }
}
