using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.Central
{
    public abstract class AbstractPeripheral : IPeripheral
    {
        protected AbstractPeripheral() {}
        protected AbstractPeripheral(string initialName, Guid uuid)
        {
            this.Name = initialName;
            this.Uuid = uuid;
        }


        public virtual string Name { get; protected set; }
        public virtual Guid Uuid { get; protected set; }
        public abstract ConnectionState Status { get; }
        public virtual int MtuSize { get; } = 20;
        public virtual IObservable<BleException> WhenConnectionFailed() => Observable.Empty<BleException>();

        public abstract void Connect(ConnectionConfig? config);
        public abstract void CancelConnection();
        public abstract IObservable<ConnectionState> WhenStatusChanged();
        public abstract IObservable<IGattService> DiscoverServices();
        public virtual IObservable<int> ReadRssi() => Observable.Empty<int>();
        public virtual IObservable<string> WhenNameUpdated() => throw new NotImplementedException("WhenNameUpdated is not supported on this platform");
        public virtual IObservable<IGattService> GetKnownService(Guid serviceUuid) => throw new NotImplementedException("GetKnownService is not supported on this platform");
    }
}
