using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Central;


namespace Shiny.Testing.BluetoothLE.Central
{
    public class MockPeripheral : IPeripheral
    {
        public object NativeDevice { get; set; }
        public string Name { get; set; }
        public Guid Uuid { get; set; }
        public int MtuSize { get; set; }
        public PairingState PairingStatus { get; set; }
        public ConnectionState Status { get; set; }
        public MockGattReliableWriteTransaction Transaction { get; set; }

        public IGattReliableWriteTransaction BeginReliableWriteTransaction() => this.Transaction;
        public void CancelConnection() {}
        public void Connect(ConnectionConfig config = null) {}


        public IList<IGattService> Services { get; set; } = new List<IGattService>();
        public IObservable<IGattService> DiscoverServices() => this.Services.ToObservable();
        public IObservable<IGattService> GetKnownService(Guid serviceUuid)
            => this.DiscoverServices().Where(x => x.Uuid == serviceUuid);
        public IObservable<bool> PairingRequest(string pin = null) => Observable.Return(this.PairingStatus == PairingState.Paired);


        public int Rssi { get; set; }
        public IObservable<int> ReadRssi() => Observable.Return(this.Rssi);


        public int Mtu { get; set; }
        public IObservable<int> RequestMtu(int size) => Observable.Return(this.Mtu);
        public IObservable<BleException> WhenConnectionFailed() => Observable.Empty<BleException>();

        public Subject<int> MtuSubject { get; }
        public IObservable<int> WhenMtuChanged() => this.MtuSubject;

        public Subject<string> NameSubject { get; } = new Subject<string>();
        public IObservable<string> WhenNameUpdated() => this.NameSubject;

        public Subject<ConnectionState> ConnectionSubject { get; } = new Subject<ConnectionState>();
        public IObservable<ConnectionState> WhenStatusChanged() => this.ConnectionSubject;
    }
}
