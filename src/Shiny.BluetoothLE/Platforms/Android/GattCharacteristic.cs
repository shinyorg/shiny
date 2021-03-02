using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.BluetoothLE.Internals;
using Android.Bluetooth;
using Java.Util;
using Observable = System.Reactive.Linq.Observable;


namespace Shiny.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        static readonly UUID NotifyDescriptorId = UUID.FromString("00002902-0000-1000-8000-00805f9b34fb");
        readonly BluetoothGattCharacteristic native;
        readonly PeripheralContext context;


        public GattCharacteristic(
            IGattService service,
            PeripheralContext context,
            BluetoothGattCharacteristic native
        )
        : base(
              service,
              native.Uuid.ToString(),
              (CharacteristicProperties)(int)native.Properties
        )
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse = true) => this.context.Invoke(Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, IDisposable>)(ob =>
        {
            this.AssertWrite(withResponse);

            var sub = this.context
                .Callbacks
                .CharacteristicWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (!args.IsSuccessful)
                    {
                        ob.OnError(new BleException($"Failed to write characteristic - {args.Status}"));
                    }
                    else
                    {
                        var writeType = withResponse
                            ? GattCharacteristicResultType.Write
                            : GattCharacteristicResultType.WriteWithoutResponse;

                        ob.Respond(new GattCharacteristicResult(this, value, writeType));
                    }
                });

            this.context.InvokeOnMainThread(() =>
            {
                try
                {
                    this.native.WriteType = withResponse ? GattWriteType.Default : GattWriteType.NoResponse;
                    var authSignedWrite =
                        this.native.Properties.HasFlag(GattProperty.SignedWrite) &&
                        this.context.NativeDevice.BondState == Bond.Bonded;

                    if (authSignedWrite)
                        this.native.WriteType |= GattWriteType.Signed;

                    this.native.SetValue(value);
                    //if (!this.native.SetValue(value))
                    //ob.OnError(new BleException("Failed to set characteristic value"));

                    //else if (!this.context.Gatt.WriteCharacteristic(this.native))
                    if (!this.context.Gatt?.WriteCharacteristic(this.native) ?? false)
                        ob.OnError(new BleException("Failed to write to characteristic"));
                }
                catch (Exception ex)
                {
                    ob.OnError(ex);
                }
            });

            return sub;
        })));


        public override IObservable<GattCharacteristicResult> Read() => this.context.Invoke(Observable.Create<GattCharacteristicResult>((Func<IObserver<GattCharacteristicResult>, IDisposable>)(ob =>
        {
            this.AssertRead();

            var sub = this.context
                .Callbacks
                .CharacteristicRead
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (!args.IsSuccessful)
                    {
                        ob.OnError(new BleException($"Failed to read characteristic - {args.Status}"));
                    }
                    else
                    {
                        var value = args.Characteristic.GetValue();
                        var result = new GattCharacteristicResult(this, value, GattCharacteristicResultType.Read);
                        ob.Respond(result);
                    }
                });

            this.context.InvokeOnMainThread(() =>
            {
                try
                {
                    if (!this.context.Gatt?.ReadCharacteristic(this.native) ?? false)
                        throw new BleException("Failed to read characteristic");
                }
                catch (Exception ex)
                {
                    ob.OnError(ex);
                }
            });

            return sub;
        })));


        public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable) => this.context.Invoke(Observable.Create<IGattCharacteristic>(ob =>
        {
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, enable))
                throw new BleException("Failed to set characteristic notification value");

            IDisposable? sub = null;
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
                throw new ArgumentException("Notification descriptor not found");

            var wrap = new GattDescriptor(this, this.context, descriptor);
            var bytes = enable
                ? this.GetNotifyDescriptorBytes(useIndicationsIfAvailable)
                : BluetoothGattDescriptor.DisableNotificationValue.ToArray();

            sub = wrap
                .WriteInternal(bytes)
                .Subscribe(
                    _ =>
                    {
                        this.IsNotifying = enable;
                        ob.Respond(this);
                    },
                    ob.OnError
                );
            return () => sub?.Dispose();
        }));


        public override IObservable<GattCharacteristicResult> WhenNotificationReceived()
        {
            this.AssertNotify();
            return this.context
                .Callbacks
                .CharacteristicChanged
                .Where(this.NativeEquals)
                .Select(args =>
                {
                    if (!args.IsSuccessful)
                        throw new BleException($"Notification error - {args.Status}");

                    return new GattCharacteristicResult(
                        this,
                        args.Characteristic.GetValue(),
                        GattCharacteristicResultType.Notification
                    );
                });
        }


        public override IObservable<IList<IGattDescriptor>> GetDescriptors() =>
            this.native
                .Descriptors
                .Select(x => new GattDescriptor(this, this.context, x))
                .Cast<IGattDescriptor>()
                .ToList()
                .Cast<IList<IGattDescriptor>>()
                .ToObservable();


        public override bool Equals(object obj)
        {
            var other = obj as GattCharacteristic;
            if (other == null)
                return false;

            if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override int GetHashCode() => this.native.GetHashCode();
        public override string ToString() => $"Characteristic: {this.Uuid}";

        #region Internals

        bool NativeEquals(GattCharacteristicEventArgs args)
        {
            if (this.context?.Gatt == null)
                return false;

            if (args?.Characteristic?.Service == null)
                return false;

            if (args.Gatt == null)
                return false;

            if (this.native.Equals(args.Characteristic))
                return true;

            if (!this.native.Uuid.Equals(args.Characteristic.Uuid))
                return false;

            if (!this.native.Service.Uuid.Equals(args.Characteristic.Service.Uuid))
                return false;

            if (!this.context.Gatt.Equals(args.Gatt))
                return false;

            return true;
        }


        byte[] GetNotifyDescriptorBytes(bool useIndicationsIfAvailable)
        {
            if ((useIndicationsIfAvailable || !this.CanNotify()) && this.CanIndicate())
                return BluetoothGattDescriptor.EnableIndicationValue.ToArray();

            return BluetoothGattDescriptor.EnableNotificationValue.ToArray();
        }

        #endregion
    }
}