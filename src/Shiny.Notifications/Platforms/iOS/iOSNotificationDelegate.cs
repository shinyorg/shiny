using System;
using System.Reactive.Subjects;
using UserNotifications;


namespace Shiny.Notifications
{
    public class iOSNotificationDelegate : UNUserNotificationCenterDelegate
    {
        public iOSNotificationDelegate()
        {
            UNUserNotificationCenter
                .Current
                .Delegate = this;
        }


        readonly Subject<(UNNotificationResponse Response, Action CompletionHandler)> responseSubj = new Subject<(UNNotificationResponse Response, Action CompletionHandler)>();
        public IObservable<(UNNotificationResponse Response, Action CompletionHandler)> WhenResponse() => this.responseSubj;



        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center,
                                                            UNNotificationResponse response,
                                                            Action completionHandler)
            => this.responseSubj.OnNext((response, completionHandler));



        readonly Subject<(UNNotification Notification, Action<UNNotificationPresentationOptions> CompletionHandler)> presentSubj = new Subject<(UNNotification Notification, Action<UNNotificationPresentationOptions> CompletionHandler)>();
        public IObservable<(UNNotification Notification, Action<UNNotificationPresentationOptions> CompletionHandler)> WhenPresented() => this.presentSubj;

        public override void WillPresentNotification(UNUserNotificationCenter center,
                                                     UNNotification notification,
                                                     Action<UNNotificationPresentationOptions> completionHandler)
            => this.presentSubj.OnNext((notification, completionHandler));
    }
}
