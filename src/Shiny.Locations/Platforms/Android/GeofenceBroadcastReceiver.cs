using System;
using Android.App;
using Android.Content;
using Shiny.Logging;
using static Android.Manifest;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = "com.shiny.locations.GeofenceBroadcastReceiver",
        Exported = true,
        DirectBootAware = true
    )]
    [IntentFilter(new [] {
        "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS",
        Permission.ReceiveBootCompleted
    })]
    public class GeofenceBroadcastReceiver : BroadcastReceiver
    {
        public const string INTENT_ACTION = "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS";


        public override async void OnReceive(Context context, Intent intent)
        {
            var pendingResult = this.GoAsync();
            try
            {
                var geofences = ShinyHost.Resolve<IAndroidGeofenceManager>();

                switch (intent.Action)
                {
                    case INTENT_ACTION:
                        await geofences.Process(intent);
                        break;

                    case Permission.ReceiveBootCompleted:
                        await geofences.ReceiveBoot();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                pendingResult.Finish();
            }
        }
    }
}
