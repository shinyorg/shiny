using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace Snippets
{
    public class PushStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // you can only register one :)
            // NOTE: these also all take a notification category if you wish to have actions available
            // on the user notification
            services.UsePush<PushDelegate>();

            services.UsePushAzureNotificationHubs<PushDelegate>("connection string", "hub name");
            services.UseFirebaseMessaging<PushDelegate>();
            services.UseOneSignalPush<PushDelegate>(new Shiny.Push.OneSignal.OneSignalPushConfig("onesignal appId"));
        }
    }
}
