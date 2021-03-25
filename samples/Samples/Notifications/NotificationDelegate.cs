using System;
using System.Threading.Tasks;
using Samples.Models;
using Shiny;
using Shiny.Notifications;


namespace Samples.Notifications
{
    public class NotificationDelegate : INotificationDelegate
    {
        readonly SampleSqliteConnection conn;
        readonly IMessageBus messageBus;
        readonly INotificationManager notifications;


        public NotificationDelegate(SampleSqliteConnection conn,
                                    INotificationManager notifications,
                                    IMessageBus messageBus)
        {
            this.conn = conn;
            this.notifications = notifications;
            this.messageBus = messageBus;
        }


        public async Task OnEntry(NotificationResponse response)
        {
            var @event = new NotificationEvent
            {
                NotificationId = response.Notification.Id,
                NotificationTitle = response.Notification.Title ?? response.Notification.Message,
                Action = response.ActionIdentifier,
                ReplyText = response.Text,
                Timestamp = DateTime.Now
            };
            await this.conn.InsertAsync(@event);
            this.messageBus.Publish(@event);

            await this.DoChat(response);
        }


        async Task DoChat(NotificationResponse response)
        {
            var cat = response.Notification.Channel;
            if (cat?.StartsWith("Chat") ?? false)
            {
                cat = cat.Replace("Chat", String.Empty).ToLower();
                switch (cat)
                {
                    case "name":
                        var name = "Shy Person";
                        if (!response.Text.IsEmpty())
                            name = response.Text!.Trim();

                        await notifications.Send(
                            "Shiny Chat",
                            $"Hi {name}, do you like me?",
                            "ChatAnswer"
                        );
                        break;

                    case "answer":
                        switch (response.ActionIdentifier?.ToLower())
                        {
                            case "yes":
                                await this.notifications.Send("Shiny Chat", "YAY!!");
                                break;

                            case "no":
                                await this.notifications.Send("Shiny Chat", "Go away then!");
                                break;
                        }
                        break;
                }
            }
        }
    }
}
