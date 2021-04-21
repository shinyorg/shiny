using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public static partial class Extensions
    {
        /// <summary>
        /// This will scan until the peripheral a specific peripheral is found, then cancel the scan
        /// </summary>
        /// <param name="bleManager"></param>
        /// <param name="peripheralName"></param>
        /// <param name="includeLocalName"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ScanUntilPeripheralFound(this IBleManager bleManager, string peripheralName, bool includeLocalName = true) => bleManager
            .Scan()
            .Where(x =>
                x.Peripheral.Name?.Equals(peripheralName, StringComparison.OrdinalIgnoreCase) ?? false
                || (includeLocalName && (x.AdvertisementData?.LocalName?.Equals(peripheralName, StringComparison.InvariantCultureIgnoreCase) ?? false))
            )
            .Take(1)
            .Select(x => x.Peripheral);


        /// <summary>
        /// Scans only for distinct peripherals instead of repeating each peripheral scan response - this will only give you peripherals, not RSSI or ad packets
        /// </summary>
        /// <param name="bleManager"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ScanForUniquePeripherals(this IBleManager bleManager, ScanConfig? config = null) => bleManager
            .Scan(config)
            .Distinct(x => x.Peripheral.Uuid)
            .Select(x => x.Peripheral);


        /// <summary>
        /// This method wraps the traditional scan, but waits for the bleManager to be ready before initiating scan
        /// </summary>
        /// <param name="bleManager">The bleManager to scan with</param>
        /// <param name="restart">Stops any current scan running</param>
        /// <param name="config">ScanConfig parameters you would like to use</param>
        /// <returns></returns>
        public static IObservable<ScanResult> Scan(this IBleManager bleManager, ScanConfig? config = null, bool restart = false)
        {
            if (restart && bleManager.IsScanning)
                bleManager.StopScan(); // need a pause to wait for scan to end

            return bleManager.Scan(config);
        }


        /// <summary>
        /// Runs BLE scan for a set timespan then pauses for configured timespan before starting again
        /// </summary>
        /// <param name="bleManager"></param>
        /// <param name="scanTime"></param>
        /// <param name="scanPauseTime"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<ScanResult> ScanInterval(this IBleManager bleManager,
                                                            TimeSpan scanTime,
                                                            TimeSpan scanPauseTime,
                                                            ScanConfig? config = null) => Observable.Create<ScanResult>(ob =>
        {
            var scanObs = bleManager.Scan(config).Do(ob.OnNext, ob.OnError);
            IObservable<long>? scanPauseObs = null;
            IObservable<long>? scanStopObs = null;

            IDisposable? scanSub = null;
            IDisposable? scanStopSub = null;
            IDisposable? scanPauseSub = null;

            void Scan()
            {
                scanPauseSub?.Dispose();
                scanSub = scanObs.Subscribe();
                scanStopSub = scanStopObs.Subscribe();
            }

            scanPauseObs = Observable.Interval(scanPauseTime).Do(_ => Scan());
            scanStopObs = Observable.Interval(scanTime).Do(_ =>
            {
                scanSub?.Dispose();
                scanStopSub?.Dispose();
                scanPauseSub = scanPauseObs.Subscribe();
            });
            Scan(); // start initial scan

            return () =>
            {
                scanSub?.Dispose();
                scanStopSub?.Dispose();
                scanPauseSub?.Dispose();
            };
        });
    }
}
