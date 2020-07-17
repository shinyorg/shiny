using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;


namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral,
                              ICanDoTransactions,
                              ICanPairPeripherals
    {
        readonly DeviceContext context;


        public Peripheral(CentralContext adapterContext, BluetoothLEDevice native)
        {
            this.context = new DeviceContext(adapterContext, this, native);
            this.Name = native.Name;
            this.Uuid = native.GetDeviceId();

        }


        public override void Connect(ConnectionConfig? config) => this.context.Connect();
        public override void CancelConnection() => this.context.Disconnect();
        public override ConnectionState Status => this.context.Status;
        public override IObservable<ConnectionState> WhenStatusChanged() => this.context.WhenStatusChanged();
        public override IObservable<int> ReadRssi() => Observable.Empty<int>();


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid) => Observable.FromAsync(async ct =>
        {
            var result = await this.context.NativeDevice.GetGattServicesForUuidAsync(serviceUuid, BluetoothCacheMode.Cached);
            if (result.Status != GattCommunicationStatus.Success)
                throw new ArgumentException("Could not find GATT service - " + result.Status);

            var wrap = new GattService(this.context, result.Services.First());
            return wrap;
        });


        public override IObservable<IGattService> DiscoverServices() => Observable.Create<IGattService>(async ob =>
        {
            var result = await this.context.NativeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            foreach (var nservice in result.Services)
            {
                var service = new GattService(this.context, nservice);
                ob.OnNext(service);
            }
            ob.OnCompleted();

            return Disposable.Empty;
        });


        IObservable<string> nameOb;
        public override IObservable<string> WhenNameUpdated()
        {
            this.nameOb = this.nameOb ?? Observable.Create<string>(ob =>
            {
                var handler = new TypedEventHandler<BluetoothLEDevice, object>(
                    (sender, args) => ob.OnNext(this.Name)
                );
                var sub = this.WhenConnected().Subscribe(_ =>
                    this.context.NativeDevice.NameChanged += handler
                );
                return () =>
                {
                    sub?.Dispose();
                    if (this.context.NativeDevice != null)
                        this.context.NativeDevice.NameChanged -= handler;
                };
            })
            .StartWith(this.Name)
            .Publish()
            .RefCount();

            return this.nameOb;
        }


        public IGattReliableWriteTransaction BeginReliableWriteTransaction() => new GattReliableWriteTransaction();


        public PairingState PairingStatus => this.context.NativeDevice.DeviceInformation.Pairing.IsPaired
            ? PairingState.Paired
            : PairingState.NotPaired;


        public IObservable<bool> PairingRequest(string? pin = null) => Observable.FromAsync(async ct =>
        {
            if (pin.IsEmpty())
            {
                var result = await this.context.NativeDevice.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);
                return result.Status == DevicePairingResultStatus.Paired;
            }

            var handler = new TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs>((sender, args) => 
            {
                switch (args.PairingKind)
                {
                    case DevicePairingKinds.ConfirmOnly:
                        args.Accept();
                        break;

                    case DevicePairingKinds.ProvidePin:
                        using (var def = args.GetDeferral())
                        {
                            args.Accept(pin);
                            def.Complete();
                        }
                        break;
                }
            });

            var pairingKind = pin.IsEmpty() 
                ? DevicePairingKinds.ConfirmOnly 
                : DevicePairingKinds.ProvidePin;

            try
            {
                this.context.NativeDevice.DeviceInformation.Pairing.Custom.PairingRequested += handler;
                var result = await this.context.NativeDevice.DeviceInformation.Pairing.Custom.PairAsync(pairingKind); 
                return result.Status == DevicePairingResultStatus.Paired;
            }
            finally
            {
                this.context.NativeDevice.DeviceInformation.Pairing.Custom.PairingRequested -= handler;
            }
        });
    }
}