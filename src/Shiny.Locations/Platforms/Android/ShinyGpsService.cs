using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;


namespace Shiny.Locations
{
    [Service(
        Enabled = true,
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public class ShinyGpsService : ShinyAndroidForegroundService<IGpsManager, IGpsDelegate>
    {
        public static bool IsStarted { get; private set; }


        protected override void OnStart(Intent? intent)
        {
            this.Service
                .WhenReading()
                .SubscribeAsync(
                    reading => this.Delegates.RunDelegates(
                        x => x.OnReading(reading)
                    )
                )
                .DisposedBy(this.DestroyWith);

            IsStarted = true;
        }


        protected override void OnStop() => IsStarted = false;
        public override IBinder? OnBind(Intent? intent) => null;
    }
}
