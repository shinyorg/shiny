using System;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Internals;


namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral
    {
        readonly ManagerContext context;
        IDisposable? autoReconnectSub;


        public Peripheral(
            ManagerContext context,
            CBPeripheral native
        ) : base(
            native.Name,
            native.Identifier.ToString()
        )
        {
            this.context = context;
            this.Native = native;
        }


        public CBPeripheral Native { get; }
        public override int MtuSize => (int)this
            .Native
            .GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);


        public override ConnectionState Status => this.Native.State switch
        {
            CBPeripheralState.Connected => ConnectionState.Connected,
            CBPeripheralState.Connecting => ConnectionState.Connecting,
            CBPeripheralState.Disconnected => ConnectionState.Disconnected,
            CBPeripheralState.Disconnecting => ConnectionState.Disconnecting,
            _ => ConnectionState.Disconnected
        };


        public override void Connect(ConnectionConfig? config = null)
        {
            var arc = config?.AutoConnect ?? true;
            if (arc)
            {
                this.autoReconnectSub = this
                    .WhenDisconnected()
                    .Skip(1)
                    .Subscribe(_ => this.DoConnect());
            }
            this.DoConnect();
        }


        protected void DoConnect() => this.context
            .Manager
            .ConnectPeripheral(this.Native, new PeripheralConnectionOptions
            {
                NotifyOnDisconnection = true,
                NotifyOnConnection = true,
                NotifyOnNotification = true
            });


        public override void CancelConnection()
        {
            this.autoReconnectSub?.Dispose();
            this.context
                .Manager
                .CancelPeripheralConnection(this.Native);
        }


        public override IObservable<BleException> WhenConnectionFailed() => this.context
            .FailedConnection
            .Where(x => x.Peripheral.Equals(this.Native))
            .Select(x => new BleException(x.Error.ToString()));


        public override IObservable<string> WhenNameUpdated() => Observable.Create<string>(ob =>
        {
            ob.OnNext(this.Name);
            var handler = new EventHandler((sender, args) => ob.OnNext(this.Name));
            this.Native.UpdatedName += handler;

            return () => this.Native.UpdatedName -= handler;
        });


        public override IObservable<ConnectionState> WhenStatusChanged() => Observable.Create<ConnectionState>(ob =>
        {
            ob.OnNext(this.Status);
            return new CompositeDisposable(
                this.context
                    .PeripheralConnected
                    .Where(x => x.Equals(this.Native))
                    .Subscribe(x => ob.OnNext(this.Status)),

                //this.context
            //    .FailedConnection
            //    .Where(x => x.Equals(this.peripheral))
            //    .Subscribe(x => ob.OnNext(ConnectionStatus.Failed));

                this.context
                    .PeripheralDisconnected
                    .Where(x => x.Equals(this.Native))
                    .Subscribe(x => ob.OnNext(this.Status))
            );
        });


        public override IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false)
            => Observable.Create<IGattService?>(ob =>
            {
                var nativeUuid = CBUUID.FromString(serviceUuid);
                var service = this.TryGetService(nativeUuid);
                if (service != null)
                {
                    ob.Respond(service);
                    return Disposable.Empty;
                }
                var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
                {
                    if (this.Native.Services == null)
                        return;

                    var service = this.TryGetService(nativeUuid);
                    ob.Respond(service);
                });

                this.Native.DiscoveredService += handler;
                this.Native.DiscoverServices(new [] { nativeUuid });

                return Disposable.Create(() => this.Native.DiscoveredService -= handler);
            })
            .Assert(serviceUuid, throwIfNotFound);


        IGattService? TryGetService(CBUUID nativeUuid)
        {
            var services = this.Native.Services?.ToList() ?? new List<CBService>(0);
            var service = services.FirstOrDefault(x => x.UUID.Equals(nativeUuid));

            if (service == null)
                return null;

            return new GattService(this, service);
        }


        public override IObservable<IList<IGattService>> GetServices() => Observable.Create<IList<IGattService>>(ob =>
        {
            var services = new Dictionary<string, IGattService>();

            var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
            {
                if (args.Error != null)
                {
                    ob.OnError(new BleException(args.Error.LocalizedDescription));
                    return;
                }
                else if (this.Native.Services != null)
                {
                    var list = this.Native
                        .Services
                        .Select(native => new GattService(this, native))
                        .Cast<IGattService>()
                        .ToList();

                    ob.Respond(list);
                }
            });
            this.Native.DiscoveredService += handler;
            this.Native.DiscoverServices();

            return () => this.Native.DiscoveredService -= handler;
        });


        public override IObservable<int> ReadRssi() => Observable.Create<int>(ob =>
        {
            var handler = new EventHandler<CBRssiEventArgs>((sender, args) =>
            {
                if (args.Error == null)
                    ob.Respond(args.Rssi?.Int32Value ?? 0);
                else
                    ob.OnError(new Exception(args.Error.LocalizedDescription));
            });
            this.Native.RssiRead += handler;
            this.Native.ReadRSSI();

            return () => this.Native.RssiRead -= handler;
        });


        public override string ToString() => this.Uuid.ToString();
        public override int GetHashCode() => this.Native.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is Peripheral other)
                return this.Native.Equals(other.Native);

            return false;
        }
    }
}