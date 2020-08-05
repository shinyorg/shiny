using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
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
                                   native.Uuid,
                                   (CharacteristicProperties)native.CharacteristicProperties)
        {
            this.context = context;
            this.Native = native;
        }


        public Native Native { get; }


        IObservable<IGattDescriptor> descriptorOb;
        public override IObservable<IGattDescriptor> DiscoverDescriptors()
        {
            this.descriptorOb = this.descriptorOb ?? Observable.Create<IGattDescriptor>(async ob =>
            {
                var result = await this.Native.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
                //if (result.Status != GattCommunicationStatus.Success)
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


        // TODO: reliable write
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


        //public override IObservable<CharacteristicGattResult> EnableNotifications(bool useIndicationIfAvailable)
        //{
        //    var type = useIndicationIfAvailable && this.CanIndicate()
        //        ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
        //        : GattClientCharacteristicConfigurationDescriptorValue.Notify;

        //    return this.SetNotify(type);
        //}


        //public override IObservable<CharacteristicGattResult> DisableNotifications() =>
        //    this.SetNotify(GattClientCharacteristicConfigurationDescriptorValue.None);



        IObservable<CharacteristicGattResult> notifyOb;
        public override IObservable<CharacteristicGattResult> Notify(bool sendHookEvent, bool enableIndicationsIfAvailable)
        {
            lock (this)
            {
                this.notifyOb ??= Observable.Create<CharacteristicGattResult>(
                    async ob =>
                    {
                        TypedEventHandler<Native, GattValueChangedEventArgs> handler = (s, a) =>
                        {
                            var bytes = a.CharacteristicValue.ToArray();
                            var result = new CharacteristicGattResult(this, bytes, CharacteristicResultType.Notification);
                            ob.OnNext(result);
                        };
                        if ((this.Native.CharacteristicProperties & GattCharacteristicProperties.Notify) == GattCharacteristicProperties.Notify)
                        {
                            var gcs = await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                            if (gcs == GattCommunicationStatus.Success)
                            {
                                this.Native.ValueChanged += handler;
                            }
                            else
                            {
                                ob.OnError(new BleException($"Characteristic configuration error - {gcs}"));
                            }
                        }
                        else
                        {
                            ob.OnError(new BleException($"Notify is not supported - {this.Native.CharacteristicProperties}"));
                        }
                        return () =>
                        {
                            this.Native.ValueChanged -= handler;
                            ob.OnCompleted();
                        };
                    })
                    .Publish()
                    .RefCount();

                return this.notifyOb;
            }
        }


        internal async Task Disconnect()
        {
            await this.Native.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            //this.Native.ValueChanged -= this.OnValueChanged;
            //notifyOb?.
        }



        /*void OnValueChanged(Native sender, GattValueChangedEventArgs args)
        {
            if (sender.Equals(this.Native))
            {
                var bytes = args.CharacteristicValue.ToArray();
                var result = new CharacteristicGattResult(this, bytes, CharacteristicResultType.Notification);
                //this.currentOb.OnNext(result);
            }
        }*/
    }
}
