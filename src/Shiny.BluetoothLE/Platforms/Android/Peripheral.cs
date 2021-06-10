using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Bluetooth;
using Shiny.BluetoothLE.Internals;


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
            this.WhenStatusChanged().Subscribe(x => this.status = x);
        }


        public BluetoothDevice Native => this.Context.NativeDevice;


        ConnectionState? status;
        public override ConnectionState Status => this.status ?? this.Context.Status;


        public override void Connect(ConnectionConfig? config)
        {
            this.connSubject.OnNext(ConnectionState.Connecting);
            this.Context.Connect(config);
        }


        public override void CancelConnection()
        {
            this.Context.Close();
            this.connSubject.OnNext(ConnectionState.Disconnected);
        }


        public override IObservable<BleException> WhenConnectionFailed() => this.Context.ConnectionFailed;


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false)
        {
            var uuid = Utils.ToUuidString(serviceUuid);

            return this
                .GetServices()
                .Select(x => x.FirstOrDefault(y => y
                    .Uuid
                    .Equals(uuid, StringComparison.InvariantCultureIgnoreCase)
                ))
                .Take(1)
                .Assert(serviceUuid, throwIfNotFound);
        }


        public override IObservable<string> WhenNameUpdated() => this.Context
            .ManagerContext
            .ListenForMe(BluetoothDevice.ActionNameChanged, this)
            .Select(_ => this.Name);


        public override IObservable<ConnectionState> WhenStatusChanged() => Observable.Create<ConnectionState>(ob =>
        {
            var comp = new CompositeDisposable();
            ob.OnNext(this.status ?? this.Context.Status);

            this.Context
                .Callbacks
                .ConnectionStateChanged
                .Select(x => x.NewState.ToStatus())
                .Subscribe(ob.OnNext)
                .DisposedBy(comp);

            this.connSubject
                .Subscribe(ob.OnNext)
                .DisposedBy(comp);

            return comp;
        });



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
                this.Context
                    .ManagerContext
                    .ListenForMe(this)
                    .Subscribe(intent =>
                    {

                        switch (intent.Action)
                        {
                            case BluetoothDevice.ActionAclConnected:
                                // if we're getting a connection for this device here, the bonding should be good
                                var success = this.PairingStatus == PairingState.Paired;
                                ob.Respond(success);
                                break;

                            case BluetoothDevice.ActionBondStateChanged:
                                var prev = (Bond)intent.GetIntExtra(BluetoothDevice.ExtraPreviousBondState, (int)Bond.None);
                                var current = (Bond)intent.GetIntExtra(BluetoothDevice.ExtraBondState, (int)Bond.None);

                                if (prev == Bond.Bonding || current == Bond.Bonded)
                                {
                                    // it is done now
                                    var bond = current == Bond.Bonded;
                                    ob.Respond(bond);
                                }

                                break;

                            case BluetoothDevice.ActionPairingRequest:
                                if (!pin.IsEmpty())
                                {
                                    var bytes = Encoding.UTF8.GetBytes(pin);
                                    if (!this.Native.SetPin(bytes))
                                    {
                                        ob.OnError(new ArgumentException("Failed to set PIN"));
                                    }
                                }
                                break;
                        }
                    })
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