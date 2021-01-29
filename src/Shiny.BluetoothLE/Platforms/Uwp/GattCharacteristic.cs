using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Shiny.BluetoothLE.Internals;
using Native = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Shiny.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly DeviceContext context;


        public GattCharacteristic(DeviceContext context,
                                  Native native,
                                  IGattService service)
                            : base(service,
                                   native.Uuid.ToString(),
                                   (CharacteristicProperties)native.CharacteristicProperties)
        {
            this.context = context;
            this.Native = native;
        }


        public Native Native { get; }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> DiscoverDescriptors()
        {
            this.descriptorOb ??= Observable.Create<IGattDescriptor>(async ob =>
            {
                var result = await this.Native.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new BleException("Unable to request descriptors");

                foreach (var dnative in result.Descriptors)
                {
                    var descriptor = new GattDescriptor(dnative, this);
                    ob.OnNext(descriptor);
                }
                return Disposable.Empty;
            })
            .Replay();
            return this.descriptorOb;
        }


        public override IObservable<CharacteristicGattResult> Write(byte[] value, bool withResponse) => Observable.FromAsync(async ct =>
        {
            this.AssertWrite(withResponse);

            var writeType = withResponse
                ? GattWriteOption.WriteWithResponse
                : GattWriteOption.WriteWithoutResponse;

            var status = await this.Native
                .WriteValueAsync(value.AsBuffer(), writeType)
                .AsTask(ct)
                .ConfigureAwait(false);

            if (status != GattCommunicationStatus.Success)
                throw new BleException($"Failed to write characteristic - {status}");

            return new CharacteristicGattResult(
                this,
                value,
                withResponse
                    ? CharacteristicResultType.Write
                    : CharacteristicResultType.WriteWithoutResponse
            );
        });


        public override IObservable<CharacteristicGattResult> Read() => Observable.FromAsync(async ct =>
        {
            this.AssertRead();
            var result = await this.Native
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            if (result.Status != GattCommunicationStatus.Success)
                throw new BleException($"Failed to read characteristic - {result.Status}");

            return new CharacteristicGattResult(
                this,
                result.Value?.ToArray(),
                CharacteristicResultType.Read
            );
        });


        public override IObservable<Unit> EnableNotifications(bool enable, bool useIndicationsIfAvailable) => Observable.FromAsync(async ct =>
        {
            if (!enable)
            {
                await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                this.IsNotifying = false;
                this.context.SetNotifyCharacteristic(this);
            }
            else
            {
                var type = useIndicationsIfAvailable && this.CanIndicate()
                    ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                    : GattClientCharacteristicConfigurationDescriptorValue.Notify;

                var status = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(type);
                if (status != GattCommunicationStatus.Success)
                    throw new BleException($"Failed to write client characteristic configuration descriptor - {status}");

                this.IsNotifying = true;
                this.context.SetNotifyCharacteristic(this);
            }
        });


        readonly List<TypedEventHandler<Native, GattValueChangedEventArgs>> handlers = new List<TypedEventHandler<Native, GattValueChangedEventArgs>>();
        public override IObservable<CharacteristicGattResult> WhenNotificationReceived() => Observable.Create<CharacteristicGattResult>(async ob =>
        {
            var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
            {
                if (sender.Equals(this.Native))
                {
                    var bytes = args.CharacteristicValue.ToArray();
                    var result = new CharacteristicGattResult(this, bytes, CharacteristicResultType.Notification);
                    ob.OnNext(result);
                }
            });

            this.Native.ValueChanged += handler;
            this.handlers.Add(handler);

            this.context.SetNotifyCharacteristic(this);
            this.IsNotifying = true;

            return () =>
            {
                this.Native.ValueChanged -= handler;
                this.handlers.Remove(handler);
            };
        });


        internal async Task Disconnect()
        {
            await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            foreach (var handler in this.handlers)
                this.Native.ValueChanged -= handler;

            this.handlers.Clear();
        }
    }
}
