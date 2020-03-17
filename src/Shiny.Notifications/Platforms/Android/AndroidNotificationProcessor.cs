using System;
using System.Collections.Generic;
using Android.Content;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    class AndroidNotificationProcessor
    {
        public const string NOTIFICATION_KEY = "ShinyNotification";
        readonly ISerializer serializer;
        readonly IEnumerable<INotificationDelegate> delegates;


        public AndroidNotificationProcessor(ISerializer serializer, IEnumerable<INotificationDelegate> delegates)
        {
            this.serializer = serializer;
            this.delegates = delegates;
        }


        public async void TryProcessIntent(Intent intent)
        {
            if (intent == null || this.ndelegate == null)
                return;

            if (!intent.HasExtra(NOTIFICATION_KEY))
                return;

            await this.delegates.RunDelegates(async ndelegate =>
            {
                var notificationString = intent.GetStringExtra(NOTIFICATION_KEY);
                var notification = this.serializer.Deserialize<Notification>(notificationString);
                var response = new NotificationResponse(notification, null, null);
                await ndelegate.OnEntry(response);
            });
        }
    }
}
