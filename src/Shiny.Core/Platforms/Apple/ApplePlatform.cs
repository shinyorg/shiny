using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Net;
using UIKit;


namespace Shiny
{
    public class ApplePlatform : IPlatform
    {
        public void Register(IServiceCollection services)
        {
            services.RegisterCommonServices();
#if __IOS__
            services.TryAddSingleton<IConnectivity, ConnectivityImpl>();
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
