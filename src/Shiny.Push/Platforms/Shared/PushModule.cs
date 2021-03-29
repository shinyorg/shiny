#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;


namespace Shiny.Push
{
    public class PushModule : ShinyModule
    {
        readonly Type pushManagerType;
        readonly Type delegateType;


        public PushModule(Type pushManagerType, Type delegateType)
        {
            this.pushManagerType = pushManagerType;
            this.delegateType = delegateType;
        }


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton(typeof(IPushDelegate), this.delegateType);
            services.TryAddSingleton(typeof(IPushManager), this.pushManagerType);
#if __IOS__
            services.TryAddSingleton(sp => (IAppDelegatePushNotificationHandler)sp.Resolve<IPushManager>());
            services.UseNotifications();
#elif __ANDROID__
            services.UseNotifications<PushNotificationDelegate>();
#elif WINDOWS_UWP
            services.UseNotifications();
#endif
        }
    }
}
#endif