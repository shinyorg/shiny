using System;
using System.Reactive.Linq;

using Android.Bluetooth;

namespace Shiny.Bluetooth
{
    public class ShinyBluetoothManager : IBluetoothManager, IBluetoothScanner
    {
        //https://github.com/harry1453/android-bluetooth-serial/blob/master/androidBluetoothSerial/src/main/java/com/harrysoft/androidbluetoothserial/BluetoothManager.kt
        public ShinyBluetoothManager()
        {
        }

        public IObservable<IBluetoothDevice> Scan() => Observable.Create<IBluetoothDevice>(ob =>
        {
            var ad = BluetoothAdapter.DefaultAdapter;
            ad.StartDiscovery();
            //.Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
            return () =>
            {
                ad.CancelDiscovery();
            };
        });
            //this.devices.Clear();

            //this.callbacks = new LollipopScanCallback(sr =>
            //{
            //    var scanResult = this.ToScanResult(sr.Device, sr.Rssi, new AdvertisementData(sr));
            //    ob.OnNext(scanResult);
            //});

            //var builder = new ScanSettings.Builder();
            //var scanMode = this.ToNative(config.ScanType);
            //builder.SetScanMode(scanMode);

            //var scanFilters = new List<ScanFilter>();
            //if (config.ServiceUuids != null && config.ServiceUuids.Count > 0)
            //{
            //    foreach (var uuid in config.ServiceUuids)
            //    {
            //        var parcel = new ParcelUuid(UUID.FromString(uuid));
            //        scanFilters.Add(new ScanFilter.Builder()
            //            .SetServiceUuid(parcel)
            //            .Build()
            //        );
            //    }
            //}

            //if (config.AndroidUseScanBatching && this.Manager.Adapter.IsOffloadedScanBatchingSupported)
            //    builder.SetReportDelay(100);

            //this.Manager.Adapter.BluetoothLeScanner.StartScan(
            //    scanFilters,
            //    builder.Build(),
            //    this.callbacks
            //);

            //return () => this.Manager.Adapter.BluetoothLeScanner?.StopScan(this.callbacks);
    }
}
