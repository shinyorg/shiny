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
        public virtual int MtuSize => 20;
        public virtual PairingState PairingStatus => PairingState.Unavailiable;
        public abstract object NativeDevice { get; }
        public abstract ConnectionState Status { get; }
        public virtual IObservable<BleException> WhenConnectionFailed() => Observable.Empty<BleException>();

        public abstract void Connect(ConnectionConfig config);
        public abstract void CancelConnection();
        public abstract IObservable<ConnectionState> WhenStatusChanged();
        public abstract IObservable<IGattService> DiscoverServices();
        public virtual IObservable<int> ReadRssi() => Observable.Empty<int>();
        public virtual IObservable<string> WhenNameUpdated() => throw new NotImplementedException("WhenNameUpdated is not supported on this platform");
        public virtual IObservable<IGattService> GetKnownService(Guid serviceUuid) => throw new NotImplementedException("GetKnownService is not supported on this platform");
		public virtual IObservable<bool> PairingRequest(string pin) => throw new ArgumentException("Pairing request is not supported on this platform");
        public virtual IObservable<int> RequestMtu(int size) => Observable.Return(this.MtuSize);
        public virtual IObservable<int> WhenMtuChanged() => Observable.Empty<int>();
        public virtual IGattReliableWriteTransaction BeginReliableWriteTransaction() => new VoidGattReliableWriteTransaction();
    }
}
