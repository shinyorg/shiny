using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using Shiny.BluetoothLE.Central.Internals;
using Android.Bluetooth;
using Android.OS;


namespace Shiny.BluetoothLE.Central
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


        public override object NativeDevice => this.context.NativeDevice;
        public override ConnectionState Status => this.context.Status;


        public override void Connect(ConnectionConfig config)
        {
            this.connSubject.OnNext(ConnectionState.Connecting);
            this.context.Connect(config);
        }


        public override void CancelConnection()
        {
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
            => this.connSubject.Merge(this.context.Callbacks.ConnectionStateChanged.Select(x => x.NewState.ToStatus()));


        public override IObservable<IGattService> DiscoverServices()
            => Observable.Create<IGattService>(ob =>
            {
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
                
            this.context.Gatt?.ReadRemoteRssi();
            return sub;
        });


        public IObservable<bool> PairingRequest(string pin) => Observable.Create<bool>(ob =>
        {
            var composite = new CompositeDisposable();

            if (this.PairingStatus == PairingState.Paired)
                ob.Respond(true);

            else
            {
                if (pin != null && Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                {
                    composite.Add(this.context
                        .CentralContext
                        .ListenForMe(BluetoothDevice.ActionPairingRequest, this)
                        .Subscribe(x =>
                        {
                            var bytes = ConvertPinToBytes(pin);
                            this.context.NativeDevice.SetPin(bytes);
                            this.context.NativeDevice.SetPairingConfirmation(true);
                        })
                    );
                }
                composite.Add(this.context.CentralContext
                    .ListenForMe(BluetoothDevice.ActionBondStateChanged, this)
                    .Where(x => this.context.NativeDevice.BondState != Bond.Bonding)
                    .Subscribe(x => ob.Respond(this.PairingStatus == PairingState.Paired))
                );
                // execute
                if (!this.context.NativeDevice.CreateBond())
                    ob.Respond(false);
            }
            return composite;
        });


        public IGattReliableWriteTransaction BeginReliableWriteTransaction() =>
            new GattReliableWriteTransaction(this.context);


        public PairingState PairingStatus
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
        public IObservable<int> RequestMtu(int size) => Observable.Create<int>(ob =>
        {
            var sub = this.WhenMtuChanged().Skip(1).Take(1).Subscribe(ob.Respond);
            this.context.Gatt.RequestMtu(size);
            return sub;
        });


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

        public static byte[] ConvertPinToBytes(string pin)
        {
            var bytes = new List<byte>();
            foreach (var p in pin)
            {
                if (!char.IsDigit(p))
                    throw new ArgumentException("PIN contain invalid value - " + p);

                var value = byte.Parse(p.ToString());
                if (value > 10)
                    throw new ArgumentException("Invalid range for PIN value - " + value);

                bytes.Add(value);
            }
            return bytes.ToArray();
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