namespace Shiny.BluetoothLE.AccessorySetup.Tests;


class NullBleManager : IBleManager
{
    public AccessState CurrentAccess => AccessState.Available;
    public bool IsScanning => false;
    public string? LastLookup { get; private set; }

    public IObservable<AccessState> RequestAccess() => throw new NotImplementedException();
    public IPeripheral? GetKnownPeripheral(string peripheralUuid)
    {
        this.LastLookup = peripheralUuid;
        return null;
    }
    public void StopScan() { }
    public IEnumerable<IPeripheral> GetConnectedPeripherals() => [];
    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => throw new NotImplementedException();
}
