using System.Reactive;
using System.Reactive.Linq;
using Shiny.BluetoothLE;
using Shiny.Printers.BluetoothLE;

namespace Shiny.Printers.Tests;


public class BlePrinterConnectionTests
{
    static BlePrinterConfig Config(int? chunkSize = null) => new()
    {
        ServiceUuid = "0000ff00-0000-1000-8000-00805f9b34fb",
        WriteCharacteristicUuid = "0000ff02-0000-1000-8000-00805f9b34fb",
        Capabilities = PrinterCapabilities.Paper58mm,
        ChunkSize = chunkSize,
        InterChunkDelay = TimeSpan.Zero
    };


    [Fact]
    public async Task DefaultChunkSize_IsTheUsableMtu()
    {
        // IPeripheral.Mtu is already the payload size - the ATT header must not be subtracted again
        var peripheral = new WritePeripheral { Mtu = 100 };
        var connection = new BlePrinterConnection(peripheral, Config());

        await connection.SendAsync(new byte[250]);

        Assert.Equal([100, 100, 50], peripheral.Writes.Select(x => x.Length));
    }


    [Fact]
    public async Task DefaultChunkSize_IsCappedAt512()
    {
        var peripheral = new WritePeripheral { Mtu = 514 };
        var connection = new BlePrinterConnection(peripheral, Config());

        await connection.SendAsync(new byte[600]);

        Assert.Equal([512, 88], peripheral.Writes.Select(x => x.Length));
    }


    [Fact]
    public async Task ExplicitChunkSize_Wins()
    {
        var peripheral = new WritePeripheral { Mtu = 244 };
        var connection = new BlePrinterConnection(peripheral, Config(chunkSize: 20));

        await connection.SendAsync(new byte[50]);

        Assert.Equal([20, 20, 10], peripheral.Writes.Select(x => x.Length));
    }


    sealed class WritePeripheral : IPeripheral
    {
        static readonly BleCharacteristicInfo Info = new(new BleServiceInfo("ff00"), "ff02", false, CharacteristicProperties.WriteWithoutResponse);

        public List<byte[]> Writes { get; } = new();
        public string Uuid => "printer";
        public string? Name => "printer";
        public int Mtu { get; set; } = 20;
        public ConnectionState Status => ConnectionState.Connected;

        public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true)
        {
            this.Writes.Add(data);
            return Observable.Return(new BleCharacteristicResult(Info, BleCharacteristicEvent.WriteWithoutResponse, data));
        }

        public void Connect(ConnectionConfig? config) { }
        public void CancelConnection() { }
        public IObservable<ConnectionState> WhenStatusChanged() => Observable.Return(ConnectionState.Connected);
        public IObservable<BleException> WhenConnectionFailed() => Observable.Never<BleException>();
        public IObservable<Unit> WhenServicesChanged() => Observable.Never<Unit>();
        public IObservable<int> ReadRssi() => throw new NotSupportedException();
        public IObservable<BleServiceInfo> GetService(string serviceUuid) => throw new NotSupportedException();
        public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => throw new NotSupportedException();
        public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotSupportedException();
        public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => throw new NotSupportedException();
        public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true) => throw new NotSupportedException();
        public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid) => throw new NotSupportedException();
        public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotSupportedException();
        public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotSupportedException();
        public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => throw new NotSupportedException();
        public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotSupportedException();
        public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => throw new NotSupportedException();
    }
}
