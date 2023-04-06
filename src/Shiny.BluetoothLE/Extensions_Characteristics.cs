using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;

namespace Shiny.BluetoothLE;


public static class CharacteristicExtensions
{
    /// <summary>
    /// Used for writing blobs
    /// </summary>
    /// <param name="ch">The characteristic to write on</param>
    /// <param name="stream">The stream to send</param>
    /// <param name="packetSendTimeout">How long to wait before timing out a packet send - defaults to 5 seconds</param>
    public static IObservable<BleWriteSegment> WriteBlob(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, Stream stream, TimeSpan? packetSendTimeout = null) => Observable.Create<BleWriteSegment>(async (ob, ct) =>
    {
        var mtu = peripheral.Mtu;
        var buffer = new byte[mtu];
        var read = stream.Read(buffer, 0, buffer.Length);
        var pos = read;
        var len = Convert.ToInt32(stream.Length);
        var remaining = 0;
        var timeout = packetSendTimeout ?? TimeSpan.FromSeconds(5);

        while (!ct.IsCancellationRequested && read > 0)
        {
            await peripheral
                .WriteCharacteristic(serviceUuid, characteristicUuid, buffer)
                .Timeout(timeout)
                .ToTask(ct)
                .ConfigureAwait(false);

            //if (this.Value != buffer)
            //{
            //    trans.Abort();
            //    throw new GattReliableWriteTransactionException("There was a mismatch response");
            //}
            var seg = new BleWriteSegment(buffer, pos, len);
            ob.OnNext(seg);

            remaining = len - pos;
            if (remaining > 0 && remaining < mtu)
            {
                // readjust buffer -- we don't want to send extra garbage
                buffer = new byte[remaining];
            }

            read = stream.Read(buffer, 0, buffer.Length);
            pos += read;
        }
        ob.OnCompleted();

        return Disposable.Empty;
    });


    public static IObservable<byte[]> ReadCharacteristicInterval(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, TimeSpan timeSpan)
        => Observable
            .Interval(timeSpan)
            .Select(_ => peripheral.ReadCharacteristic(serviceUuid, characteristicUuid))
            .Switch()
            .Select(x => x.Data);


    public static bool CanRead(this BleCharacteristicInfo ch) => ch.Properties.HasFlag(CharacteristicProperties.Read);
    public static bool CanWriteWithResponse(this BleCharacteristicInfo ch) => ch.Properties.HasFlag(CharacteristicProperties.Write);
    public static bool CanWriteWithoutResponse(this BleCharacteristicInfo ch) => ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse);
    public static bool CanWrite(this BleCharacteristicInfo ch) =>
        ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse) ||
        ch.Properties.HasFlag(CharacteristicProperties.Write) ||
        ch.Properties.HasFlag(CharacteristicProperties.AuthenticatedSignedWrites);


    public static bool CanNotifyOrIndicate(this BleCharacteristicInfo ch) => ch.CanNotify() || ch.CanIndicate();


    public static bool CanNotify(this BleCharacteristicInfo ch) =>
        ch.Properties.HasFlag(CharacteristicProperties.Notify) ||
        ch.Properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired) ||
        ch.CanIndicate();


    public static bool CanIndicate(this BleCharacteristicInfo ch) =>
        ch.Properties.HasFlag(CharacteristicProperties.Indicate) ||
        ch.Properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired);


    public static void AssertWrite(this BleCharacteristicInfo characteristic, bool withResponse)
    {
        if (!characteristic.CanWrite())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support writes");

        if (withResponse && !characteristic.CanWriteWithResponse())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support writes with response");
    }


    public static void AssertRead(this BleCharacteristicInfo characteristic)
    {
        if (!characteristic.CanRead())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support reads");
    }


    public static void AssertNotify(this BleCharacteristicInfo characteristic)
    {
        if (!characteristic.CanNotify())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support notifications");
    }

    public static IObservable<DeviceInfo> ReadDeviceInformation(this IPeripheral peripheral)
        => peripheral
            .GetCharacteristics(StandardUuids.DeviceInformationServiceUuid)
            .SelectMany(x => x.Select(y => peripheral.ReadCharacteristic(y.Service.Uuid, y.Uuid)))
            .Concat()
            .ToList()
            .Select(data =>
            {
                var dev = new DeviceInfo();
                foreach (var item in data)
                {
                    if (item.Data == null)
                        continue;

                    switch (item.Characteristic.Uuid.ToLower())
                    {
                        case "2a23":
                        case "00002a23-0000-1000-8000-00805f9b34fb":
                            dev.SystemId = Encoding.UTF8.GetString(item.Data);
                            break;

                        case "2a24":
                        case "00002a24-0000-1000-8000-00805f9b34fb":
                            dev.ModelNumber = Encoding.UTF8.GetString(item.Data);
                            break;

                        case "2a25":
                        case "00002a25-0000-1000-8000-00805f9b34fb":
                            dev.SerialNumber = Encoding.UTF8.GetString(item.Data);
                            break;

                        case "2a26":
                        case "00002a26-0000-1000-8000-00805f9b34fb":
                            dev.FirmwareRevision = Encoding.UTF8.GetString(item.Data);
                            break;

                        case "2a27":
                        case "00002a27-0000-1000-8000-00805f9b34fb":
                            dev.HardwareRevision = Encoding.UTF8.GetString(item.Data);
                            break;

                        case "2a28":
                        case "00002a28-0000-1000-8000-00805f9b34fb":
                            dev.SoftwareRevision = Encoding.UTF8.GetString(item.Data);
                            break;

                        case "2a29":
                        case "00002a29-0000-1000-8000-00805f9b34fb":
                            dev.ManufacturerName = Encoding.UTF8.GetString(item.Data);
                            break;
                    }
                }
                return dev;
            });


    public static IObservable<int> ReadBatteryInformation(this IPeripheral peripheral)
        => StandardIntObserable(peripheral, StandardUuids.BatteryService);

    public static IObservable<int> HeartRateSensor(this IPeripheral peripheral)
        => StandardIntObserable(peripheral, StandardUuids.HeartRateMeasurementSensor);


    static IObservable<int> StandardIntObserable(IPeripheral peripheral, (string ServiceUuid, string CharacteristicUuid) uuid) => peripheral
        .ReadCharacteristic(uuid.ServiceUuid, uuid.CharacteristicUuid)
        .Select(x => (int)x.Data[0]);
}


public record BleWriteSegment(
    byte[] Chunk,
    int Position,
    int TotalLength
);


public class DeviceInfo
{
    public string? SystemId { get; set; }
    public string? ManufacturerName { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareRevision { get; set; }
    public string? HardwareRevision { get; set; }
    public string? SoftwareRevision { get; set; }
}
