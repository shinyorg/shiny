using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Shiny.BluetoothLE.Internals;
using System.Collections.Generic;

namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral,
                              ICanDoTransactions,
                              ICanPairPeripherals
    {
        readonly PeripheralContext context;


        public Peripheral(ManagerContext managerContext, BluetoothLEDevice native)
        {
            this.context = new PeripheralContext(managerContext, this, native);
            this.Name = native.Name;
            this.Uuid = native.GetDeviceId().ToString();
        }


        public override void Connect(ConnectionConfig? config) => this.context.Connect();
        public override void CancelConnection() => this.context.Disconnect();
        public override ConnectionState Status => this.context.Status;
        public override IObservable<ConnectionState> WhenStatusChanged() => this.context.WhenStatusChanged();
        public override IObservable<int> ReadRssi() => Observable.Empty<int>();
        public override IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false) => Observable
            .FromAsync(async ct =>
            {
                var uuid = Utils.ToUuidType(serviceUuid);
                var result = await this.context.NativeDevice.GetGattServicesForUuidAsync(uuid, BluetoothCacheMode.Cached);
                if (result.Status != GattCommunicationStatus.Success)
                    throw new ArgumentException("Could not find GATT service - " + result.Status);

                var wrap = new GattService(this.context, result.Services.First());
                return wrap;
            })
            .Assert(serviceUuid, throwIfNotFound);


        public override IObservable<IList<IGattService>> GetServices() => Observable.FromAsync(async ct =>
        {
            var result = await this.context
                .NativeDevice
                .GetGattServicesAsync(BluetoothCacheMode.Uncached)
                .AsTask(ct)
                .ConfigureAwait(false);

            result.Status.Assert();
            return result
                .Services
                .Select(x => new GattService(this.context, x))
                .Cast<IGattService>()
                .ToList();
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