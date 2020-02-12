using System;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;
using UserNotifications;


namespace Shiny.Notifications
{
    public class ShinyNotificationContext : UNUserNotificationCenterDelegate
    {

        public ShinyNotificationContext(IServiceProvider serviceProvider)
        {
            this.Services = serviceProvider;

            UNUserNotificationCenter
                .Current
                .Delegate = this;
        }


        public IServiceProvider Services { get; }

        readonly Subject<WillPresentArgs> presentSubj = new Subject<WillPresentArgs>();
        public IObservable<WillPresentArgs> WhenWillPresentNotification() => this.presentSubj;


        readonly Subject<DidReceiveNotificationResponseArgs> receiveSubj = new Subject<DidReceiveNotificationResponseArgs>();
        public IObservable<DidReceiveNotificationResponseArgs> WhenDidReceiveNotificationResponse() => this.receiveSubj;


        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
            => this.receiveSubj.OnNext(new DidReceiveNotificationResponseArgs(response, completionHandler));


        public override void WillPresentNotification(UNUserNotificationCenter center, UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
            => this.presentSubj.OnNext(new WillPresentArgs(notification, completionHandler));
    }


    public static class ServiceCollectionExtensions
    {
        static bool registered;
        public static void RegisterIosNotificationContext(this IServiceCollection services)
        {
            if (registered)
                return;

            services.AddSingleton<ShinyNotificationContext>();
            registered = true;
        }
    }

    public class WillPresentArgs
    {
        public WillPresentArgs(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler)
        {
            this.Notification = notification;
            this.CompletionHandler = completionHandler;
        }


        public Action<UNNotificationPresentationOptions> CompletionHandler { get; }
        public UNNotification Notification { get; }
    }


    public class DidReceiveNotificationResponseArgs
    {
        public DidReceiveNotificationResponseArgs(UNNotificationResponse response, Action completionHandler)
        {
            this.Response = response;
            this.CompletionHandler = completionHandler;
        }


        public Action CompletionHandler { get; }
        public UNNotificationResponse Response { get; }
    }
}
