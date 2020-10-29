using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using static Android.Manifest;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = GeofenceManagerImpl.ReceiverName,
        Enabled = true,
        Exported = true
    )]
    [IntentFilter(new [] {
        GeofenceManagerImpl.IntentAction,
        Permission.ReceiveBootCompleted
    })]
    public class GeofenceBroadcastReceiver : ShinyBroadcastReceiver
    {
        // startup tasks replace this, but this receiver is still used to trigger the wakeup on reboot
        protected override Task OnReceiveAsync(Context context, Intent intent) => this
            .Resolve<GeofenceProcessor>()
            .Process(intent);
    }
}
