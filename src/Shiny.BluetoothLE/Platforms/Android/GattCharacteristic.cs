using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Shiny.BluetoothLE.Internals;
using Shiny.Logging;
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
        IObservable<CharacteristicGattResult>? notifyOb;
        IObservable<IGattDescriptor>? descriptorOb;


        public GattCharacteristic(IGattService service,
                                  PeripheralContext context,
                                  BluetoothGattCharacteristic native)
                            : base(service,
                                   native.Uuid.ToGuid(),
                                   (CharacteristicProperties)(int)native.Properties)
        {
            this.context = context;
            this.native = native;
        }


        public override IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse = true) => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertWrite(false);

            var sub = this.context
                .Callbacks
                .CharacteristicWrite
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    Log.Write("BLE-Characteristic", "write event - " + args.Characteristic.Uuid);

                    if (args.IsSuccessful)
                        ob.Respond(new CharacteristicGattResult(this, value, withResponse ? CharacteristicResultType.Write : CharacteristicResultType.WriteWithoutResponse));
                    else
                        ob.OnError(new BleException($"Failed to write characteristic - {args.Status}"));
                });

            Log.Write("BLE-Characteristic", "Hooking for write response - " + this.Uuid);
            this.context.InvokeOnMainThread(() =>
            {
                try
                {
                    this.native.WriteType = withResponse ? GattWriteType.Default : GattWriteType.NoResponse;
                    var authSignedWrite =
                        this.native.Properties.HasFlag(CharacteristicProperties.AuthenticatedSignedWrites) &&
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
        }));


        public override IObservable<CharacteristicGattResult> Read() => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            this.AssertRead();

            var sub = this.context
                .Callbacks
                .CharacteristicRead
                .Where(this.NativeEquals)
                .Subscribe(args =>
                {
                    if (args.IsSuccessful)
                        ob.Respond(new CharacteristicGattResult(this, args.Characteristic.GetValue(), CharacteristicResultType.Read));
                    else
                        ob.OnError(new BleException($"Failed to read characteristic - {args.Status}"));
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
        }));



        public override IObservable<CharacteristicGattResult> Notify(bool sendHookEvent = false, bool enableIndicationsIfAvailable = false)
        {
            this.AssertNotify();

            return this.notifyOb ??= Observable.Create<CharacteristicGattResult>(ob =>
            {
                this.context
                    .Callbacks
                    .CharacteristicChanged
                    .Where(this.NativeEquals)
                    .Subscribe(args =>
                    {
                        if (args.IsSuccessful)
                            ob.OnNext(new CharacteristicGattResult(this, args.Characteristic.GetValue(), CharacteristicResultType.Notification));
                        else
                            ob.OnError(new BleException($"Notification error - {args.Status}"));
                    });

                this.EnableNotifications(enableIndicationsIfAvailable).Subscribe(
                    _ =>
                    {
                        if (sendHookEvent)
                            ob.OnNext(new CharacteristicGattResult(this, null, CharacteristicResultType.NotificationSubscribed));
                    },
                    ob.OnError
                );

                return () => this
                    .DisableNotifications()
                    .Subscribe(
                        _ => { },
                        ex => { }
                    );
            })
            .Publish()
            .RefCount();
        }


        public override IObservable<IGattDescriptor> DiscoverDescriptors()
            => this.descriptorOb ??= Observable.Create<IGattDescriptor>(ob =>
            {
                foreach (var nd in this.native.Descriptors)
                {
                    var wrap = new GattDescriptor(this, this.context, nd);
                    ob.OnNext(wrap);
                }
                return Disposable.Empty;
            })
            .Replay()
            .RefCount();


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

        IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationsIfAvailable) => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            void success()
            {
                this.IsNotifying = true;
                ob.Respond(new CharacteristicGattResult(this, null, CharacteristicResultType.NotificationSubscribed));
            };

            if (!this.context.Gatt.SetCharacteristicNotification(this.native, true))
                throw new BleException("Failed to set characteristic notification value");

            IDisposable? sub = null;
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
            {
                success();
            }
            else
            {
                var wrap = new GattDescriptor(this, this.context, descriptor);
                var bytes = this.GetNotifyDescriptorBytes(useIndicationsIfAvailable);
                sub = wrap
                    .WriteInternal(bytes)
                    .Subscribe(
                        _ => success(),
                        ex => success()
                    );
            }
            return () => sub?.Dispose();
        }));


        IObservable<CharacteristicGattResult> DisableNotifications() => this.context.Invoke(Observable.Create<CharacteristicGattResult>(ob =>
        {
            void success()
            {
                this.IsNotifying = false;
                ob.Respond(new CharacteristicGattResult(this, null, CharacteristicResultType.NotificationUnsubscribed));
            };
            if (!this.context.Gatt.SetCharacteristicNotification(this.native, false))
                throw new BleException("Could not set characteristic notification value");

            IDisposable? sub = null;
            var descriptor = this.native.GetDescriptor(NotifyDescriptorId);
            if (descriptor == null)
            {
                success();
            }
            else
            {
                var wrap = new GattDescriptor(this, this.context, descriptor);
                sub = wrap
                    .WriteInternal(BluetoothGattDescriptor.DisableNotificationValue.ToArray())
                    .Subscribe(
                        _ => success(),
                        ex => success()
                    );
            }

            return () => sub?.Dispose();
        }));

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