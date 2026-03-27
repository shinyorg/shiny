using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var uuid = Utils.ToUuidType(descriptorUuid);

        var result = await ch.GetDescriptorsForUuidAsync(uuid).AsTask(ct).ConfigureAwait(false);
        result.Status.Assert("GetDescriptor", serviceUuid, characteristicUuid, descriptorUuid);

        var desc = result.Descriptors.FirstOrDefault()
            ?? throw new BleException($"Descriptor '{descriptorUuid}' not found");

        return ToDescInfo(ch, desc);
    });


    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);

        var result = await ch
            .GetDescriptorsAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("GetDescriptors", serviceUuid, characteristicUuid);

        return (IReadOnlyList<BleDescriptorInfo>)result.Descriptors
            .Select(d => ToDescInfo(ch, d))
            .ToList();
    });


    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var (ch, desc) = await this.GetNativeDescriptorAsync(serviceUuid, characteristicUuid, descriptorUuid, ct).ConfigureAwait(false);

        var result = await desc
            .ReadValueAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("ReadDescriptor", serviceUuid, characteristicUuid, descriptorUuid);

        return new BleDescriptorResult(
            ToDescInfo(ch, desc),
            result.Value?.ToArray()
        );
    });


    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var (ch, desc) = await this.GetNativeDescriptorAsync(serviceUuid, characteristicUuid, descriptorUuid, ct).ConfigureAwait(false);

        var buffer = data.AsBuffer();
        var result = await desc
            .WriteValueAsync(buffer)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Assert("WriteDescriptor", serviceUuid, characteristicUuid, descriptorUuid);

        return new BleDescriptorResult(
            ToDescInfo(ch, desc),
            data
        );
    });


    async System.Threading.Tasks.Task<(GattCharacteristic Ch, GattDescriptor Desc)> GetNativeDescriptorAsync(
        string serviceUuid,
        string characteristicUuid,
        string descriptorUuid,
        System.Threading.CancellationToken ct)
    {
        var ch = await this.GetNativeCharacteristicAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var uuid = Utils.ToUuidType(descriptorUuid);

        var result = await ch.GetDescriptorsForUuidAsync(uuid).AsTask(ct).ConfigureAwait(false);
        result.Status.Assert("GetDescriptor", serviceUuid, characteristicUuid, descriptorUuid);

        var desc = result.Descriptors.FirstOrDefault()
            ?? throw new BleException($"Descriptor '{descriptorUuid}' not found");

        return (ch, desc);
    }


    static BleDescriptorInfo ToDescInfo(GattCharacteristic ch, GattDescriptor desc) => new(
        ToCharInfo(ch),
        desc.Uuid.ToString()
    );
}
