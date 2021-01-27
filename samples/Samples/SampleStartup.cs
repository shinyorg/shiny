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
using Shiny.Testing;
using Samples.Notifications;

[assembly: GenerateStaticClasses("Samples")]


namespace Samples
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // THESE LOGGERS ARE ONLY GOOD FOR FOREGROUND LEVEL DEBUG TESTING
            Log.UseConsole();
            Log.UseDebug();

            // YOU REALLY NEED TO USE ONE OF THE FOLLOWING LOGGERS TO TEST BACKGROUND PROCESSES FOR CRASHES AND IN PRODUCTION
            Log.UseFile();
            //services.UseAppCenterLogging(Constants.AppCenterTokens, true, false);
            services.UseSqliteLogging(true, true);
            services.UseNotificationErrorLogging(); // NEVER USE IN PRODUCTION - limited testing for background processes in dev

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
            services.UseSpeechRecognition();
            services.UseAllSensors();
            services.UseNfc();

            services.UseTestMotionActivity();
            //services.UseMotionActivity();

            services.UseGeofencing<GeofenceDelegate>();
            //services.UseGpsDirectGeofencing<LocationDelegates>();
            services.UseGps<GpsDelegate>();

            var channels = new [] {
                Channel.Create(
                    "Test",
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
            };

            // only pass channels to push or here, not both - technically you don't need this with push
            services.UseNotifications<NotificationDelegate>();

            //services.UsePush<PushDelegate>(channels);
            //services.UseFirebaseMessaging<PushDelegate>(channels);
            services.UsePushAzureNotificationHubs<PushDelegate>(
                Constants.AnhListenerConnectionString,
                Constants.AnhHubName,
                channels
            );
        }
    }
}
