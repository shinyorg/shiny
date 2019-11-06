//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Shiny.Infrastructure;
//using AppKit;
//using Foundation;


//namespace Acr.Notifications
//{
//    public class NotificationManagerImpl : AbstractNotificationManagerImpl
//    {
//        readonly INotificationRepository repository;
//        public NotificationManagerImpl(INotificationRepository repository)
//            => this.repository = repository;


//        public override Task CancelAll() => this.Invoke(() =>
//        {
//            var dunc = NSUserNotificationCenter.DefaultUserNotificationCenter;
//            //dunc.Delegate = new AcrUserNotificationDelegate(native =>
//            //{
//            //    var notification = this.FromNative(native);
//            //    this.OnActivated(notification);
//            //});
//            foreach (var native in dunc.ScheduledNotifications)
//                dunc.RemoveScheduledNotification(native);

//            dunc.RemoveAllDeliveredNotifications();
//        });


//        public override Task Cancel(int notificationId) => this.Invoke(() =>
//        {
//            var dnc = NSUserNotificationCenter.DefaultUserNotificationCenter;
//            var native = dnc.ScheduledNotifications.FirstOrDefault(x => x.Identifier == notificationId.ToString());
//            if (native != null)
//                dnc.RemoveScheduledNotification(native);

//            native = dnc.DeliveredNotifications.FirstOrDefault(x => x.Identifier == notificationId.ToString());
//            if (native != null)
//                dnc.RemoveDeliveredNotification(native);
//        });


//        public override async Task Send(Notification notification)
//        {
//            notification.Id = await this.repository.Insert(notification);
//            var native = new NSUserNotification
//            {
//                Identifier = notification.Id.ToString(),
//                Title = notification.Title,
//                InformativeText = notification.Message,
//                SoundName = notification.Sound,
//                // UserInfo = notification.MetadataToNsDictionary()
//            };
//            if (notification.NextTriggerDate != null)
//                native.DeliveryDate = notification.NextTriggerDate.Value.ToNSDate();

//            NSUserNotificationCenter
//                .DefaultUserNotificationCenter
//                .ScheduleNotification(native);
//        }


//        public override Task<IEnumerable<Notification>> GetPendingNotifications()
//        {
//            var tcs = new TaskCompletionSource<IEnumerable<Notification>>();
//            NSApplication.SharedApplication.InvokeOnMainThread(() =>
//            {
//                var natives = NSUserNotificationCenter
//                    .DefaultUserNotificationCenter
//                    .ScheduledNotifications
//                    .Select(this.FromNative);

//                tcs.TrySetResult(natives);
//            });
//            return tcs.Task;
//        }


//        protected int ToNotificationId(string value)
//        {
//            if (!Int32.TryParse(value, out var i))
//                return -1;

//            return i;
//        }


//        //protected virtual NotificationInfo FromNative(NSUserNotification x) => new NotificationInfo(0, null, null)
//        //{
//        //    //Id = this.ToNotificationId(x.Identifier),
//        //    //Title = x.Title,
//        //    //Message = x.InformativeText,
//        //    //Sound = x.SoundName
//        //    //ScheduledDate = x.DeliveryDate.ToDateTime(),
//        //    //Metadata = x.UserInfo.FromNsDictionary()
//        //};


//        protected async Task Invoke(Action action)
//        {
//            var tcs = new TaskCompletionSource<object>();
//            NSApplication.SharedApplication.InvokeOnMainThread(() =>
//            {
//                action();
//                tcs.TrySetResult(null);
//            });
//            await tcs.Task;
//        }
//    }
//}
