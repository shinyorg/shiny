using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;


namespace Shiny.Beacons
{
    [Service(
        Name = "com.shiny.BeaconService",
        Exported = false
    )]
    public class BeaconService : Service
    {
        public override IBinder OnBind(Intent intent) => null;


        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
            => StartCommandResult.NotSticky;


        public override void OnCreate()
        {
            base.OnCreate();

            ShinyHost
                .Resolve<BackgroundTask>()
                .Run();
        }
    }
}
