using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Shiny.Infrastructure;

namespace Shiny.BluetoothLE;


public class BleManager : IBleManager, IShinyWebAssemblyService
{
    IJSInProcessObjectReference jsModule = null!;
    public async Task OnStart(IJSInProcessRuntime jsRuntime)
        => this.jsModule = await jsRuntime.ImportInProcess("Shiny.BluetoothLE.Blazor", "ble.js");


    public bool IsScanning { get; private set; }

    public IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null) => throw new NotImplementedException();
    public IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => throw new NotImplementedException();
    
    public IObservable<AccessState> RequestAccess() => throw new NotImplementedException();
    public IObservable<ScanResult> Scan(ScanConfig? config = null) => throw new NotImplementedException();
    public void StopScan() => throw new NotImplementedException();
}