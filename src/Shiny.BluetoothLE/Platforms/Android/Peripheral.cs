using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE.Internals;
using Android.Bluetooth;


namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral,
                              ICanDoTransactions,
                              ICanPairPeripherals,
                              ICanRequestMtu

    {
        readonly Subject<ConnectionState> connSubject;
        internal PeripheralContext Context { get; }


        public Peripheral(
            ManagerContext centralContext,
            BluetoothDevice native
        ) : base(
            native.Name,
            ToDeviceId(native.Address).ToString()
        )
        {
            this.connSubject = new Subject<ConnectionState>();
            this.Context = new PeripheralContext(centralContext, native);
        }


        public BluetoothDevice Native => this.Context.NativeDevice;
        public override ConnectionState Status => this.Context.Status;


        public override void Connect(ConnectionConfig? config)
        {
            this.connSubject.OnNext(ConnectionState.Connecting);
            this.Context.Connect(config);
        }


        public override void CancelConnection()
        {
            //this.connSubject.OnNext(ConnectionState.Disconnecting);
            this.Context.Close();
            this.connSubject.OnNext(ConnectionState.Disconnected);
        }


        public override IObservable<BleException> WhenConnectionFailed() => this.Context.ConnectionFailed;


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false) => this
            .GetServices()
            .Select(x => x.FirstOrDefault(y => y
                .Uuid
                .Equals(serviceUuid, StringComparison.InvariantCultureIgnoreCase)
            ))
            .Take(1)
            .Assert(serviceUuid, throwIfNotFound);


        public override IObservable<string> WhenNameUpdated() => this.Context
            .ManagerContext
            .ListenForMe(BluetoothDevice.ActionNameChanged, this)
            .Select(x => x.Name);


        public override IObservable<ConnectionState> WhenStatusChanged()
            => this.Context
                .Callbacks
                .ConnectionStateChanged
                .Select(x => x.NewState.ToStatus())
                .StartWith(this.Status)
                .Merge(this.connSubject);


        public override IObservable<IList<IGattService>> GetServices()
            => this.Context.Invoke(Observable.Create<IList<IGattService>>(ob =>
            {
                this.AssertConnection();
                var sub = this.Context
                    .Callbacks
                    .ServicesDiscovered
                    .Select(x => x.Gatt?.Services)
                    .Where(x => x != null)
                    .Select(x => x
                        .Select(native => new GattService(this, this.Context, native))
                        .Cast<IGattService>()
                        .ToList()
                    )
                    .Subscribe(
                        ob.Respond,
                        ob.OnError
                    );

                this.Context.Gatt.DiscoverServices();
                return sub;
            }));


        public override IObservable<int> ReadRssi()
        {
            this.AssertConnection();

            return this.Context.Invoke(
                this.Context
                    .Callbacks
                    .ReadRemoteRssi
                    .Take(1)
                    .Select(x =>
                    {
                        if (x.IsSuccessful)
                            throw new BleException("Failed to get RSSI - " + x.Status);

                        return x.Rssi;
                    })
            );
        }


        internal string? PairingRequestPin { get; set; }

        public IGattReliableWriteTransaction BeginReliableWriteTransaction() =>
            new GattReliableWriteTransaction(this.Context);


        public IObservable<bool> PairingRequest(string? pin = null) => Observable.Create<bool>(ob =>
        {
            var disp = new CompositeDisposable();

            if (this.PairingStatus == PairingState.Paired)
            {
                ob.Respond(true);
            }
            else
            {
                PairingRequestPin = pin;
                this.Context
                    .ManagerContext
                    .ListenForMe(ManagerContext.BlePairingFailed, this)
                    .Subscribe(_ => ob.Respond(false))
                    .DisposedBy(disp);

                this.Context
                    .ManagerContext
                    .ListenForMe(BluetoothDevice.ActionBondStateChanged, this)
                    .Where(x => this.Context.NativeDevice.BondState == Bond.Bonded)
                    .Subscribe(_ => ob.Respond(true))
                    .DisposedBy(disp);

                if (!this.Context.NativeDevice.CreateBond())
                    ob.Respond(false);
            }
            return disp;
        });


        public PairingState PairingStatus => this.Context.NativeDevice.BondState switch
        {
            Bond.Bonded => PairingState.Paired,
            _ => PairingState.NotPaired
        };


        int currentMtu = 20;
        public IObservable<int> RequestMtu(int size) => this.Context.Invoke(Observable.Create<int>(ob =>
        {
            this.AssertConnection();
            var sub = this.WhenMtuChanged().Skip(1).Take(1).Subscribe(ob.Respond);
            this.Context.Gatt.RequestMtu(size);
            return sub;
        }));


        public IObservable<int> WhenMtuChanged() => this.Context
            .Callbacks
            .MtuChanged
            .Where(x => x.IsSuccessful)
            .Select(x =>
            {
                this.currentMtu = x.Mtu;
                return x.Mtu;
            })
            .StartWith(this.currentMtu);


        public override int MtuSize => this.currentMtu;
        public override int GetHashCode() => this.Context.NativeDevice.GetHashCode();


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