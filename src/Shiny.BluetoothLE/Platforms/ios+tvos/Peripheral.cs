using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using CoreBluetooth;
using Foundation;
using Shiny.Logging;
using Shiny.BluetoothLE.Internals;


namespace Shiny.BluetoothLE
{
    public class Peripheral : AbstractPeripheral
    {
        readonly CentralContext context;
        IDisposable autoReconnectSub;


        public Peripheral(CentralContext context, CBPeripheral native) : base(native.Name, native.Identifier.ToGuid())
        {
            this.context = context;
            this.Native = native;
        }


        public CBPeripheral Native { get; }
        public override int MtuSize => (int)this
            .Native
            .GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);

        public override ConnectionState Status
        {
            get
            {
                switch (this.Native.State)
                {
                    case CBPeripheralState.Connected:
                        return ConnectionState.Connected;

                    case CBPeripheralState.Connecting:
                        return ConnectionState.Connecting;

                    case CBPeripheralState.Disconnecting:
                        return ConnectionState.Disconnecting;

                    case CBPeripheralState.Disconnected:
                    default:
                        return ConnectionState.Disconnected;
                }
            }
        }

        public override void Connect(ConnectionConfig config)
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


        public override IObservable<IGattService> GetKnownService(Guid serviceUuid)
            => Observable.Create<IGattService>(ob =>
            {
                var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
                {
                    if (this.Native.Services == null)
                        return;

                    foreach (var native in this.Native.Services)
                    {
                        var service = new GattService(this, native);
                        if (service.Uuid.Equals(serviceUuid))
                        {
                            ob.Respond(service);
                            break;
                        }
                    }
                });
                this.Native.DiscoveredService += handler;
                this.Native.DiscoverServices(new[] {serviceUuid.ToCBUuid()});

                return () => this.Native.DiscoveredService -= handler;
            });


        public override IObservable<IGattService> DiscoverServices() => Observable.Create<IGattService>(ob =>
        {
            Log.Write("BluetoothLe-Device", "service discovery hooked for peripheral " + this.Uuid);
            var services = new Dictionary<Guid, IGattService>();

            var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
            {
                if (args.Error != null)
                {
                    ob.OnError(new BleException(args.Error.LocalizedDescription));
                    return;
                }

                if (this.Native.Services == null)
                    return;

                foreach (var native in this.Native.Services)
                {
                    var service = new GattService(this, native);
                    if (!services.ContainsKey(service.Uuid))
                    {
                        services.Add(service.Uuid, service);
                        ob.OnNext(service);
                    }
                }
                ob.OnCompleted();
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