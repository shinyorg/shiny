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


        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var context = ShinyHost.Resolve<AndroidContext>();
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

            return StartCommandResult.Sticky;
        }


        public override void OnDestroy()
        {
            IsStarted = false;

            var context = ShinyHost.Resolve<AndroidContext>();
            if (context.IsMinApiLevel(26))
                this.StopForeground(true);

            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}
