using System;
using System.Reactive.Concurrency;
using Shiny.BluetoothLE.Managed;


namespace Shiny.BluetoothLE
{
    public static class ManagedExtensions
    {
        //public static ManagedScanWrapper<T> CreateManagedScan<T>(this IBleManager bleManager, ScanConfig scanConfig, IScheduler ? scheduler = null, TimeSpan? clearTime = null)
        //{
        //    var managed = bleManager.CreateManagedScanner(scheduler, clearTime, scanConfig);
        //    return new ManagedScanWrapper<T>
        //}


        public static IManagedScan CreateManagedScanner(this IBleManager bleManager, IScheduler? scheduler = null, TimeSpan? clearTime = null, ScanConfig? scanConfig = null)
            => new ManagedScan(bleManager, scanConfig, scheduler, clearTime);


        public static IManagedPeripheral CreateManaged(this IPeripheral peripheral, IScheduler? scheduler = null)
            => new ManagedPeripheral(peripheral, scheduler);
    }
}
