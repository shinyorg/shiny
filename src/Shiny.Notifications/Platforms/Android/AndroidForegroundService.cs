//using System;


//namespace Shiny.Notifications.Platforms.Android
//{
//    public class AndroidForegroundService
//    {
//        public AndroidForegroundService(IPersistentNotification notification)
//        {

//        }
//    }
//}
//using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

using Shiny.Notifications;

using S = Shiny.Notifications;


namespace Shiny.Locations
{
    [Service(
        Enabled = true,
        Exported = true,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public abstract class ShinyAndroidForegroundService : Service
    {
        public static bool IsStarted { get; private set; }


        protected abstract void OnStart(IPersistentNotification notification);
        protected abstract void OnStop();

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var context = ShinyHost.Resolve<IAndroidContext>();
            if (context.IsMinApiLevel(26))
            {
                //var notificationManager = (S.NotificationManager)ShinyHost.Resolve<S.INotificationManager>();
                //var config = ShinyHost.Resolve<IGpsManager>() as IGpsBackgroundNotificationConfiguration;

                //var native = notificationManager.CreateNativeNotification(new S.Notification
                //{
                //    Title = config?.Title ?? "Shiny GPS Tracking",
                //    Message = config?.Description ?? "Shiny GPS tracking is running",
                //    Android = new S.AndroidOptions
                //    {
                //        ContentInfo = config?.Title ?? "Shiny GPS Tracking",
                //        OnGoing = true,
                //        //LightColor = Android.Graphics.Color.Blue,
                //        Ticker = config?.Ticker ?? config?.Description ?? "Shiny GPS tracking is running",
                //        Category = Notification.CategoryService
                //    }
                //});
                //this.StartForeground(2, native);
            }
            IsStarted = true;

            //var gps = ShinyHost.Resolve<IGpsManager>();
            //// TODO: start the listener here too?
            //this.cleanUp = gps
            //    .WhenReading()
            //    .SubscribeAsync(reading =>
            //        ShinyHost
            //            .Container
            //            .RunDelegates<IGpsDelegate>(
            //                x => x.OnReading(reading)
            //            )
            //    );

            this.OnStart(null);
            return StartCommandResult.Sticky;
        }


        public override void OnDestroy()
        {
            IsStarted = false;

            var context = ShinyHost.Resolve<IAndroidContext>();
            if (context.IsMinApiLevel(26))
                this.StopForeground(true);

            this.OnStop();
            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}