using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(this.FromNative);

    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => this
        .GetNativeDescriptors(serviceUuid, characteristicUuid)
        .Select(x => x.Select(this.FromNative).ToList());


    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => this.operations.QueueToObservable(async ct =>
        {
            var task = this.descSubj.Where(x => x.Descriptor.Equals(desc)).Take(1).ToTask(ct);
            this.Native.ReadValue(desc);
            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw ToException(result.Error);

            return this.ToResult(desc);
        }))
        .Switch();


    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => this.operations.QueueToObservable(async ct =>
        {
            var task = this.descSubj.Where(x => x.Descriptor!.Equals(desc)).Take(1).ToTask(ct);

            var nsdata = NSData.FromArray(data);            
            this.Native.WriteValue(nsdata, desc);
            
            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw ToException(result.Error);

            return this.ToResult(desc);
        }))
        .Switch();


    readonly Subject<(CBDescriptor Descriptor, NSError? Error, bool IsWrite)> descSubj = new();
    public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError? error)
        => this.descSubj.OnNext((descriptor, error, false));

    public override void WroteDescriptorValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError? error)
        => this.descSubj.OnNext((descriptor, error, true));


    readonly Subject<(CBCharacteristic Char, NSError? Error)> descDiscSubj = new();
    public override void DiscoveredDescriptor(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.descDiscSubj.OnNext((characteristic, error));


    //var bytes = args.Descriptor.ToByteArray(); from old code
    protected BleDescriptorResult ToResult(CBDescriptor native) => new(
        this.FromNative(native),
        (native.Value as NSData)?.ToArray()
    );

    protected BleDescriptorInfo FromNative(CBDescriptor native) => new(
        this.FromNative(native.Characteristic!),
        native.UUID.ToString()
    );

    protected IObservable<CBDescriptor[]> GetNativeDescriptors(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => this.operations.QueueToObservable(async ct =>
        {
            var task = this.descDiscSubj.Where(x => x.Char!.Equals(ch)).Take(1).ToTask(ct);
            this.Native!.DiscoverDescriptors(ch);

            var result = await task.ConfigureAwait(false);
            if (result.Error != null)
                throw new BleException("Could not read descriptors for characteristic: " + characteristicUuid);

            return ch.Descriptors!;
        }))
        .Switch();

    protected IObservable<CBDescriptor> GetNativeDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptors(serviceUuid, characteristicUuid)
        .Select(descs =>
        {
            var uuid = CBUUID.FromString(descriptorUuid);
            var desc = descs.FirstOrDefault(x => x.UUID.Equals(uuid));
            if (desc == null)
                throw new BleException("No descriptor found for " + descriptorUuid);

            return desc;
        });
}