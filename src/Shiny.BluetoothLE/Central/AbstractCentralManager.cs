using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Central
{
    public abstract class AbstractCentralManager : ICentralManager
    {
        public abstract IObservable<AccessState> RequestAccess();
        public abstract IObservable<IScanResult> Scan(ScanConfig config = null);
        public abstract IObservable<AccessState> WhenStatusChanged();
        public abstract void StopScan();

        public virtual string AdapterName { get; protected set; }
        public virtual AccessState Status { get; protected set; }
        public virtual bool IsScanning { get; protected set; }
        public virtual IObservable<IPeripheral> GetKnownPeripheral(Guid deviceId) => throw new NotImplementedException("GetKnownPeripheral is not supported on this platform");
        public virtual IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(Guid? serviceUuid = null) => throw new NotImplementedException("GetConnectedPeripherals is not supported on this platform");
    }
}