using System;
using Shiny.Infrastructure;
using Android.App;
using Android.Content;
using Android.OS;


namespace Shiny.Beacons
{
    [Service(
        Name = "com.shiny.BeaconService",
        Exported = false
    )]
    public class BeaconService : Service
    {
        public override IBinder OnBind(Intent intent) => null;


        public override void OnCreate()
        {
            base.OnCreate();
            var beaconManager = ShinyHost.Resolve<IBeaconManager>();
            var beaconDelegate = ShinyHost.Resolve<IBeaconDelegate>();
            var repository = ShinyHost.Resolve<IRepository>();
            //var regions = await repo.GetAll<BeaconRegion>();
            //mgr.WhenBeaconRanged(regions[0]);
        }
    }
}
