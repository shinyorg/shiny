using System;
using Android.Content;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Logging;


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

            var ndelegate = this.services.GetService<INotificationDelegate>();
            if (ndelegate == null)
                return;

            await Log.SafeExecute(async () =>
            {
                var notificationString = intent.GetStringExtra(NOTIFICATION_KEY);
                var notification = this.serializer.Deserialize<Notification>(notificationString);
                var response = new NotificationResponse(notification, null, null);
                await ndelegate.OnEntry(response);
            });
        }
    }
}
