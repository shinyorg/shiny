using System;
using Samples.Settings;
using Samples.Infrastructure;
using Samples.Jobs;
using Samples.HttpTransfers;
using Samples.Beacons;
using Samples.BluetoothLE;
using Samples.Geofences;
using Samples.Gps;
using Samples.Push;
using Microsoft.Extensions.DependencyInjection;
using Shiny;
using Shiny.Notifications;
using Shiny.Logging;
using Samples.Notifications;

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
            services.UseNotifications<NotificationDelegate>(
                true,
                null,
                Channel.Create(
                    "Test",
                    ChannelAction.Create("Reply", ChannelActionType.TextReply),
                    ChannelAction.Create("Reply", ChannelActionType.TextReply),
                    ChannelAction.Create("Yes"),
                    ChannelAction.Create("No", ChannelActionType.Destructive)
                ),
                Channel.Create(
                    "ChatName",
                    ChannelAction.Create("Answer", ChannelActionType.TextReply)
                ),
                Channel.Create(
                    "ChatAnswer",
                    ChannelAction.Create("Yes"),
                    ChannelAction.Create("No", ChannelActionType.Destructive)
                )
            );

            //services.UsePushNotifications<PushDelegate>();
            //services.UseFirebaseMessaging<PushDelegate>();
            //services.UsePushAzureNotificationHubs<PushDelegate>(
            //    Constants.AnhListenerConnectionString,
            //    Constants.AnhHubName
            //);
        }
    }
}
