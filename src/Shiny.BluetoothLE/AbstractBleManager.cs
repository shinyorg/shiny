using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractBleManager : IBleManager
    {
        public abstract IObservable<AccessState> RequestAccess(bool requestConnectPermission = true);
        public abstract IObservable<ScanResult> Scan(ScanConfig? config = null);
        public abstract void StopScan();

        public virtual bool IsScanning { get; protected set; }
        public virtual IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => throw new NotImplementedException("GetKnownPeripheral is not supported on this platform");
        public virtual IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null) => throw new NotImplementedException("GetConnectedPeripherals is not supported on this platform");
    }
}