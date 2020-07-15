using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Connectivity;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Display;

namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral,
                              ICanDoTransactions,
                              ICanPairPeripherals
    {
        readonly DeviceContext context;
        readonly ObservableBluetoothLEDevice observableDevice;


        public Peripheral(CentralContext adapterContext, BluetoothLEDevice native)
        {
            this.context = new DeviceContext(adapterContext, this, native);
            this.observableDevice = new ObservableBluetoothLEDevice(native.DeviceInformation);
            this.Name = native.Name;
            this.Uuid = native.GetDeviceId();

        }


        public override void Connect(ConnectionConfig? config) => this.context.Connect();
        public override void CancelConnection() => this.context.Disconnect();
        public override ConnectionState Status => this.context.Status;
        public override IObservable<ConnectionState> WhenStatusChanged() => this.context.WhenStatusChanged();
        public override IObservable<int> ReadRssi()
        {
            return Observable.Return<int>(0);
        }


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


        public override PairingState PairingStatus => this.context.NativeDevice.DeviceInformation.Pairing.IsPaired
            ? PairingState.Paired
            : PairingState.NotPaired;


        public IObservable<bool> PairingRequest(PairingConfiguration? configuration = null) => Observable.FromAsync(async token =>
        {
            var state = false;
            TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs> pairingRequestedHandler = (s, a) => 
            {
                switch(a.PairingKind)
                {
                    case DevicePairingKinds.ConfirmOnly:
                        {
                            a.Accept();
                        }
                        break;
                    case DevicePairingKinds.ProvidePin:
                        {
                            var collectPinDeferral = a.GetDeferral();
                            Task.Run(() =>
                            {
                                if (!string.IsNullOrEmpty(configuration?.Pin))
                                {
                                    a.Accept(configuration?.Pin);
                                }
                                collectPinDeferral.Complete();
                            });
                        }
                        break;
                    default:
                        {

                        }
                        break;
                }
            };
            var pairingKind = DevicePairingKinds.None;
            if(configuration != null)
            {
                if(!string.IsNullOrEmpty(configuration?.Pin))
                {
                    pairingKind = DevicePairingKinds.ProvidePin;
                }
                this.context.NativeDevice.DeviceInformation.Pairing.Custom.PairingRequested += pairingRequestedHandler;
                try
                {
                    state = (await this.context.NativeDevice.DeviceInformation.Pairing.Custom.PairAsync(pairingKind)).Status == DevicePairingResultStatus.Paired;
                }
                finally
                {
                    this.context.NativeDevice.DeviceInformation.Pairing.Custom.PairingRequested -= pairingRequestedHandler;
                }
            }
            else
            {
                state = (await this.context.NativeDevice.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None)).Status == DevicePairingResultStatus.Paired;
            }
            return state;
        });
    }
}