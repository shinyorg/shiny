using System;
using System.Linq;
using System.Collections.Generic;
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
        readonly PeripheralContext context;


        public GattCharacteristic(
            PeripheralContext context,
            Native native,
            IGattService service
        ) : base(
            service,
            native.Uuid.ToString(),
            (CharacteristicProperties)native.CharacteristicProperties
        )
        {
            this.context = context;
            this.Native = native;
        }


        public Native Native { get; }


        public override IObservable<IList<IGattDescriptor>> GetDescriptors() => Observable.FromAsync<IList<IGattDescriptor>>(async ct =>
        {
            var result = await this.Native
                .GetDescriptorsAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            result.Status.Assert();

            return result
                .Descriptors
                .Select(native => new GattDescriptor(native, this))
                .Cast<IGattDescriptor>()
                .ToList();
        });


        public override IObservable<GattCharacteristicResult> Write(byte[] value, bool withResponse) => Observable.FromAsync(async ct =>
        {
            this.AssertWrite(withResponse);

            var writeType = withResponse
                ? GattWriteOption.WriteWithResponse
                : GattWriteOption.WriteWithoutResponse;

            await this.Native
                .WriteValueAsync(value.AsBuffer(), writeType)
                .Execute(ct)
                .ConfigureAwait(false);

            return new GattCharacteristicResult(
                this,
                value,
                withResponse
                    ? GattCharacteristicResultType.Write
                    : GattCharacteristicResultType.WriteWithoutResponse
            );
        });


        public override IObservable<GattCharacteristicResult> Read() => Observable.FromAsync(async ct =>
        {
            this.AssertRead();
            var result = await this.Native
                .ReadValueAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            if (result.Status != GattCommunicationStatus.Success)
                throw new BleException($"Failed to read characteristic - {result.Status}");

            return new GattCharacteristicResult(
                this,
                result.Value?.ToArray(),
                GattCharacteristicResultType.Read
            );
        });


        public override IObservable<IGattCharacteristic> EnableNotifications(bool enable, bool useIndicationsIfAvailable) => Observable.FromAsync(async ct =>
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
            return this;
        });


        readonly List<TypedEventHandler<Native, GattValueChangedEventArgs>> handlers = new List<TypedEventHandler<Native, GattValueChangedEventArgs>>();
        public override IObservable<GattCharacteristicResult> WhenNotificationReceived() => Observable.Create<GattCharacteristicResult>(async ob =>
        {
            var handler = new TypedEventHandler<Native, GattValueChangedEventArgs>((sender, args) =>
            {
                if (sender.Equals(this.Native))
                {
                    var bytes = args.CharacteristicValue.ToArray();
                    var result = new GattCharacteristicResult(this, bytes, GattCharacteristicResultType.Notification);
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
