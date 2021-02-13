using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE;


namespace Shiny.Testing.BluetoothLE
{
    public class TestPeripheral : IPeripheral
    {
        public TestPeripheral(string uuid) => this.Uuid = uuid;

        
        public string Uuid { get; }


        readonly Subject<string> nameSubj = new Subject<string>();
        string name;
        public string Name
        {
            get => this.name;
            set
            {
                this.name = value;
                this.nameSubj.OnNext(value);
            }
        }


        readonly Subject<ConnectionState> connSubj = new Subject<ConnectionState>();
        ConnectionState connectionStatus = ConnectionState.Disconnected;
        public ConnectionState Status
        {
            get => this.connectionStatus;
            set
            {
                this.connectionStatus = value;
                this.connSubj.OnNext(value);
            }
        }


        public int MtuSize => 20;
        public void CancelConnection()
            => this.Status = ConnectionState.Disconnected;

        public void Connect(ConnectionConfig? config = null)
            => this.Status = ConnectionState.Connected;


        public List<IGattService> Services { get; set; } = new List<IGattService>();

        public IObservable<IList<IGattService>> GetServices()
            => this.Services.Cast<IList<IGattService>>().ToObservable();

        public IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false)
            => Observable.Return(this.Services.FirstOrDefault(x => x.Uuid == serviceUuid)).Do(ch =>
            {
                if (ch == null && throwIfNotFound)
                    throw new ArgumentException($"Service not found - {serviceUuid}");
            });


        public Subject<int> RssiSubject { get; } = new Subject<int>();
        public IObservable<int> ReadRssi() => this.RssiSubject;
        public Subject<BleException> ConnectionFailedSubject { get; } = new Subject<BleException>();
        public IObservable<BleException> WhenConnectionFailed() => this.ConnectionFailedSubject;
        public IObservable<string> WhenNameUpdated() => this.nameSubj;
        public IObservable<ConnectionState> WhenStatusChanged() => this.connSubj;
    }
}
