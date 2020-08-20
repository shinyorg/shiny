using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using S = Shiny.Notifications;


namespace Shiny.Beacons
{
    [Service(
        Enabled = true,
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public class ShinyBeaconMonitoringService : Service
    {
        public static bool IsStarted { get; private set; }


        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var context = ShinyHost.Resolve<AndroidContext>();
            if (context.IsMinApiLevel(26))
            {
                var notificationManager = (S.NotificationManager)ShinyHost.Resolve<S.INotificationManager>();
                var native = notificationManager.CreateNativeNotification(new S.Notification
                {
                    Title = "Shiny Beacons",
                    Message = "Shiny beacon monitoring is on",
                    Android = new S.AndroidOptions
                    {
                        ContentInfo = "Shiny Beacons",
                        OnGoing = true,
                        //LightColor = Android.Graphics.Color.Blue,
                        Ticker = "Shiny Beacons",
                        Category = Notification.CategoryService
                    }
                });
                this.StartForeground(1, native);
            }
            IsStarted = true;

            ShinyHost.Container.Resolve<BackgroundTask>().StartScan();
            return StartCommandResult.Sticky;
        }


        public override void OnDestroy()
        {
            IsStarted = false;

            var context = ShinyHost.Resolve<AndroidContext>();
            if (context.IsMinApiLevel(26))
                this.StopForeground(true);

            ShinyHost.Container.Resolve<BackgroundTask>().StopScan();
            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}
