using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Shiny.Settings;
using UserNotifications;


namespace Shiny.Notifications
{
    public class NotificationManager : INotificationManager
    {
        readonly ISettings settings;


        public NotificationManager(ISettings settings)
        {
            this.settings = settings;
        }


        public int Badge
        {
            get => 0;
            set { }
            //get => this.settings.Get("Badge", 0);
            //set
            //{
            //    this.settings.Set("Badge", value);

            //    //Dispatcher.InvokeOnMainThreadAsync(() =>
            //        NSApplication.SharedApplication. = value
            //    //);
            //}
        }


        public Task Clear() => this.Invoke(() =>
        {
            //UNUserNotificationCenter.
            var dunc = NSUserNotificationCenter.DefaultUserNotificationCenter;
            //dunc.Delegate = new AcrUserNotificationDelegate(native =>
            //{
            //    var notification = this.FromNative(native);
            //    this.OnActivated(notification);
            //});
            foreach (var native in dunc.ScheduledNotifications)
                dunc.RemoveScheduledNotification(native);

            dunc.RemoveAllDeliveredNotifications();
        });


        public Task Cancel(int notificationId) => this.Invoke(() =>
        {
            var dnc = NSUserNotificationCenter.DefaultUserNotificationCenter;
            var native = dnc.ScheduledNotifications.FirstOrDefault(x => x.Identifier == notificationId.ToString());
            if (native != null)
                dnc.RemoveScheduledNotification(native);

            native = dnc.DeliveredNotifications.FirstOrDefault(x => x.Identifier == notificationId.ToString());
            if (native != null)
                dnc.RemoveDeliveredNotification(native);
        });



        public Task<AccessState> RequestAccess()
        {
            //NSApplication.SharedApplication.RegisteredForRemoteNotifications();
            //NSApplication.SharedApplication.RegisterForRemoteNotificationTypes(NSRemoteNotificationType.Alert);
            return Task.FromResult(AccessState.Available);
        }


        public Task<IEnumerable<Notification>> GetPending()
        {
            var list = NSUserNotificationCenter
                .DefaultUserNotificationCenter
                .ScheduledNotifications
                .Select(FromNative);

            return Task.FromResult(list);
        }


        public Task Send(Notification notification)
        {
            notification.Id = this.settings.IncrementValue("NotificationId");
            var native = new NSUserNotification
            {
                Identifier = notification.Id.ToString(),
                Title = notification.Title,
                InformativeText = notification.Message
                //SoundName = notification.,
                // UserInfo = notification.MetadataToNsDictionary()
            };
            //if (notification.NextTriggerDate != null)
            //    native.DeliveryDate = notification.NextTriggerDate.Value.ToNSDate();

            NSUserNotificationCenter
                .DefaultUserNotificationCenter
                .ScheduleNotification(native);

            return Task.CompletedTask;
        }


        public Task<IEnumerable<Notification>> GetPendingNotifications()
        {
            var tcs = new TaskCompletionSource<IEnumerable<Notification>>();
            NSApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                var natives = NSUserNotificationCenter
                    .DefaultUserNotificationCenter
                    .ScheduledNotifications
                    .Select(this.FromNative);

                tcs.TrySetResult(natives);
            });
            return tcs.Task;
        }


        public void RegisterCategory(NotificationCategory category)
        {
        }


        protected int ToNotificationId(string value)
        {
            if (!Int32.TryParse(value, out var i))
                return -1;

            return i;
        }


        protected virtual Notification FromNative(NSUserNotification x) => new Notification
        {
            //Id = this.ToNotificationId(x.Identifier),
            //Title = x.Title,
            //Message = x.InformativeText,
            //Sound = x.SoundName
            //ScheduledDate = x.DeliveryDate.ToDateTime(),
            //Metadata = x.UserInfo.FromNsDictionary()
        };


        protected async Task Invoke(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            NSApplication.SharedApplication.InvokeOnMainThread(() =>
            {
                action();
                tcs.TrySetResult(null);
            });
            await tcs.Task;
        }
    }
}
