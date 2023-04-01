using System;
using System.Collections.Generic;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => throw new NotImplementedException();
    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => throw new NotImplementedException();

    public override void OnServicesDiscovered(BluetoothGatt gatt, GattStatus status) { }

    //public void RefreshServices()
    //{
    //    if (this.Gatt == null || !this.ManagerContext.Configuration.FlushServicesBetweenConnections)
    //        return;

    //    // https://stackoverflow.com/questions/22596951/how-to-programmatically-force-bluetooth-low-energy-service-discovery-on-android
    //    try
    //    {
    //        //Log.Warn(BleLogCategory.Device, "Try to clear Android cache");
    //        var method = this.Gatt.Class.GetMethod("refresh");
    //        if (method != null)
    //        {
    //            var result = (bool)method.Invoke(this.Gatt);
    //            this.Logger.LogWarning("Clear Internal Cache Refresh Result: " + result);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        this.Logger.LogWarning(ex, "Failed to clear internal device cache");
    //    }
    //}



    //    // android does not have a find "1" service - it must discover all services.... seems shit
    //    public override IObservable<IGattService?> GetKnownService(string serviceUuid, bool throwIfNotFound = false)
    //    {
    //        var uuid = Utils.ToUuidString(serviceUuid);

    //        return this
    //            .GetServices()
    //            .Select(x => x.FirstOrDefault(y => y
    //                .Uuid
    //                .Equals(uuid, StringComparison.InvariantCultureIgnoreCase)
    //            ))
    //            .Take(1)
    //            .Assert(serviceUuid, throwIfNotFound);
    //    }


    //    public override IObservable<IList<IGattService>> GetServices()
    //        => this.Context.Invoke(Observable.Create<IList<IGattService>>(ob =>
    //        {
    //            this.AssertConnection();
    //            var sub = this.Context
    //                .Callbacks
    //                .ServicesDiscovered
    //                .Select(x => x.Gatt!.Services)
    //                .Where(x => x != null)
    //                .Select(x => x!
    //                    .Select(native => new GattService(this, this.Context, native))
    //                    .Cast<IGattService>()
    //                    .ToList()
    //                )
    //                .Subscribe(
    //                    ob.Respond,
    //                    ob.OnError
    //                );

    //            this.Context.RefreshServices();
    //            this.Context.Gatt!.DiscoverServices();
    //            return sub;
    //        }));
}