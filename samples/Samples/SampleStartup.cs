using System;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.DryIoc;
using Prism.Ioc;
using Shiny;
using Shiny.Notifications;
using Shiny.Testing;
using Samples.Infrastructure;
using Samples.Jobs;
using Samples.HttpTransfers;
using Samples.Beacons;
using Samples.BluetoothLE;
using Samples.Geofences;
using Samples.Gps;
using Samples.Push;
using Samples.Notifications;
using Samples.Stores;

[assembly: GenerateStaticClasses("Samples")]


namespace Samples
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureLogging(ILoggingBuilder builder, IPlatform platform)
        {
            builder.AddConsole(opts =>
                opts.LogToStandardErrorThreshold = LogLevel.Debug
            );
            builder.AddSqliteLogging(LogLevel.Warning);
            //builder.AddFirebase(LogLevel.Warning);
            builder.AddAppCenter(Secrets.Values.AppCenterKey, LogLevel.Warning);
        }


        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        {
            services.UseSqliteStore();
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

            services.UseNotifications<NotificationDelegate>(null, new[] {
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
            });

            //services.UsePush<PushDelegate>();
            //services.UseFirebaseMessaging<PushDelegate>();
            services.UsePushAzureNotificationHubs<PushDelegate>(
                Secrets.Values.AzureNotificationHubListenerConnectionString,
                Secrets.Values.AzureNotificationHubName
            );
        }


        public override IServiceProvider CreateServiceProvider(IServiceCollection services)
        {
            // This registers and initializes the Container with Prism ensuring
            // that both Shiny & Prism use the same container
            ContainerLocator.SetContainerExtension(() => new DryIocContainerExtension());
            var container = ContainerLocator.Container.GetContainer();
            DryIocAdapter.Populate(container, services);
            return container.GetServiceProvider();
        }
    }
}
