using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    public class AndroidNotificationProcessor
    {
        public const string IntentNotificationKey = "ShinyNotification";
        public const string IntentActionKey = "Action";
        public const string RemoteInputResultKey = "Result";

        readonly ISerializer serializer;
        readonly IEnumerable<INotificationDelegate> delegates;


        public AndroidNotificationProcessor(ISerializer serializer, IEnumerable<INotificationDelegate> delegates)
        {
            this.serializer = serializer;
            this.delegates = delegates;
        }


        public async Task TryProcessIntent(Intent? intent)
        {
            if (intent == null || !this.delegates.Any())
                return;

            if (intent.HasExtra(IntentNotificationKey))
            {
                var notificationString = intent.GetStringExtra(IntentNotificationKey);
                var notification = this.serializer.Deserialize<Notification>(notificationString);

                var action = intent.GetStringExtra(IntentActionKey);
                var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
                var response = new NotificationResponse(notification, action, text);

                // the notification lives within the intent since it has already been removed from the repo
                await this.delegates.RunDelegates(x => x.OnEntry(response));
            }
        }
    }
}
