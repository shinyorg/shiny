using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE.Internals;
using Android.Bluetooth;
using Android.Media;
using Android.Content.Res;

namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral,
                              ICanDoTransactions,
                              ICanPairPeripherals,
                              ICanRequestMtu

    {
        readonly Subject<ConnectionState> connSubject;
        readonly DeviceContext context;


        public Peripheral(CentralContext centralContext, BluetoothDevice native)
            : base(native.Name, ToDeviceId(native.Address))
        {
            this.connSubject = new Subject<ConnectionState>();
            this.context = new DeviceContext(centralContext, native);
        }


        public BluetoothDevice Native => this.context.NativeDevice;
        public override ConnectionState Status => this.context.Status;


        public override void Connect(ConnectionConfig? config)
        {
            this.connSubject.OnNext(ConnectionState.Connecting);
            this.context.Connect(config);
        }


        public override void CancelConnection()
        {
            //this.connSubject.OnNext(ConnectionState.Disconnecting);
            this.context.Close();
            this.connSubject.OnNext(ConnectionState.Disconnected);
        }


        public override IObservable<BleException> WhenConnectionFailed() => this.context.ConnectionFailed;


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService> GetKnownService(Guid serviceUuid) => this
            .DiscoverServices()
            .Where(x => x.Uuid.Equals(serviceUuid))
            .Take(1)
            .Select(x => x);


        public override IObservable<string> WhenNameUpdated() => this.context
            .CentralContext
            .ListenForMe(BluetoothDevice.ActionNameChanged, this)
            .Select(x => x.Name);


        public override IObservable<ConnectionState> WhenStatusChanged()
            => this.context
                .Callbacks
                .ConnectionStateChanged
                .Select(x => x.NewState.ToStatus())
                .StartWith(this.Status)
                .Merge(this.connSubject);


        public override IObservable<IGattService> DiscoverServices()
            => Observable.Create<IGattService>(ob =>
            {
                this.AssertConnection();

                var sub = this.context.Callbacks.ServicesDiscovered.Subscribe(cb =>
                {
                    foreach (var ns in cb.Gatt.Services)
                    {
                        var service = new GattService(this, this.context, ns);
                        ob.OnNext(service);
                    }
                    ob.OnCompleted();
                });

                //this.context.RefreshServices();
                this.context.Gatt.DiscoverServices();

                return sub;
            });


        public override IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
        {
            this.AssertConnection();

            var sub = this.context
                .Callbacks
                .ReadRemoteRssi
                .Take(1)
                .Subscribe(cb =>
                {
                    if (cb.IsSuccessful)
                        ob.Respond(cb.Rssi);
                    else
                        ob.OnError(new BleException("Failed to get RSSI - " + cb.Status));
                });

            this.context.Gatt.ReadRemoteRssi();
            return sub;
        });


        public IGattReliableWriteTransaction BeginReliableWriteTransaction() =>
            new GattReliableWriteTransaction(this.context);


        public IObservable<bool> PairingRequest(PairingConfiguration? configuration = null) => Observable.Create<bool>(ob =>
        {
            IDisposable? sub = null;
            IDisposable pairingRequestSubscription = null;
            if (this.PairingStatus == PairingState.Paired)
            {
                ob.Respond(true);
            }
            else
            {
                sub = this.context
                    .CentralContext
                    .ListenForMe(BluetoothDevice.ActionBondStateChanged, this)
                    .Where(x => this.context.NativeDevice.BondState != Bond.Bonding)
                    .Subscribe(x => ob.Respond(this.PairingStatus == PairingState.Paired));
                pairingRequestSubscription = this.context
                    .CentralContext
                    .ListenForMe(BluetoothDevice.ActionPairingRequest, this)
                    .OfType<Peripheral>()
                    .Subscribe((p) => 
                    {
                        if(!string.IsNullOrEmpty(configuration?.Pin))
                        {
                            p.context.NativeDevice.SetPin(System.Text.Encoding.ASCII.GetBytes(configuration?.Pin) );
                        }
                    }, (ex)=>pairingRequestSubscription?.Dispose(), ()=>pairingRequestSubscription?.Dispose());
                // execute
                if (!this.context.NativeDevice.CreateBond())
                    ob.Respond(false);
            }
            return () =>
            {
                sub?.Dispose();
                pairingRequestSubscription?.Dispose();
            };
        });


        public override PairingState PairingStatus
        {
            get
            {
                switch (this.context.NativeDevice.BondState)
                {
                    case Bond.Bonded:
                        return PairingState.Paired;

                    default:
                    case Bond.None:
                        return PairingState.NotPaired;
                }
            }
        }


        int currentMtu = 20;
        public IObservable<int> RequestMtu(int size) => this.context.Invoke(Observable.Create<int>(ob =>
        {
            this.AssertConnection();
            var sub = this.WhenMtuChanged().Skip(1).Take(1).Subscribe(ob.Respond);
            this.context.Gatt.RequestMtu(size);
            return sub;
        }));


        public IObservable<int> WhenMtuChanged() => Observable
            .Create<int>(ob => this.context
                .Callbacks
                .MtuChanged
                .Subscribe(cb =>
                {
                    if (!cb.IsSuccessful)
                        ob.OnError(new BleException("Failed to request MTU - " + cb.Status));
                    else
                    {
                        this.currentMtu = cb.Mtu;
                        ob.Respond(cb.Mtu);
                    }
                })
            )
            .StartWith(this.currentMtu);


        public override int MtuSize => this.currentMtu;
        public override int GetHashCode() => this.context.NativeDevice.GetHashCode();


        public override bool Equals(object obj)
        {
            var other = obj as Peripheral;
            if (other == null)
                return false;

            if (!Object.ReferenceEquals(this, other))
                return false;

            return true;
        }


        public override string ToString() => $"Peripheral: {this.Uuid}";


        #region Internals

        void AssertConnection()
        {
            if (this.Status != ConnectionState.Connected)
                throw new ArgumentException("Peripheral is not connected");
        }


        // thanks monkey robotics
        protected static Guid ToDeviceId(string address)
        {
            var deviceGuid = new byte[16];
            var mac = address.Replace(":", "");
            var macBytes = Enumerable
                .Range(0, mac.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
                .ToArray();

            macBytes.CopyTo(deviceGuid, 10);
            return new Guid(deviceGuid);
        }

        #endregion
    }
}