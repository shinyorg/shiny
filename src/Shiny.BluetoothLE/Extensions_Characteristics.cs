using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public static class CharacteristicExtensions
{   
    /// <summary>
    /// Requests all services and characteristics from a peripheral.  Should only be used for niche cases or debugging.
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static IObservable<IReadOnlyList<BleCharacteristicInfo>> GetAllCharacteristics(this IPeripheral peripheral)
        => peripheral
            .GetServices()
            .SelectMany(x => x.Select(y => peripheral.GetCharacteristics(y.Uuid)))
            .Switch()
            .ToArray()
            .Select(results =>
            {
                var list = new List<BleCharacteristicInfo>();
                foreach (var result in results)
                    list.AddRange(result);

                return list;
            });


    /// <summary>
    /// Used for writing streams to a characteristic
    /// </summary>
    /// <param name="serviceUuid">The service of the characteristic</param>
    /// <param name="characteristicUuid">The characteristic to write to</param>
    /// <param name="stream">The stream to send</param>
    /// <param name="packetSendTimeout">How long to wait before timing out a packet send - defaults to 5 seconds</param>
    public static IObservable<BleWriteSegment> WriteCharacteristicBlob(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, Stream stream, TimeSpan? packetSendTimeout = null) => Observable.Create<BleWriteSegment>(async (ob, ct) =>
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
    
    
    /// <summary>
    /// Checks if a characteristic can read
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>     
    public static bool CanRead(this BleCharacteristicInfo ch) => ch.Properties.HasFlag(CharacteristicProperties.Read);
    
    
    /// <summary>
    /// Checks if a characteristic can write with response
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns> 
    public static bool CanWriteWithResponse(this BleCharacteristicInfo ch) => ch.Properties.HasFlag(CharacteristicProperties.Write);
    
    
    /// <summary>
    /// Checks if a characteristic can write without response
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns> 
    public static bool CanWriteWithoutResponse(this BleCharacteristicInfo ch) => ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse);
    
    
    /// <summary>
    /// Checks if a characteristic has some form of write capabilities
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns> 
    public static bool CanWrite(this BleCharacteristicInfo ch) =>
        ch.Properties.HasFlag(CharacteristicProperties.WriteWithoutResponse) ||
        ch.Properties.HasFlag(CharacteristicProperties.Write) ||
        ch.Properties.HasFlag(CharacteristicProperties.AuthenticatedSignedWrites);


    /// <summary>
    /// Checks if a characteristic has notification or indication capabilities
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    public static bool CanNotifyOrIndicate(this BleCharacteristicInfo ch) => ch.CanNotify() || ch.CanIndicate();


    /// <summary>
    /// Checks if a characteristic has notification capabilities
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    public static bool CanNotify(this BleCharacteristicInfo ch) =>
        ch.Properties.HasFlag(CharacteristicProperties.Notify) ||
        ch.Properties.HasFlag(CharacteristicProperties.NotifyEncryptionRequired) ||
        ch.CanIndicate();


    /// <summary>
    /// Checks if a characteristic has indication capabilities
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    public static bool CanIndicate(this BleCharacteristicInfo ch) =>
        ch.Properties.HasFlag(CharacteristicProperties.Indicate) ||
        ch.Properties.HasFlag(CharacteristicProperties.IndicateEncryptionRequired);


    /// <summary>
    /// Asserts that a characteristic has some form of write capabilities
    /// </summary>
    public static void AssertWrite(this BleCharacteristicInfo characteristic, bool withResponse)
    {
        if (!characteristic.CanWrite())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support writes");

        if (withResponse && !characteristic.CanWriteWithResponse())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support writes with response");
    }


    /// <summary>
    /// Asserts that a characteristic has read capabilities
    /// </summary>
    public static void AssertRead(this BleCharacteristicInfo characteristic)
    {
        if (!characteristic.CanRead())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support reads");
    }


    /// <summary>
    /// Asserts that a characteristic has notification capabilities
    /// </summary>
    /// <param name="characteristic"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AssertNotify(this BleCharacteristicInfo characteristic)
    {
        if (!characteristic.CanNotify())
            throw new InvalidOperationException($"This characteristic '{characteristic.Uuid}' does not support notifications");
    }


    /// <summary>
    /// Attempts to read device information a peripheral (if available)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
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


    /// <summary>
    /// Reads battery information from a peripheral (if available)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static IObservable<int> ReadBatteryInformation(this IPeripheral peripheral)
        => StandardIntObservable(peripheral, StandardUuids.BatteryService);

    /// <summary>
    /// Easy observable to connect to a standard BLE heart rate sensor (not guaranteed for all heart rate sensors)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static IObservable<int> HeartRateSensor(this IPeripheral peripheral)
        => StandardIntObservable(peripheral, StandardUuids.HeartRateMeasurementSensor);


    static IObservable<int> StandardIntObservable(IPeripheral peripheral, (string ServiceUuid, string CharacteristicUuid) uuid) => peripheral
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
