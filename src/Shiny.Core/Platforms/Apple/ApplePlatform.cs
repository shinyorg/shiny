using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Infrastructure;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;
using UIKit;


namespace Shiny
{
    public class ApplePlatform : IPlatform
    {
        public void Register(IServiceCollection services)
        {
            services.TryAddSingleton<ISerializer, ShinySerializer>();
            services.TryAddSingleton<IMessageBus, MessageBus>();
            services.TryAddSingleton<IEnvironment, EnvironmentImpl>();
#if __IOS__
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
#endif
            services.TryAddSingleton<IPowerManager, PowerManagerImpl>();
            services.TryAddSingleton<IFileSystem, FileSystemImpl>();
            services.TryAddSingleton<ISettings, SettingsImpl>();

#if __IOS__
            if (BgTasksJobManager.IsAvailable)
                services.TryAddSingleton<IJobManager, BgTasksJobManager>();
            else
                services.TryAddSingleton<IJobManager, JobManager>();
#endif
        }


#if __WATCHOS__
        public IObservable<PlatformState> WhenStateChanged()
        {
            throw new NotImplementedException();
        }
#else
        public IObservable<PlatformState> WhenStateChanged() => Observable.Create<PlatformState>(ob =>
        {
            var fg = UIApplication.Notifications.ObserveWillEnterForeground(
                UIApplication.SharedApplication,
                (_, __) => ob.OnNext(PlatformState.Foreground)
            );
            var bg = UIApplication.Notifications.ObserveDidEnterBackground(
                UIApplication.SharedApplication,
                (_, __) => ob.OnNext(PlatformState.Background)
            );
            return () =>
            {
                fg?.Dispose();
                bg?.Dispose();
            };
        });
#endif
    }
}
