using System;
using System.Reactive.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using S = Shiny.Notifications;


namespace Shiny.Locations
{
    [Service(
        Enabled = true,
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public class ShinyGpsService : Service
    {
        public static bool IsStarted { get; private set; }
        IDisposable? cleanUp;


        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var context = ShinyHost.Resolve<IAndroidContext>();
            if (context.IsMinApiLevel(26))
            {
                var notificationManager = (S.NotificationManager)ShinyHost.Resolve<S.INotificationManager>();
                var config = ShinyHost.Resolve<IGpsManager>() as IGpsBackgroundNotificationConfiguration;

                var native = notificationManager.CreateNativeNotification(new S.Notification
                {
                    Title = config?.Title ?? "Shiny GPS Tracking",
                    Message = config?.Description ?? "Shiny GPS tracking is running",
                    Android = new S.AndroidOptions
                    {
                        ContentInfo = config?.Title ?? "Shiny GPS Tracking",
                        OnGoing = true,
                        //LightColor = Android.Graphics.Color.Blue,
                        Ticker = config?.Ticker ?? config?.Description ?? "Shiny GPS tracking is running",
                        Category = Notification.CategoryService
                    }
                });
                this.StartForeground(2, native);
            }
            IsStarted = true;

            var gps = ShinyHost.Resolve<IGpsManager>();

            var request = new GpsRequest(); // TODO: grab from intent
            var ob = gps.WhenReading();

            if (request.ThrottledInterval != null)
                ob.Sample(request.ThrottledInterval.Value);

            if (request.MinimumDistance != null)
            {
                ob
                    .WithPrevious()
                    .Where(x => x.Item1
                        .Position
                        .GetDistanceTo(x.Item2.Position).TotalMeters >= request.MinimumDistance.TotalMeters
                    )
                    .Select(x => x.Item2);
            }

            this.cleanUp = ob.SubscribeAsync(reading =>
                ShinyHost
                    .Container
                    .RunDelegates<IGpsDelegate>(
                        x => x.OnReading(reading)
                    )
            );

            return StartCommandResult.Sticky;
        }


        public override void OnDestroy()
        {
            IsStarted = false;

            this.cleanUp?.Dispose();
            var context = ShinyHost.Resolve<IAndroidContext>();
            if (context.IsMinApiLevel(26))
                this.StopForeground(true);

            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}
