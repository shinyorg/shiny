using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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


        public override IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse)
        {
            this.AssertWrite(withResponse);
            return withResponse
                ? this.WriteWithResponse(value)
                : this.WriteWithoutResponse(value);
        }


        public override IObservable<GattCharacteristicResult> Read() => Observable.Create<GattCharacteristicResult>(ob =>
        {
            this.AssertRead();
            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Characteristic))
                    return;

                if (args.Error != null)
                {
                    ob.OnError(new BleException(args.Error.Description));
                }
                else
                {
                    var value = this.NativeCharacteristic.Value?.ToArray();
                    var result = new GattCharacteristicResult(this, value, GattCharacteristicResultType.Read);
                    ob.Respond(result);
                }
            });
            this.Peripheral.UpdatedCharacterteristicValue += handler;
            this.Peripheral.ReadValue(this.NativeCharacteristic);

            return () => this.Peripheral.UpdatedCharacterteristicValue -= handler;
        });


        public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable)
        {
            this.AssertNotify();
            this.Peripheral.SetNotifyValue(enable, this.NativeCharacteristic);
            this.IsNotifying = enable;
            return Observable.Return(this);
        }


        public override IObservable<GattCharacteristicResult> WhenNotificationReceived() => Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, Action>)(ob =>
        {
            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Characteristic))
                    return;

                if (args.Error == null)
                    ob.OnNext(new GattCharacteristicResult(this, args.Characteristic.Value?.ToArray(), GattCharacteristicResultType.Notification));
                else
                    ob.OnError(new BleException(args.Error.Description));
            });
            this.Peripheral.UpdatedCharacterteristicValue += handler;

            return () => this.Peripheral.UpdatedCharacterteristicValue -= handler;
        }));



        public override IObservable<IList<IGattDescriptor>> GetDescriptors() => Observable.Create<IList<IGattDescriptor>>(ob =>
        {
            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
            {
                if (this.NativeCharacteristic.Descriptors == null)
                    return;

                var list = this.NativeCharacteristic
                    .Descriptors
                    .Select(native => new GattDescriptor(this, native))
                    .Distinct()
                    .Cast<IGattDescriptor>()
                    .ToList();

                ob.Respond(list);
            });
            this.Peripheral.DiscoveredDescriptor += handler;
            this.Peripheral.DiscoverDescriptors(this.NativeCharacteristic);

            return () => this.Peripheral.DiscoveredDescriptor -= handler;
        });


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

        IObservable<GattCharacteristicResult> WriteWithResponse(byte[] value) => Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, Action>)(ob =>
        {
            var data = NSData.FromArray(value);
            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
            {
                if (!this.Equals(args.Characteristic))
                    return;

                if (args.Error == null)
                    ob.Respond(new GattCharacteristicResult(this, null, GattCharacteristicResultType.Write));
                else
                    ob.OnError(new BleException(args.Error.Description));
            });
            this.Peripheral.WroteCharacteristicValue += handler;
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithResponse);

            return () => this.Peripheral.WroteCharacteristicValue -= handler;
        }));


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


        IObservable<GattCharacteristicResult> WriteWithoutResponse(byte[] value)
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(11, 0))
                return this.NewInternalWrite(value);

            return Observable.Return(this.DoWriteNoResponse(value));
        }


        IObservable<GattCharacteristicResult> NewInternalWrite(byte[] value) => Observable.Create<GattCharacteristicResult>(ob =>
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


        GattCharacteristicResult DoWriteNoResponse(byte[] value)
        {
            var data = NSData.FromArray(value);
            this.Peripheral.WriteValue(data, this.NativeCharacteristic, CBCharacteristicWriteType.WithoutResponse);
            return new GattCharacteristicResult(this, value, GattCharacteristicResultType.WriteWithoutResponse);
        }

        #endregion
    }
}