using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;


namespace Shiny.Locations
{
    [Service(
        Name = nameof(ShinyLocationService),
        Enabled = false,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public class ShinyLocationService : Service
    {
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
            => StartCommandResult.Sticky;

        public override IBinder? OnBind(Intent? intent) => null;
    }
}
