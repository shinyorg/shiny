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


        public PushModule(Type pushManagerType, Type delegateType, Channel[] channels)
        {
            this.pushManagerType = pushManagerType;
            this.delegateType = delegateType;
            this.channels = channels;
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IPushDelegate), this.delegateType);
            services.TryAddSingleton(typeof(IPushManager), this.pushManagerType);
#if __IOS__ || WINDOWS_UWP
            services.UseNotifications(null, null, this.channels);
#elif __ANDROID__
            services.UseNotifications<PushNotificationDelegate>(null, this.channels);
#endif
        }
    }
}
#endif