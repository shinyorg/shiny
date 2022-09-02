using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Infrastructure;
using Shiny;
using System;


namespace Sample
{
    public class Startup : ShinyStartup
    {
        public Action<IServiceCollection, IConfiguration>? RegisterPlatform { get; set; }


        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        {
            var config = new ConfigurationBuilder()
                .AddJsonPlatformBundle("appsettings.json", false)
                .Build();

            // we inject our db so we can use it in our shiny background events to store them for display later
            services.AddSingleton<SampleSqliteConnection>();
            services.AddSingleton<SampleApi>();

#if AZURE
            services.UsePushAzureNotificationHubs<MyPushDelegate>(
                config["AzureNotificationHubs:ListenerConnectionString"],
                config["AzureNotificationHubs:HubName"]
            );
#elif ONESIGNAL
            services.UseOneSignalPush<MyPushDelegate>(config["OneSignal:AppId"]);
#elif FIREBASE            
            // to try direct firebase configuration, set the Firebase section under appsettings.json, comment out the line below, uncomment line 37 and run            
            services.UseFirebaseMessaging<MyPushDelegate>();
#else
            // to try direct firebase configuration, set the Firebase section under appsettings.json, comment out the line below, uncomment line 37 and run
            services.UsePush<MyPushDelegate>();
#endif
            //this.RegisterPlatform?.Invoke(services, config);
        }
    }
}
