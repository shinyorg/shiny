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


        public override ConnectionState Status => this
            .context
            .CentralContext
            .Manager
            .GetConnectionState(this.context.NativeDevice, ProfileType.Gatt)
            .ToStatus();


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
        //public override IObservable<BleException> WhenConnectionFailed() => this.context
        //    .Callbacks
        //    .ConnectionStateChanged
        //    .Where(x => !x.IsSuccessful)
        //    .Select(x => new BleException($"Failed to connect to peripheral - {x.Status}"));


        // android does not have a find "1" service - it must discover all services.... seems shit
        public override IObservable<IGattService> GetKnownService(Guid serviceUuid) => this
            .DiscoverServices()
            .Where(x => x.Uuid.Equals(serviceUuid))
            .Take(1)
            .Select(x => x);


        public override IObservable<string> WhenNameUpdated() => null;
        //this.context
        //    .CentralContext
        //    .Android
        //    .WhenDeviceNameChanged()
        //    .Where(x => x.Equals(this.context.NativeDevice))
        //    .Select(x => this.Name);


        public override IObservable<ConnectionState> WhenStatusChanged()
            => Observable.Create<ConnectionState>(ob => new CompositeDisposable(
                this.connSubject.Subscribe(ob.OnNext),
                this.context
                    .Callbacks
                    .ConnectionStateChanged
                    .Where(x => x.Gatt.Device.Address.Equals(this.context.NativeDevice.Address))
                    .Select(x => x.NewState.ToStatus())
                    //.DistinctUntilChanged()
                    .Subscribe(ob.OnNext)
            ));


        public override IObservable<IGattService> DiscoverServices()
            => Observable.Create<IGattService>(ob =>
            {
                var sub = this.context
                    .Callbacks
                    .ServicesDiscovered
                    .Where(x => x.Gatt.Device.Equals(this.context.NativeDevice))
                    .Subscribe(args =>
                    {
                        foreach (var ns in args.Gatt.Services)
                        {
                            var service = new GattService(this, this.context, ns);
                            ob.OnNext(service);
                        }
                        ob.OnCompleted();
                    });

                this.context.RefreshServices();
                this.context.Gatt.DiscoverServices();

                return sub;
            });


        public override IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
        {
            var sub = this.context
                .Callbacks
                .ReadRemoteRssi
                .Take(1)
                .Subscribe(x => ob.Respond(x.Rssi));

            this.context.Gatt?.ReadRemoteRssi();
            //if (this.context.Gatt?.ReadRemoteRssi() ?? false)
            //    ob.OnError(new BleException("Failed to read RSSI"));

            return sub;
        });


        public IObservable<bool> PairingRequest(string pin) => Observable.Create<bool>(ob =>
        {
            IDisposable requestOb = null;
            IDisposable istatusOb = null;

            if (this.PairingStatus == PairingState.Paired)
            {
                ob.Respond(true);
            }
            else
            {
                //if (pin != null && Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                //{
                //    requestOb = this.context
                //        .CentralContext
                //        .Android
                //        .WhenBondRequestReceived()
                //        .Where(x => x.Equals(this.context.NativeDevice))
                //        .Subscribe(x =>
                //        {
                //            var bytes = ConvertPinToBytes(pin);
                //            x.SetPin(bytes);
                //            x.SetPairingConfirmation(true);
                //        });
                //}
                //istatusOb = this.context
                //    .CentralContext
                //    .Android
                //    .WhenBondStatusChanged()
                //    .Where(x => x.Equals(this.context.NativeDevice) && x.BondState != Bond.Bonding)
                //    .Subscribe(x => ob.Respond(x.BondState == Bond.Bonded)); // will complete here

                // execute
                if (!this.context.NativeDevice.CreateBond())
                    ob.Respond(false);
            }
            return () =>
            {
                requestOb?.Dispose();
                istatusOb?.Dispose();
            };
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
            var sub = this.WhenMtuChanged().Skip(1).Subscribe(ob.Respond);
            this.context.Gatt.RequestMtu(size);
            return sub;
        });


        public IObservable<int> WhenMtuChanged() => this.context
            .Callbacks
            .MtuChanged
            .Where(x => x.Gatt.Equals(this.context.Gatt))
            .Select(x =>
            {
                this.currentMtu = x.Mtu;
                return x.Mtu;
            })
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