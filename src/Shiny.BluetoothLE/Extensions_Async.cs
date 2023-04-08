using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public static class AsyncExtensions
{
    public static Task<AccessState> RequestAccessAsync(this IBleManager manager)
        => manager.RequestAccess().ToTask();


    public static Task ConnectAsync(this IPeripheral peripheral, ConnectionConfig? config = null, CancellationToken cancelToken = default, TimeSpan? timeout = null)
        => peripheral
            .WithConnectIf(config)
            .Timeout(timeout ?? TimeSpan.FromSeconds(30))
            .ToTask(cancelToken);


    public static Task<IReadOnlyList<BleServiceInfo>> GetServicesAsync(this IPeripheral peripheral, CancellationToken cancelToken = default)
        => peripheral
            .GetServices()
            .ToTask(cancelToken);


    public static Task<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristicsAsync(this IPeripheral peripheral, string serviceUuid, CancellationToken cancelToken = default)
        => peripheral
            .GetCharacteristics(serviceUuid)
            .ToTask(cancelToken);

    public static Task<IReadOnlyList<BleDescriptorInfo>> GetDescriptorsAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default)
        => peripheral
            .GetDescriptors(serviceUuid, characteristicUuid)
            .ToTask(cancelToken);

    public static Task<BleCharacteristicResult> WriteCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral
            .WriteCharacteristic(serviceUuid, characteristicUuid, data, withResponse)
            .Timeout(TimeSpan.FromMilliseconds(timeoutMs))
            .ToTask(cancelToken);


    public static Task<BleCharacteristicResult> ReadCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral
            .ReadCharacteristic(serviceUuid, characteristicUuid)
            .Timeout(TimeSpan.FromMilliseconds(timeoutMs))
            .ToTask(cancelToken);


    public static Task<BleDescriptorResult> ReadDescriptorAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, string descriptorUuid, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral
            .ReadDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
            .Timeout(TimeSpan.FromMilliseconds(timeoutMs))
            .ToTask(cancelToken);


    public static Task<BleDescriptorResult> WriteDescriptorAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral
            .WriteDescriptor(serviceUuid, characteristicUuid, descriptorUuid, data)
            .Timeout(TimeSpan.FromMilliseconds(timeoutMs))
            .ToTask(cancelToken);



    public static Task<BleCharacteristicResult> ReadCharacteristicAsync(this IPeripheral peripheral, BleCharacteristicInfo info, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral.ReadCharacteristicAsync(info.Service.Uuid, info.Uuid, cancelToken, timeoutMs);

    public static Task<BleCharacteristicResult> WriteCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, byte[] data, bool withoutResponse = false, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral.WriteCharacteristicAsync(info.Service.Uuid, info.Uuid, data, withoutResponse, cancelToken, timeoutMs);

    public static Task<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(this IPeripheral peripheral, BleCharacteristicInfo info, CancellationToken cancelToken = default)
        => peripheral.GetDescriptorsAsync(info.Service.Uuid, info.Uuid, cancelToken);

    public static Task<BleDescriptorResult> WriteDescriptorAsync(this IPeripheral peripheral, BleDescriptorInfo info, byte[] data, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral.WriteDescriptorAsync(info.Characteristic.Service.Uuid, info.Characteristic.Uuid, info.Uuid, data, cancelToken, timeoutMs);

    public static Task<BleDescriptorResult> ReadDescriptorAsync(this IPeripheral peripheral, BleDescriptorInfo info, CancellationToken cancelToken = default, int timeoutMs = 3000)
        => peripheral.ReadDescriptorAsync(info.Characteristic.Service.Uuid, info.Characteristic.Uuid, info.Uuid, cancelToken, timeoutMs);
}
