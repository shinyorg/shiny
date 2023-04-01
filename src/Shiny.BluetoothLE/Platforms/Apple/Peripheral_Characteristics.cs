using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace Shiny.BluetoothLE;


public record NotificationSubscription(
    string ServiceUuid,
    string CharacteristicUuid
);

public partial class Peripheral
{
    public IObservable<BleCharacteristicResult> WhenNotification(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true) => throw new NotImplementedException();
    public IObservable<BleCharacteristicInfo> WhenNotificationHooked() => throw new NotImplementedException();
    public IObservable<Unit> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => throw new NotImplementedException();

    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotImplementedException();
    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => throw new NotImplementedException();


    public bool IsNotifying(string serviceUuid, string characteristicUuid)
    {
        return false;
    }


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => Observable.Create<BleCharacteristicResult>(ob =>
    {
        //AssertConnected
        //this.AssertRead();
        var suid = CBUUID.FromString(serviceUuid);
        var cuid = CBUUID.FromString(characteristicUuid);

        var sub = this.charUpdateSubj
            .Where(x => x.Char.UUID.Equals(cuid) && x.Char.Service!.UUID.Equals(suid))
            .Do(x =>
            {
                if (x.Error != null)
                    throw new InvalidCastException(x.Error.LocalizedDescription);
            })
            .Select(x => new BleCharacteristicResult(
                serviceUuid,
                characteristicUuid,
                x.Char.Value?.ToArray()
            ))
            .Subscribe(
                ob.OnNext,
                ob.OnError
            );

        var service = this.Native.Services?.FirstOrDefault(x => x.UUID.Equals(suid));
        var ch = service.Characteristics?.FirstOrDefault(x => x.UUID.Equals(cuid));
        this.Native.ReadValue(ch);
        
        return () => { };
    });


    readonly Subject<CBCharacteristic[]> charDiscoverySubj = new();
    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
    {
        if (service.Characteristics != null)
            this.charDiscoverySubj.OnNext(service.Characteristics);
    }


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charUpdateSubj = new();
    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.charUpdateSubj.OnNext((characteristic, error));


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charWroteSubj = new();
    public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.charWroteSubj.OnNext((characteristic, error));

    //        public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable)
    //        {
    //            this.AssertNotify();
    //            this.Peripheral.SetNotifyValue(enable, this.NativeCharacteristic);
    //            this.IsNotifying = enable;
    //            return Observable.Return(this);
    //        }


    //        public override IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse)
    //        {
    //            this.AssertWrite(withResponse);
    //            return withResponse
    //                ? this.WriteWithResponse(value)
    //                : this.WriteWithoutResponse(value);
    //        }

    //        public override IObservable<GattCharacteristicResult> WhenNotificationReceived() => Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, Action>)(ob =>
    //        {
    //            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
    //            {
    //                if (!this.Equals(args.Characteristic))
    //                    return;

    //                if (args.Error == null)
    //                    ob.OnNext(new GattCharacteristicResult(this, args.Characteristic.Value?.ToArray(), GattCharacteristicResultType.Notification));
    //                else
    //                    ob.OnError(new BleException(args.Error.Description));
    //            });
    //            this.Peripheral.UpdatedCharacterteristicValue += handler;

    //            return () => this.Peripheral.UpdatedCharacterteristicValue -= handler;
    //        }));


    //        #region Internals

    //        IObservable<GattCharacteristicResult> WriteWithResponse(byte[] value) => Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, Action>)(ob =>
    //        {
    //            var data = NSData.FromArray(value);
    //            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
    //            {
    //                if (!this.Equals(args.Characteristic))
    //                    return;

    //                if (args.Error == null)
    //                    ob.Respond(new GattCharacteristicResult(this, null, GattCharacteristicResultType.Write));
    //                else
    //                    ob.OnError(new BleException(args.Error.Description));
    //            });
    //            this.Peripheral.WroteCharacteristicValue += handler;
    //            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithResponse);

    //            return () => this.Peripheral.WroteCharacteristicValue -= handler;
    //        }));





    //        IObservable<GattCharacteristicResult> WriteWithoutResponse(byte[] value)
    //        {
    //            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
    //                return this.NewInternalWrite(value);

    //            return Observable.Return(this.DoWriteNoResponse(value));
    //        }


    //        IObservable<GattCharacteristicResult> NewInternalWrite(byte[] value) => Observable.Create<GattCharacteristicResult>(ob =>
    //        {
    //            EventHandler? handler = null;
    //            if (this.Peripheral.CanSendWriteWithoutResponse)
    //            {
    //                ob.Respond(this.DoWriteNoResponse(value));
    //            }
    //            else
    //            {
    //                handler = new EventHandler((sender, args) => ob.Respond(this.DoWriteNoResponse(value)));
    //                this.Peripheral.IsReadyToSendWriteWithoutResponse += handler;
    //            }
    //            return () =>
    //            {
    //                if (handler != null)
    //                    this.Peripheral.IsReadyToSendWriteWithoutResponse -= handler;
    //            };
    //        });


    //        GattCharacteristicResult DoWriteNoResponse(byte[] value)
    //        {
    //            var data = NSData.FromArray(value);
    //            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithoutResponse);
    //            return new GattCharacteristicResult(this, value, GattCharacteristicResultType.WriteWithoutResponse);
    //        }
}