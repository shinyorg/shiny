using System;
using System.Threading.Tasks;
using Prism.Navigation;
using Samples.Logging;
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
        string? errorMessage;


        public NotificationDelegate(SampleSqliteConnection conn,
                                    INotificationManager notifications,
                                    IMessageBus messageBus)
        {
            this.conn = conn;
            this.notifications = notifications;
            this.messageBus = messageBus;
        }


        public async Task<bool> TryNavigateFromNotification(INavigationService navigator)
        {
            if (this.errorMessage == null)
                return false;

            await navigator.ShowBigText(this.errorMessage, "ERROR");
            this.errorMessage = null;
            return true;
        }


        public async Task OnEntry(NotificationResponse response)
        {
            string? exception = null;
            if (response.Notification.Payload?.TryGetValue("ERROR", out exception) ?? false)
                this.errorMessage = exception;

            await this.Store(new NotificationEvent
            {
                NotificationId = response.Notification.Id,
                NotificationTitle = response.Notification.Title ?? response.Notification.Message,
                Action = response.ActionIdentifier,
                ReplyText = response.Text,
                IsEntry = true,
                Timestamp = DateTime.Now
            });
            await this.DoChat(response);
        }


        public Task OnReceived(Notification notification) => this.Store(new NotificationEvent
        {
            NotificationId = notification.Id,
            NotificationTitle = notification.Title ?? notification.Message,
            IsEntry = false,
            Timestamp = DateTime.Now
        });


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
                            name = response.Text.Trim();

                        await notifications.Send("Shiny Chat", $"Hi {name}, do you like me?", "ChatAnswer");
                        break;

                    case "answer":
                        switch (response.ActionIdentifier.ToLower())
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


        async Task Store(NotificationEvent @event)
        {
            await this.conn.InsertAsync(@event);
            this.messageBus.Publish(@event);
        }
    }
}
