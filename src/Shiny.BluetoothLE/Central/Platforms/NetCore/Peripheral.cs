using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Shiny.BluetoothLE.Central;
using DBus;
using Mono.BlueZ.DBus;
using Plugin.BluetoothLE.Infrastructure;


namespace Acr.BluetoothLE.Central
{
    public class Peripheral : AbstractPeripheral
    {
        readonly Device1 native;


        public Peripheral(Device1 native)
        {
            this.native = native;
        }


        public override ConnectionStatus Status
            => this.native.Connected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;


        public override IObservable<object> Connect(GattConnectionConfig config) => Observable.Create<object>(ob =>
        {
            this.native.Connect();
            return () =>
            {
            };
        });


        public override void CancelConnection()
        {
            this.native.Disconnect();
            base.CancelConnection();
        }


        public override IObservable<int> WhenRssiUpdated(TimeSpan? timeSpan) => Observable
            .Interval(timeSpan ?? TimeSpan.FromSeconds(1))
            .Select(_ => (int)this.native.RSSI);


        public override IObservable<bool> PairingRequest(string pin) => Observable.Create<bool>(ob =>
        {
            this.native.Pair();
            this.native.Trusted = true;

            return () => { };
        });


        public override PairingState PairingStatus => this.native.Paired
            ? PairingStatus.Paired
            : PairingStatus.NotPaired;


        public override IObservable<ConnectionState> WhenStatusChanged() => Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Select(_ => this.Status)
            .DistinctUntilChanged();


        public override IObservable<IGattService> WhenServiceDiscovered() => Observable.Create<IGattService>(ob =>
        {
            // TODO: refresh per connection
            foreach (var path in this.native.GattServices)
            {
                var service = Bus.System.GetObject<GattService1>(BlueZPath.Service, path);
                var acr = new GattService(service, this);
                ob.OnNext(acr);
            }
            return Disposable.Empty;
        });


        public override BleFeatures Features => BleFeatures.PairingRequests;
        public override object NativeDevice => this.native;
    }
}
