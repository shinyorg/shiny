using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.Central
{
    public static partial class Extensions
    {
        /// <summary>
        /// This will scan until the peripheral a specific peripheral is found, then cancel the scan
        /// </summary>
        /// <param name="centralManager"></param>
        /// <param name="deviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ScanUntilPeripheralFound(this ICentralManager centralManager, Guid deviceUuid) => centralManager
            .Scan()
            .Where(x => x.Peripheral.Uuid.Equals(deviceUuid))
            .Take(1)
            .Select(x => x.Peripheral);


        /// <summary>
        /// This will scan until the peripheral a specific peripheral is found, then cancel the scan
        /// </summary>
        /// <param name="centralManager"></param>
        /// <param name="deviceName"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ScanUntilPeripheralFound(this ICentralManager centralManager, string deviceName) => centralManager
            .Scan()
            .Where(x => x.Peripheral.Name?.Equals(deviceName, StringComparison.OrdinalIgnoreCase) ?? false)
            .Take(1)
            .Select(x => x.Peripheral);


        //public static IObservable<IScanResult> ScanTimed(this ICentralManager centralManager, TimeSpan scanTime, ScanConfig config = null) => centralManager
        //    .Scan(config)
        //    .Take(scanTime);

        /// <summary>
        /// Scans only for distinct peripherals instead of repeating each peripheral scan response - this will only give you peripherals, not RSSI or ad packets
        /// </summary>
        /// <param name="centralManager"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ScanForUniquePeripherals(this ICentralManager centralManager, ScanConfig config = null) => centralManager
            .Scan(config)
            .Distinct(x => x.Peripheral.Uuid)
            .Select(x => x.Peripheral);


        /// <summary>
        /// This method wraps the traditional scan, but waits for the centralManager to be ready before initiating scan
        /// </summary>
        /// <param name="centralManager">The centralManager to scan with</param>
        /// <param name="restart">Stops any current scan running</param>
        /// <param name="config">ScanConfig parameters you would like to use</param>
        /// <returns></returns>
        public static IObservable<IScanResult> Scan(this ICentralManager centralManager, ScanConfig config = null, bool restart = false)
        {
            if (restart && centralManager.IsScanning)
                centralManager.StopScan(); // need a pause to wait for scan to end

            return centralManager.Scan(config);
        }


        /// <summary>
        /// Runs BLE scan for a set timespan then pauses for configured timespan before starting again
        /// </summary>
        /// <param name="centralManager"></param>
        /// <param name="scanTime"></param>
        /// <param name="scanPauseTime"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IScanResult> ScanInterval(this ICentralManager centralManager, TimeSpan scanTime, TimeSpan scanPauseTime, ScanConfig config = null) => Observable.Create<IScanResult>(ob =>
        {
            var scanObs = centralManager.Scan(config).Do(ob.OnNext, ob.OnError);
            IObservable<long> scanPauseObs = null;
            IObservable<long> scanStopObs = null;

            IDisposable scanSub = null;
            IDisposable scanStopSub = null;
            IDisposable scanPauseSub = null;

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
