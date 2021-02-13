using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractPeripheral : IPeripheral
    {
        protected AbstractPeripheral() {}
        protected AbstractPeripheral(string initialName, string uuid)
        {
            this.Name = initialName;
            this.Uuid = uuid;
        }


        public virtual string Name { get; protected set; }
        public virtual string Uuid { get; protected set; }
        public abstract ConnectionState Status { get; }
        public virtual int MtuSize { get; } = 20;
        public virtual IObservable<BleException> WhenConnectionFailed() => Observable.Empty<BleException>();

        public abstract void Connect(ConnectionConfig? config);
        public abstract void CancelConnection();
        public abstract IObservable<ConnectionState> WhenStatusChanged();
        public abstract IObservable<IList<IGattService>> GetServices();

        public virtual IObservable<int> ReadRssi()
            => Observable.Empty<int>();

        public virtual IObservable<string> WhenNameUpdated()
            => throw new NotImplementedException("WhenNameUpdated is not supported on this platform");

        public virtual IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false)
            => throw new NotImplementedException("GetKnownService is not supported on this platform");
    }
}
