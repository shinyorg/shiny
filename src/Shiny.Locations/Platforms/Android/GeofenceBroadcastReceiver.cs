using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Gms.Location;
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
        public static Func<GeofencingEvent, Task>? Process { get; set; }

        // startup tasks replace this, but this receiver is still used to trigger the wakeup on reboot
        protected override async Task OnReceiveAsync(Context? context, Intent? intent)
        {
            var e = GeofencingEvent.FromIntent(intent);
            if (e != null && Process != null)
                await Process(e);
        }
    }
}
