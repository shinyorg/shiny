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


        readonly Subject<ResponseArgs> responseSubj = new Subject<ResponseArgs>();
        public IObservable<ResponseArgs> WhenResponse() => this.responseSubj;


        readonly Subject<PresentArgs> presentSubj = new Subject<PresentArgs>();
        public IObservable<PresentArgs> WhenPresented() => this.presentSubj;


        public override void DidReceiveNotificationResponse(UNUserNotificationCenter center,
                                                            UNNotificationResponse response,
                                                            Action completionHandler)
            => this.responseSubj.OnNext(new ResponseArgs(response, completionHandler));


        public override void WillPresentNotification(UNUserNotificationCenter center,
                                                     UNNotification notification,
                                                     Action<UNNotificationPresentationOptions> completionHandler)
            => this.presentSubj.OnNext(new PresentArgs(notification, completionHandler));
    }
}
