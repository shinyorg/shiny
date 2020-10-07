using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;
using UIKit;


namespace Shiny.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly GattService serivceObj;
        public CBPeripheral Peripheral => this.serivceObj.Peripherial;
        public CBService NativeService => this.serivceObj.Service;
        public CBCharacteristic NativeCharacteristic { get; }


        public GattCharacteristic(GattService service, CBCharacteristic native)
            : base(service, native.UUID.ToString(), (CharacteristicProperties)(int)native.Properties)
        {
            this.serivceObj = service;
            this.NativeCharacteristic = native;
        }


        public override IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse)
        {
            this.AssertWrite(withResponse);
            return withResponse
                ? this.WriteWithResponse(value)
                : this.WriteWithoutResponse(value);
        }


        public override IObservable<CharacteristicGattResult> Read() => Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertRead();
            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Characteristic))
                    return;

                if (args.Error == null)
                    ob.Respond(new CharacteristicGattResult(this, this.NativeCharacteristic.Value?.ToArray(), CharacteristicResultType.Read));
                else
                    ob.OnError(new BleException(args.Error.Description));
            });
            this.Peripheral.UpdatedCharacterteristicValue += handler;
            this.Peripheral.ReadValue(this.NativeCharacteristic);

            return () => this.Peripheral.UpdatedCharacterteristicValue -= handler;
        });


        IObservable<CharacteristicGattResult> notifyOb;
        public override IObservable<CharacteristicGattResult> Notify(bool sendHookEvent, bool enableIndicationsIfAvailable)
        {
            this.AssertNotify();

            this.notifyOb ??= Observable.Create<CharacteristicGattResult>(ob =>
            {
                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (!this.Equals(args.Characteristic))
                        return;

                    if (args.Error == null)
                        ob.OnNext(new CharacteristicGattResult(this, args.Characteristic.Value?.ToArray(), CharacteristicResultType.Notification));
                    else
                        ob.OnError(new BleException(args.Error.Description));
                });
                this.Peripheral.UpdatedCharacterteristicValue += handler;
                this.Peripheral.SetNotifyValue(true, this.NativeCharacteristic);
                this.IsNotifying = true;

                return () =>
                {
                    this.Peripheral.SetNotifyValue(false, this.NativeCharacteristic);
                    this.Peripheral.UpdatedCharacterteristicValue -= handler;
                    this.IsNotifying = false;
                };
            })
            .Publish()
            .RefCount();

            return this.notifyOb;
        }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> DiscoverDescriptors()
        {
            this.descriptorOb ??= Observable.Create<IGattDescriptor>(ob =>
            {
                var descriptors = new Dictionary<string, IGattDescriptor>();

                var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
                {
                    if (this.NativeCharacteristic.Descriptors == null)
                        return;

                    foreach (var dnative in this.NativeCharacteristic.Descriptors)
                    {
                        var wrap = new GattDescriptor(this, dnative);
                        if (!descriptors.ContainsKey(wrap.Uuid))
                        {
                            descriptors.Add(wrap.Uuid, wrap);
                            ob.OnNext(wrap);
                        }
                    }
                });
                this.Peripheral.DiscoveredDescriptor += handler;
                this.Peripheral.DiscoverDescriptors(this.NativeCharacteristic);

                return () => this.Peripheral.DiscoveredDescriptor -= handler;
            })
            .Replay()
            .RefCount();

            return this.descriptorOb;
        }


        public override bool Equals(object obj)
        {
            var other = obj as GattCharacteristic;
            if (other == null)
                return false;

            if (!this.NativeCharacteristic.UUID.Equals(other.NativeCharacteristic.UUID))
                return false;

            if (!other.Service.Equals(this.Service))
                return false;

            return true;
        }


        public override int GetHashCode() => this.NativeCharacteristic.GetHashCode();
        public override string ToString() => this.Uuid.ToString();


        #region Internals

        IObservable<CharacteristicGattResult> WriteWithResponse(byte[] value) => Observable.Create<CharacteristicGattResult>(ob =>
        {
            var data = NSData.FromArray(value);
            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Characteristic))
                    return;

                if (args.Error == null)
                    ob.Respond(new CharacteristicGattResult(this, null, CharacteristicResultType.Write));
                else
                    ob.OnError(new BleException(args.Error.Description));
            });
            this.Peripheral.WroteCharacteristicValue += handler;
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithResponse);

            return () => this.Peripheral.WroteCharacteristicValue -= handler;
        });


        bool Equals(CBCharacteristic ch)
        {
            if (!this.NativeCharacteristic.UUID.Equals(ch.UUID))
                return false;

            if (!this.NativeService.UUID.Equals(ch.Service.UUID))
                return false;

            if (!this.Peripheral.Identifier.Equals(ch.Service.Peripheral.Identifier))
                return false;

            return true;
        }


        IObservable<CharacteristicGattResult> WriteWithoutResponse(byte[] value)
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return this.NewInternalWrite(value);

            return Observable.Return(this.DoWriteNoResponse(value));
        }


        IObservable<CharacteristicGattResult> NewInternalWrite(byte[] value) => Observable.Create<CharacteristicGattResult>(ob =>
        {
            EventHandler? handler = null;
            if (this.Peripheral.CanSendWriteWithoutResponse)
            {
                ob.Respond(this.DoWriteNoResponse(value));
            }
            else
            {
                handler = new EventHandler((sender, args) => ob.Respond(this.DoWriteNoResponse(value)));
                this.Peripheral.IsReadyToSendWriteWithoutResponse += handler;
            }
            return () =>
            {
                if (handler != null)
                    this.Peripheral.IsReadyToSendWriteWithoutResponse -= handler;
            };
        });


        CharacteristicGattResult DoWriteNoResponse(byte[] value)
        {
            var data = NSData.FromArray(value);
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithoutResponse);
            return new CharacteristicGattResult(this, value, CharacteristicResultType.WriteWithoutResponse);
        }

        #endregion
    }
}