using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.Bluetooth;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => ch.Descriptors!.Select(this.FromNative).ToList());


    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(this.FromNative);


    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => this.operations.QueueToObservable(async ct =>
        {
            await this.WriteDescriptor(desc, data, ct).ConfigureAwait(false);
            return this.ToResult(desc);
        }))
        .Switch();


    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => this.operations.QueueToObservable(async ct =>
        {
            this.descSubj ??= new();

            var task = this.descSubj
                .Where(x => x.Descriptor.Equals(desc) && !x.IsWrite)
                .Take(1)
                .ToTask(ct);
            
            if (!this.Gatt!.ReadDescriptor(desc))
                throw new InvalidOperationException("Could not read descriptor: " + descriptorUuid);

            await task.ConfigureAwait(false);
            return this.ToResult(desc);
        }))
        .Switch();

    
    protected async Task WriteDescriptor(BluetoothGattDescriptor descriptor, byte[] data, CancellationToken ct)
    {
        this.descSubj ??= new();
        var task = this.descSubj
            .Where(x => x.Descriptor.Equals(descriptor) && x.IsWrite)
            .Take(1)
            .ToTask(ct);

#if XAMARIN
        if (!descriptor.SetValue(data))
            throw new BleException("Unable to set GattDescriptor: " + descriptor.Uuid);

        if (!this.Gatt!.WriteDescriptor(descriptor))
            throw new InvalidOperationException("Could not writte descriptor: " + descriptor.Uuid);
#else
        if (OperatingSystemShim.IsAndroidVersionAtLeast(33))
        {
            this.Gatt!.WriteDescriptor(descriptor, data);
        }
        else
        {
            if (!descriptor.SetValue(data))
                throw new BleException("Unable to set GattDescriptor: " + descriptor.Uuid);

            if (!this.Gatt!.WriteDescriptor(descriptor))
                throw new InvalidOperationException("Could not write descriptor: " + descriptor.Uuid);
        }
#endif
        var result = await task.ConfigureAwait(false);
        if (result.Status != GattStatus.Success)
            throw new BleException($"Failed to write descriptor: {descriptor.Uuid} - {result.Status}");
    }
    
    
    protected IObservable<BluetoothGattDescriptor> GetNativeDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch =>
        {
            var uuid = Utils.ToUuidType(descriptorUuid);
            var desc = ch.GetDescriptor(uuid);
            if (desc == null)
                throw new InvalidOperationException($"There is not descriptor '{descriptorUuid}' under service/characteristic: {serviceUuid}/{characteristicUuid}");

            return desc;
        });


    Subject<(BluetoothGattDescriptor Descriptor, GattStatus Status, bool IsWrite)>? descSubj = new();
    public override void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        => this.descSubj?.OnNext((descriptor!, status, false));


    public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        => this.descSubj?.OnNext((descriptor!, status, true));


    protected BleDescriptorResult ToResult(BluetoothGattDescriptor desc) => new BleDescriptorResult(
        this.FromNative(desc),
        desc.GetValue()
    );

    
    protected BleDescriptorInfo FromNative(BluetoothGattDescriptor desc) => new BleDescriptorInfo(
        this.FromNative(desc.Characteristic!),
        desc.Uuid!.ToString()
        //desc.Permissions
    );
}