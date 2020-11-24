using System;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Jobs;
using Shiny.Net;
using UIKit;
using Foundation;


namespace Shiny
{
    public class ApplePlatform : IPlatform
    {
        public string AppIdentifier => NSBundle.MainBundle.BundleIdentifier;
        public string AppVersion => NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
        public string AppBuild => NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();

        public string MachineName { get; } = "";
        public string OperatingSystem => NSProcessInfo.ProcessInfo.OperatingSystemName;
        public string OperatingSystemVersion => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
        public string Manufacturer { get; } = "Apple";
        public string Model { get; } = "";


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
