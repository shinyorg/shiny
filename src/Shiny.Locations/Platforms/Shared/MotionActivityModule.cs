#if __IOS__ || __ANDROID__
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public class MotionActivityModule : ShinyModule
    {
        readonly bool requestPermissionOnStart;
        public MotionActivityModule(bool requestPermissionOnStart) => this.requestPermissionOnStart = requestPermissionOnStart;


        public override void Register(IServiceCollection services)
        {
#if __ANDROID__
            services.AddSingleton<AndroidSqliteDatabase>();
#endif
            services.TryAddSingleton<IMotionActivityManager, MotionActivityManagerImpl>();
        }


        public override async void OnContainerReady(IServiceProvider sp)
        {
            if (this.requestPermissionOnStart)
            {
                var access = await sp
                    .GetRequiredService<IMotionActivityManager>()
                    .RequestAccess();

                if (access != AccessState.Available)
                    sp.Resolve<ILogger<IMotionActivityManager>>().LogWarning("Invalid access - " + access);
            }
        }
    }
}
#endif