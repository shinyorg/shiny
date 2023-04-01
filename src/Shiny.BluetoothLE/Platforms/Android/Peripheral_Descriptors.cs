using System;
using System.Collections.Generic;
using System.Reactive;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => throw new NotImplementedException();

    public IObservable<byte[]> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotImplementedException();
    public IObservable<Unit> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => throw new NotImplementedException();

    public override void OnDescriptorRead(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status) { }
    public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status) { }
}

//public override IObservable<GattDescriptorResult> Write(byte[] data) =>
//        this.context.Invoke(this.WriteInternal(data));


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