using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(ToInfo);


    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        //.Select(_ => Observable.Empty<IReadOnlyList<BleDescriptorInfo>>())
        .Select(ch => Observable.FromAsync<IReadOnlyList<BleDescriptorInfo>>(async ct =>
        {
            var result = await ch.GetDescriptorsAsync(BluetoothCacheMode.Uncached).AsTask(ct);
            //result.Status.Assert()

            //result.Descriptors. //.Select(x => ToInfo(x)).ToList();

            //result.Descriptors.Select(x => { }).ToList();
            //    //return result.Descriptors.Select(x => ToInfo(x, ch)).ToList();
            //    return null;
            return new List<BleDescriptorInfo>(0);
        }))
        .Switch();


    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => Observable.FromAsync(async ct =>
        {
            var result = await desc
                .Descriptor
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);
            // TODO

            return new BleDescriptorResult(
                ToInfo(desc),
                null
            );
        }))
        .Switch();


    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => Observable.FromAsync(async ct =>
        {
            var result = await desc
                .Descriptor
                .WriteValueAsync(null)
                .AsTask(ct)
                .ConfigureAwait(false);
            // TODO

            return new BleDescriptorResult(
                ToInfo(desc),
                data
            );
        }))
        .Switch();


    protected IObservable<(GattDescriptor Descriptor, GattCharacteristic Characteristic)> GetNativeDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => Observable.FromAsync(async ct =>
        {
            var uuid = Utils.ToUuidType(descriptorUuid);
            var result = await ch.GetDescriptorsForUuidAsync(uuid).AsTask(ct).ConfigureAwait(false);

            var desc = result.Descriptors.First();

            return (desc, ch);
        }))
        .Switch();


    protected static BleDescriptorInfo ToInfo((GattDescriptor Descriptor, GattCharacteristic Characteristic) values) => new(
        ToChar(values.Characteristic),
        values.Descriptor.Uuid.ToString()
    );
}