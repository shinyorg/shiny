using Shiny.Bluetooth.Infrastructure;
using Xunit;

namespace Shiny.Bluetooth.Tests;


public class BluetoothDeviceTests
{
    [Fact]
    public void Address_IsNormalized()
        => Assert.Equal("AA:BB:CC:DD:EE:FF", new BluetoothDevice(new FakeOperations(), "aa-bb-cc-dd-ee-ff").Address);


    [Fact]
    public void Equality_IsByAddressOnly()
    {
        var ops = new FakeOperations();
        IBluetoothDevice discovered = new BluetoothDevice(ops, "AA:BB:CC:DD:EE:FF") { Name = "Printer" };
        IBluetoothDevice paired = new BluetoothDevice(ops, "aa:bb:cc:dd:ee:ff") { Paired = true };

        Assert.Equal(discovered, paired);
        Assert.Equal(discovered.GetHashCode(), paired.GetHashCode());
        Assert.Contains(paired, new List<IBluetoothDevice> { discovered });
        Assert.NotEqual(discovered, new BluetoothDevice(ops, "AA:BB:CC:DD:EE:00"));
    }


    [Theory]
    [InlineData(0x5A020Cu, BluetoothMajorDeviceClass.Phone)]      // smartphone
    [InlineData(0x240404u, BluetoothMajorDeviceClass.AudioVideo)] // headset
    [InlineData(0x001F00u, BluetoothMajorDeviceClass.Uncategorized)]
    [InlineData(0x000680u, BluetoothMajorDeviceClass.Imaging)]    // printer
    public void MajorDeviceClass_DecodesBits8To12(uint cod, BluetoothMajorDeviceClass expected)
        => Assert.Equal(expected, new BluetoothDevice(new FakeOperations(), "AA:BB:CC:DD:EE:FF") { ClassOfDevice = cod }.MajorDeviceClass);


    [Fact]
    public void MajorDeviceClass_NullWithoutClassOfDevice()
        => Assert.Null(new BluetoothDevice(new FakeOperations(), "AA:BB:CC:DD:EE:FF").MajorDeviceClass);


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pair_DelegatesToTheManager_AndTracksTheResult(bool result)
    {
        var ops = new FakeOperations { PairResult = result };
        var device = new BluetoothDevice(ops, "AA:BB:CC:DD:EE:FF");

        Assert.Equal(result, await device.Pair());
        Assert.Same(device, ops.Paired);
        Assert.Equal(result, device.IsPaired);
    }


    [Fact]
    public async Task Connect_DelegatesToTheManager()
    {
        var ops = new FakeOperations();
        var device = new BluetoothDevice(ops, "AA:BB:CC:DD:EE:FF");

        await using var connection = await device.Connect(BluetoothServiceUuids.SerialPort, secure: false);
        Assert.Same(device, connection.Device);
        Assert.Equal((BluetoothServiceUuids.SerialPort, false), ops.Connected);
    }


    [Fact]
    public void ServiceUuids_ExpandOnTheBaseUuid()
    {
        Assert.Equal(Guid.Parse("00001101-0000-1000-8000-00805F9B34FB"), BluetoothServiceUuids.SerialPort);
        Assert.Equal(Guid.Parse("00001105-0000-1000-8000-00805F9B34FB"), BluetoothServiceUuids.ObexObjectPush);
    }
}


class FakeOperations : IBluetoothDeviceOperations
{
    public bool PairResult { get; init; } = true;
    public BluetoothDevice? Paired { get; private set; }
    public (Guid, bool)? Connected { get; private set; }


    public Task<bool> Pair(BluetoothDevice device, CancellationToken ct)
    {
        this.Paired = device;
        return Task.FromResult(this.PairResult);
    }


    public Task<BluetoothConnection> Connect(BluetoothDevice device, Guid serviceUuid, bool secure, CancellationToken ct)
    {
        this.Connected = (serviceUuid, secure);
        return Task.FromResult(new BluetoothConnection(device, serviceUuid, new MemoryStream()));
    }
}
