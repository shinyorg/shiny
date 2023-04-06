using System;
using System.Collections.Generic;
using System.Linq;
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

    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => this.operations.QueueToObservable(async ct =>
        {
            var task = this.descWriteSubj
                .Where(x => x.Descriptor.Equals(x))
                .Take(1)
                .ToTask(ct);

            if (!this.Gatt!.ReadDescriptor(desc))
                throw new InvalidOperationException("Could not read descriptor: " + descriptorUuid);

            await task.ConfigureAwait(false);
            return this.ToResult(desc);
        }))
        .Switch();

    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeDescriptor(serviceUuid, characteristicUuid, descriptorUuid)
        .Select(desc => this.operations.QueueToObservable(async ct =>
        {
            var task = this.descReadSubj
                .Where(x => x.Descriptor.Equals(x))
                .Take(1)
                .ToTask(ct);

            if (!this.Gatt!.ReadDescriptor(desc))
                throw new InvalidOperationException("Could not read descriptor: " + descriptorUuid);

            await task.ConfigureAwait(false);
            return this.ToResult(desc);
        }))
        .Switch();


    readonly Subject<(BluetoothGattDescriptor Descriptor, GattStatus Status)> descReadSubj = new();
    public override void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        => this.descReadSubj.OnNext((descriptor!, status));

    readonly Subject<(BluetoothGattDescriptor Descriptor, GattStatus Status)> descWriteSubj = new();
    public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        => this.descWriteSubj.OnNext((descriptor!, status));

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

    protected BleDescriptorResult ToResult(BluetoothGattDescriptor desc, byte[]? data = null) => new BleDescriptorResult(
        this.FromNative(desc),
        data ?? desc.GetValue()
    );

    protected BleDescriptorInfo FromNative(BluetoothGattDescriptor desc) => new BleDescriptorInfo(
        this.FromNative(desc.Characteristic!),
        desc.Uuid!.ToString()
        //desc.Permissions
    );
}


//protected internal IObservable<GattDescriptorResult> WriteInternal(byte[] data) => Observable.Create<GattDescriptorResult>(ob =>
//{
//    var sub = this.context
//        .Callbacks
//        .DescriptorWrite
//        .Where(this.NativeEquals)
//        .Subscribe(args =>
//        {
//            if (args.IsSuccessful)
//                ob.Respond(new GattDescriptorResult(this, data));
//            else
//                ob.OnError(new BleException($"Failed to write descriptor value - {args.Status}"));
//        });

//    this.context.InvokeOnMainThread(() =>
//    {
//        try
//        {
//            if (!this.native.SetValue(data))
//                ob.OnError(new BleException("Failed to set descriptor value"));

//            else if (!this.context.Gatt?.WriteDescriptor(this.native) ?? false)
//                ob.OnError(new BleException("Failed to write to descriptor"));
//        }
//        catch (Exception ex)
//        {
//            ob.OnError(ex);
//        }
//    });

//    return sub;
//});


//public override IObservable<GattDescriptorResult> Read() => this.context.Invoke(Observable.Create<GattDescriptorResult>(ob =>
//{
//    var sub = this.context
//        .Callbacks
//        .DescriptorRead
//        .Where(this.NativeEquals)
//        .Subscribe(args =>
//        {
//            if (args.IsSuccessful)
//                ob.Respond(new GattDescriptorResult(this, args.Descriptor.GetValue()));
//            else
//                ob.OnError(new BleException($"Failed to read descriptor value - {args.Status}"));
//        });

//    this.context.InvokeOnMainThread(() =>
//    {
//        try
//        {
//            if (!this.context.Gatt?.ReadDescriptor(this.native) ?? false)
//                ob.OnError(new BleException("Failed to read descriptor"));
//        }
//        catch (Exception ex)
//        {
//            ob.OnError(ex);
//        }
//    });

//    return sub;
//}));