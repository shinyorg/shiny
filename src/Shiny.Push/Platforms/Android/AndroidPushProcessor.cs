using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;


namespace Shiny.Push
{
    public class AndroidPushProcessor
    {
        public const string IntentNotificationKey = "ShinyPush";
        public const string IntentActionKey = "Action";
        public const string RemoteInputResultKey = "Result";

        readonly ISerializer serializer;
        readonly IEnumerable<IPushDelegate> delegates;


        public AndroidPushProcessor(ISerializer serializer, IEnumerable<IPushDelegate> delegates)
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
                var notification = this.serializer.Deserialize<Shiny.Notifications.Notification>(notificationString);

                var action = intent.GetStringExtra(IntentActionKey);
                var text = RemoteInput.GetResultsFromIntent(intent)?.GetString("Result");
                var response = new PushNotificationResponse(notification, action, text);

                await this.delegates.RunDelegates(x => x.OnEntry(response));
            }
        }
    }
}
