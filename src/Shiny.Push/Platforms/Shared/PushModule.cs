#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Notifications;


namespace Shiny.Push
{
    public class PushModule : ShinyModule
    {
        readonly Type pushManagerType;
        readonly Type delegateType;
        readonly Channel[] channels;


        public PushModule(Type pushManagerType,
                          Type delegateType,
                          Channel[] channels)
        {
            this.pushManagerType = pushManagerType;
            this.delegateType = delegateType;
            this.channels = channels;
        }


        public override void Register(IServiceCollection services)
        {
#if __IOS__
            services.TryAddSingleton<iOSNotificationDelegate>();
            services.TryAddSingleton(sp => (IAppDelegatePushNotificationHandler)sp.Resolve<IPushManager>());

#endif
            services.UseNotifications<PushNotificationDelegate>(false, null, this.channels);
            services.TryAddSingleton(typeof(IPushManager), this.pushManagerType);
            services.AddSingleton(typeof(IPushDelegate), this.delegateType);
        }
    }
}
#endif