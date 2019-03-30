using System;
using System.Threading.Tasks;
using Foundation;
using Shiny.Settings;
using UIKit;
using UserNotifications;


namespace Shiny.Notifications
{
    public class NotificationManagerImpl : INotificationManager
    {
        readonly ISettings settings; // this will have problems with data protection

        public NotificationManagerImpl(ISettings settings)
        {
            this.settings = settings;
        }


        public Task<AccessState> RequestAccess()
        {
            var tcs = new TaskCompletionSource<AccessState>();

            UNUserNotificationCenter.Current.RequestAuthorization(
                UNAuthorizationOptions.Alert |
                UNAuthorizationOptions.Badge |
                UNAuthorizationOptions.Sound,
                (approved, error) =>
                {
                    if (error != null)
                        tcs.SetException(new Exception(error.Description));
                    else
                    {
                        var state = approved ? AccessState.Available : AccessState.Denied;
                        tcs.SetResult(state);
                    }
                });

            return tcs.Task;
        }


        public Task Clear() => this.Invoke(() =>
        {
            UNUserNotificationCenter.Current.RemoveAllPendingNotificationRequests();
            UNUserNotificationCenter.Current.RemoveAllDeliveredNotifications();
        });


        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            //var permission = await this.RequestAccess();
            var content = new UNMutableNotificationContent
            {
                Title = notification.Title,
                Body = notification.Message
                //Badge=
                //LaunchImageName = ""
                //Subtitle = ""
            };
            //UNNotificationAttachment.FromIdentifier("", NSUrl.FromString(""), new UNNotificationAttachmentOptions().)
            if (!String.IsNullOrWhiteSpace(notification.Payload))
                content.UserInfo.SetValueForKey(new NSString(notification.Payload), new NSString("Payload"));

            if (!String.IsNullOrWhiteSpace(notification.Sound))
                content.Sound = UNNotificationSound.GetSound(notification.Sound);

            var request = UNNotificationRequest.FromIdentifier(
                notification.Id.ToString(),
                content,
                UNTimeIntervalNotificationTrigger.CreateTrigger(3, false)
            );
            await UNUserNotificationCenter
                .Current
                .AddNotificationRequestAsync(request);
        }


        protected Task Invoke(Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            var app = UIApplication.SharedApplication;
            app.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    action();
                    tcs.TrySetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
//UNUserNotificationCenter.Current.SetNotificationCategories(
//    UNNotificationCategory.FromIdentifier(
//        "",
//        new UNNotificationAction[]
//        {
//            UNNotificationAction.FromIdentifier(
//                "id",
//                "title",
//                UNNotificationActionOptions.AuthenticationRequired
//            )
//        },
//        new string[] { "" },
//        "hiddenPreviewsBodyPlaceholder",
//        new NSString(""),
//        UNNotificationCategoryOptions.None
//    )
//);

//UNUserNotificationCenter
//    .Current
//    .Delegate = new AcrUserNotificationCenterDelegate(response =>
//    {
//        var notification = response.Notification.Request.FromNative();
//        this.OnActivated(notification);
//    });