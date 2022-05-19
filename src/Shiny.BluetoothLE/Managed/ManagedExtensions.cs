using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Managed;

namespace Shiny.BluetoothLE;


public static class ManagedExtensions
{
    public static IManagedScan CreateManagedScanner(this IBleManager bleManager, IScheduler? scheduler = null, TimeSpan? clearTime = null, ScanConfig? scanConfig = null)
        => new ManagedScan(bleManager, scanConfig, scheduler, clearTime);


    public static IManagedPeripheral CreateManaged(this IPeripheral peripheral, IScheduler? scheduler = null)
        => new ManagedPeripheral(peripheral, scheduler);


    public static async Task<bool> Toggle(this IManagedScan scan)
    {
        if (scan.IsScanning)
            scan.Stop();
        else
            await scan.Start().ConfigureAwait(false);

        return scan.IsScanning;
    }
}
