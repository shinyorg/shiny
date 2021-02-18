using System;
using System.Reactive.Concurrency;
using Shiny.BluetoothLE.Managed;


namespace Shiny.BluetoothLE
{
    public static class ManagedExtensions
    {
        public static ManagedScan CreateManagedScanner(this IBleManager bleManager, IScheduler? scheduler = null, TimeSpan? clearTime = null)
            => new ManagedScan(bleManager, scheduler, clearTime);


        public static ManagedPeripheral CreateManaged(this IPeripheral peripheral, IScheduler? scheduler = null)
            => new ManagedPeripheral(peripheral, scheduler);
    }
}
