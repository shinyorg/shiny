using System;
using Samples.Settings;
using Samples.ShinyDelegates;
using Samples.Infrastructure;
using Samples.Jobs;
using Samples.HttpTransfers;
using Samples.Beacons;
using Samples.BluetoothLE;
using Samples.Geofences;
using Samples.Gps;
using Samples.Notifications;
using Samples.Push;
using Samples.DataSync;
using Samples.TripTracker;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Notifications;
using Shiny.Logging;


namespace Samples
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            Log.UseConsole();
            Log.UseDebug();

            //services.UseTestMotionActivity(Shiny.Locations.MotionActivityType.Automotive);

            //services.UseAppCenterLogging(Constants.AppCenterTokens, true, false);
            services.UseSqliteLogging(true, true);
            //services.UseSqliteSettings();
            //services.UseSqliteStorage();
            services.AddSingleton<AppNotifications>();
            services.AddSingleton<IDialogs, Dialogs>();

            // your infrastructure
            services.AddSingleton<SampleSqliteConnection>();
            services.AddSingleton<CoreDelegateServices>();
            services.AddSingleton<IAppSettings, AppSettings>();

            // startup tasks
            services.AddSingleton<GlobalExceptionHandler>();
            services.AddSingleton<JobLoggerTask>();

            // register all of the shiny stuff you want to use
            //services.UseJobForegroundService(TimeSpan.FromSeconds(30));
            services.UseHttpTransfers<HttpTransferDelegate>();
            services.UseBeaconRanging();
            services.UseBeaconMonitoring<BeaconDelegate>();
            services.UseBleClient<BleClientDelegate>();
            services.UseBleHosting();
            services.UseMotionActivity();
            services.UseSpeechRecognition();
            services.UseAllSensors();
            services.UseNfc();

            services.UseGeofencing<GeofenceDelegate>();
            //services.UseGpsDirectGeofencing<LocationDelegates>();
            services.UseGps<GpsDelegate>();

            services.UseNotifications();
            //services.UseNotifications<NotificationDelegate>(
            //    true,
            //    new NotificationCategory(
            //        "Test",
            //        new NotificationAction("Reply", "Reply", NotificationActionType.TextReply),
            //        new NotificationAction("Yes", "Yes", NotificationActionType.None),
            //        new NotificationAction("No", "No", NotificationActionType.Destructive)
            //    ),
            //    new NotificationCategory(
            //        "ChatName",
            //        new NotificationAction("Answer", "Answer", NotificationActionType.TextReply)
            //    ),
            //    new NotificationCategory(
            //        "ChatAnswer",
            //        new NotificationAction("yes", "Yes", NotificationActionType.None),
            //        new NotificationAction("no", "No", NotificationActionType.Destructive)
            //    )
            //);

            //services.UsePushNotifications<PushDelegate>();
            //services.UseFirebaseMessaging<PushDelegate>();
            services.UsePushAzureNotificationHubs<PushDelegate>(
                Constants.AnhListenerConnectionString,
                Constants.AnhHubName
            );

            //// app services
            services.UseGeofencingSync<LocationSyncDelegates>();
            services.UseGpsSync<LocationSyncDelegates>();
            services.UseTripTracker<SampleTripTrackerDelegate>();
            services.UseDataSync<SampleDataSyncDelegate>(true);
        }
    }
}
