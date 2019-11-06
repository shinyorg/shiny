using System;
using Android.Content;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Notifications
{
    class AndroidNotificationProcessor
    {
        public const string NOTIFICATION_KEY = "ShinyNotification";
        readonly ISerializer serializer;
        readonly INotificationDelegate ndelegate;


        public AndroidNotificationProcessor(ISerializer serializer, INotificationDelegate ndelegate)
        {
            this.serializer = serializer;
            this.ndelegate = ndelegate;
        }


        public async void TryProcessIntent(Intent intent)
        {
            if (intent == null)
                return;

            if (!intent.HasExtra(NOTIFICATION_KEY))
                return;

            try
            {
                var notificationString = intent.GetStringExtra(NOTIFICATION_KEY);
                var notification = this.serializer.Deserialize<Notification>(notificationString);

                await this.ndelegate.OnEntry(notification);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
