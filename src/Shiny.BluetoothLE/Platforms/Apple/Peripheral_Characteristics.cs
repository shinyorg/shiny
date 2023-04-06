using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;
using UIKit;

namespace Shiny.BluetoothLE;


//public record NotificationSubscription(
//    string ServiceUuid,
//    string CharacteristicUuid
//);

public partial class Peripheral
{
    public IObservable<BleCharacteristicResult> WhenNotification(string serviceUuid, string characteristicUuid, bool useIndicateIfAvailable = true) => throw new NotImplementedException();
    public IObservable<BleCharacteristicInfo> WhenNotificationHooked() => throw new NotImplementedException();
    public bool IsNotifying(string serviceUuid, string characteristicUuid)
    {
        return false;
    }


    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(this.FromNative);


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch =>
        {
            this.AssertWrite(ch, withResponse);
            return withResponse
                ? this.WriteWithResponse(ch, data)
                : this.WriteWithoutResponse(ch, data);
        })
        .Switch();


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => Observable.Create<IReadOnlyList<BleCharacteristicInfo>>(ob =>
        {
            var sub = this.charDiscoverySubj
                .Where(x => x.Service.Equals(service))
                .Subscribe(x =>
                {
                    if (x.Error != null)
                    {
                        ob.OnError(new InvalidOperationException(x.Error.LocalizedDescription));
                    }
                    else if (service.Characteristics != null)
                    {
                        var list = service.Characteristics.Select(this.FromNative).ToList();
                        ob.Respond(list);
                    }
                });

            this.Native.DiscoverCharacteristics(service);

            return () => sub.Dispose();
        }))
        .Switch();


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => Observable.Create<BleCharacteristicResult>(ob =>
        {
            if (!ch.Properties.HasFlag(CBCharacteristicProperties.Read))
                throw new InvalidOperationException($"Characteristic '{characteristicUuid}' does not support read");

            var sub = this.charUpdateSubj
                .Where(x => x.Char.Equals(ch))
                .Do(x =>
                {
                    if (x.Error != null)
                        throw new InvalidCastException(x.Error.LocalizedDescription);
                })
                .Select(x => new BleCharacteristicResult(
                    this.FromNative(x.Char),
                    x.Char.Value?.ToArray()
                ))
                .Subscribe(
                    x => ob.Respond(x), // 1 and done
                    ob.OnError
                );

            // could queue writes to ensure operation order - iOS does this pretty well
            this.Native.ReadValue(ch);

            return () => sub.Dispose();
        }))
        .Switch();



    // TODO: this should be a process queue
    public override void IsReadyToSendWriteWithoutResponse(CBPeripheral peripheral) { }

    readonly Subject<(CBService Service, NSError? Error)> charDiscoverySubj = new();
    public override void DiscoveredCharacteristics(CBPeripheral peripheral, CBService service, NSError? error)
        => this.charDiscoverySubj.OnNext((service, error));


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charUpdateSubj = new();
    public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.charUpdateSubj.OnNext((characteristic, error));


    readonly Subject<(CBCharacteristic Char, NSError? Error)> charWroteSubj = new();
    public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError? error)
        => this.charWroteSubj.OnNext((characteristic, error));


    protected BleCharacteristicInfo FromNative(CBCharacteristic ch) => new BleCharacteristicInfo(
            this.FromNative(ch.Service!),
            ch.UUID.ToString(),
            (CharacteristicProperties)ch.Properties
        );


    protected IObservable<CBCharacteristic> GetNativeCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetNativeService(serviceUuid)
        .Select(service => Observable.Create<CBCharacteristic>(ob =>
        {
            IDisposable? sub = null;
            var uuid = CBUUID.FromString(characteristicUuid);
            var ch = service.Characteristics?.FirstOrDefault(x => x.UUID.Equals(uuid));

            if (ch == null)
            {
                sub = this.charDiscoverySubj
                    .Where(x => x.Service.Equals(service))
                    .Subscribe(x =>
                    {
                        if (x.Error != null)
                        {
                            ob.OnError(new InvalidOperationException(x.Error.LocalizedDescription));
                        }
                        else if (service.Characteristics != null)
                        {
                            ch = service.Characteristics.FirstOrDefault(x => x.UUID.Equals(uuid));
                            if (ch == null)
                                ob.OnError(new InvalidOperationException($"No characteristic found in service '{serviceUuid}' with UUID: {characteristicUuid}"));
                            else
                                ob.Respond(ch);
                        }
                    });

                this.Native.DiscoverCharacteristics(new[] { uuid }, service);
            }

            return () => sub?.Dispose();
        }))
        .Switch();


    protected IObservable<BleCharacteristicResult> WriteWithResponse(CBCharacteristic nativeCh, byte[] value) => Observable.Create<BleCharacteristicResult>(ob =>
    {
        var data = NSData.FromArray(value);

        // TODO: if queuing ops, should wait to hook this
        var sub = this.charWroteSubj
            .Where(x => x.Char.Equals(nativeCh))
            .Subscribe(x =>
            {
                if (x.Error == null)
                    ob.Respond(new BleCharacteristicResult(this.FromNative(nativeCh), nativeCh.Value?.ToArray()));
                else
                    ob.OnError(new InvalidOperationException(x.Error.LocalizedDescription));
            });

        // TODO: should wait in queue
        this.Native.WriteValue(data, nativeCh, CBCharacteristicWriteType.WithResponse);

        return () => sub.Dispose();
    });


    protected IObservable<BleCharacteristicResult> WriteWithoutResponse(CBCharacteristic nativeCh, byte[] value) => Observable.FromAsync<BleCharacteristicResult>(async ct =>
    {
        if (!this.Native.CanSendWriteWithoutResponse)
        {
            // TODO: wait for opportunity to send
            //ob.Respond(new BleCharacteristicResult(this.FromNative(nativeCh), nativeCh.Value?.ToArray()));
        }
        var data = NSData.FromArray(value);
        this.Native.WriteValue(data, nativeCh, CBCharacteristicWriteType.WithoutResponse);

        return default;
        // TODO: if cancelled, remove from queue
    });


    protected void AssertWrite(CBCharacteristic ch, bool withResponse)
    {
        if (withResponse && !ch.Properties.HasFlag(CBCharacteristicProperties.Write))
            throw new InvalidOperationException($"Characteristic '{ch.UUID}' does not support write with response");

        if (!withResponse && !ch.Properties.HasFlag(CBCharacteristicProperties.WriteWithoutResponse))
            throw new InvalidOperationException($"Characteristic '{ch.UUID}' does not support write without response");
    }


    //        public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable)
    //        {
    //            this.AssertNotify();
    //            this.Peripheral.SetNotifyValue(enable, this.NativeCharacteristic);
    //            this.IsNotifying = enable;
    //            return Observable.Return(this);
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

}