using System;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Managed;

namespace Shiny.BluetoothLE;


public static class ManagedExtensions
{
    public static IManagedScan CreateManagedScanner(this IBleManager bleManager)
        => new ManagedScan(bleManager);


    public static IManagedPeripheral CreateManaged(this IPeripheral peripheral, IScheduler? scheduler = null)
        => new ManagedPeripheral(peripheral, scheduler);
}
