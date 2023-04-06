using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    static readonly Java.Util.UUID NotifyDescriptorId = Java.Util.UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");


    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(this.FromNative);

    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => service
            .Characteristics?
            .Select(this.FromNative)
            .ToList()
            ?? new List<BleCharacteristicInfo>(0)
        );

    public bool IsNotifying(string serviceUuid, string characteristicUuid) => throw new NotImplementedException();


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => Observable.Create<BleCharacteristicResult>(ob => 
        {
            if (!ch.Properties.HasFlag(GattProperty.Read))
                throw new InvalidOperationException($"Characteristic '{characteristicUuid}' does not support read");

            // TODO: hook and listen

            if (!this.Gatt!.ReadCharacteristic(ch))
                throw new BleException("Failed to read characteristic: " + characteristicUuid);

            return () => { };
        }))
        .Switch();

    public IObservable<BleCharacteristicResult> WhenNotification(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true) => throw new NotImplementedException();
    public IObservable<BleCharacteristicInfo> WhenNotificationHooked() => throw new NotImplementedException();
    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => throw new NotImplementedException();


    public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status) { }
    public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status) { }
    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic) { }


    protected IObservable<BluetoothGattCharacteristic> GetNativeCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service =>
        {
            var uuid = Utils.ToUuidType(characteristicUuid);
            var ch = service.Characteristics!.FirstOrDefault(x => x.Uuid.Equals(uuid));
            if (ch == null)
                throw new InvalidOperationException($"No characteristic '{characteristicUuid}' exists under service '{serviceUuid}'");

            return ch;
        });


    protected BleCharacteristicInfo FromNative(BluetoothGattCharacteristic ch) => new BleCharacteristicInfo(
        this.FromNative(ch.Service),
        ch.Uuid.ToString(),
        (CharacteristicProperties)(int)ch.Properties
    );


    protected void AssertWrite(BluetoothGattCharacteristic ch, bool withResponse)
    {
        if (withResponse && !ch.Properties.HasFlag(GattProperty.Write))
            throw new InvalidOperationException($"Characteristic '{ch.Uuid}' does not support write with response");

        if (!withResponse && !ch.Properties.HasFlag(GattProperty.WriteNoResponse))
            throw new InvalidOperationException($"Characteristic '{ch.Uuid}' does not support write without response");
    }
}

//public override IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse = true) => this.context.Invoke(Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, IDisposable>)(ob =>
//{
//    this.AssertWrite(withResponse);

//    var sub = this.context
//        .Callbacks
//        .CharacteristicWrite
//        .Where(this.NativeEquals)
//        .Subscribe(args =>
//        {
//            if (!args.IsSuccessful)
//            {
//                ob.OnError(new BleException($"Failed to write characteristic - {args.Status}"));
//            }
//            else
//            {
//                var writeType = withResponse
//                    ? GattCharacteristicResultType.Write
//                    : GattCharacteristicResultType.WriteWithoutResponse;

//                ob.Respond(new GattCharacteristicResult(this, value, writeType));
//            }
//        });

//    this.context.InvokeOnMainThread(() =>
//    {
//        try
//        {
//            this.native.WriteType = withResponse ? GattWriteType.Default : GattWriteType.NoResponse;
//            var authSignedWrite =
//                this.native.Properties.HasFlag(GattProperty.SignedWrite) &&
//                this.context.NativeDevice.BondState == Bond.Bonded;

//            if (authSignedWrite)
//                this.native.WriteType |= GattWriteType.Signed;

//            this.native.SetValue(value);
//            //if (!this.native.SetValue(value))
//            //ob.OnError(new BleException("Failed to set characteristic value"));

//            //else if (!this.context.Gatt.WriteCharacteristic(this.native))
//            if (!this.context.Gatt?.WriteCharacteristic(this.native) ?? false)
//                ob.OnError(new BleException("Failed to write to characteristic"));
//        }
//        catch (Exception ex)
//        {
//            ob.OnError(ex);
//        }
//    });

//    return sub;
//})));


//public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable) => this.context.Invoke(Observable.Create<IGattCharacteristic>(ob =>
//{
//    if (!this.context.Gatt.SetCharacteristicNotification(this.native, enable))
//        throw new BleException("Failed to set characteristic notification value");

//    IDisposable? sub = null;
//    var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
//    if (descriptor == null)
//        throw new ArgumentException("Notification descriptor not found");

//    var wrap = new GattDescriptor(this, this.context, descriptor);
//    var bytes = enable
//        ? this.GetNotifyDescriptorBytes(useIndicationsIfAvailable)
//        : BluetoothGattDescriptor.DisableNotificationValue.ToArray();

//    sub = wrap
//        .WriteInternal(bytes)
//        .Subscribe(
//            _ =>
//            {
//                this.IsNotifying = enable;
//                ob.Respond(this);
//            },
//            ob.OnError
//        );
//    return () => sub?.Dispose();
//}));


//public override IObservable<GattCharacteristicResult> WhenNotificationReceived()
//{
//    this.AssertNotify();
//    return this.context
//        .Callbacks
//        .CharacteristicChanged
//        .Where(this.NativeEquals)
//        .Select(args =>
//        {
//            if (!args.IsSuccessful)
//                throw new BleException($"Notification error - {args.Status}");

//            return new GattCharacteristicResult(
//                this,
//                args.Characteristic.GetValue(),
//                GattCharacteristicResultType.Notification
//            );
//        });
//}


//public override IObservable<IList<IGattDescriptor>> GetDescriptors() => Observable.Return(
//    this.native
//        .Descriptors
//        .Select(x => new GattDescriptor(this, this.context, x))
//        .Cast<IGattDescriptor>()
//        .ToList()
//);


//byte[] GetNotifyDescriptorBytes(bool useIndicationsIfAvailable)
//{
//    if ((useIndicationsIfAvailable || !this.CanNotify()) && this.CanIndicate())
//        return BluetoothGattDescriptor.EnableIndicationValue.ToArray();

//    return BluetoothGattDescriptor.EnableNotificationValue.ToArray();
//}