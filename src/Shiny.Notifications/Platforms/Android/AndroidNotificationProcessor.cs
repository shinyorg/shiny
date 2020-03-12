using System;
using Android.Content;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    class AndroidNotificationProcessor
    {
        public const string NOTIFICATION_KEY = "ShinyNotification";
        readonly ISerializer serializer;
        readonly IServiceProvider services;


        public AndroidNotificationProcessor(ISerializer serializer, IServiceProvider services)
        {
            this.serializer = serializer;
            this.services = services;
        }


        public async void TryProcessIntent(Intent intent)
        {
            if (intent == null)
                return;

            if (!intent.HasExtra(NOTIFICATION_KEY))
                return;

            await this.services.SafeResolveAndExecute<INotificationDelegate>(async ndelegate =>
            {
                var notificationString = intent.GetStringExtra(NOTIFICATION_KEY);
                var notification = this.serializer.Deserialize<Notification>(notificationString);
                var response = new NotificationResponse(notification, null, null);
                await ndelegate.OnEntry(response);
            }, false);
        }
    }
}
