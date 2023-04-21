using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.Notifications;

namespace Sample.Notifications;


public class MyNotificationDelegate : INotificationDelegate
{
    readonly SampleSqliteConnection conn;
    readonly INotificationManager notifications;


    public MyNotificationDelegate(SampleSqliteConnection conn, INotificationManager notifications)
    {
        this.conn = conn;
        this.notifications = notifications;
    }


    public async Task OnEntry(NotificationResponse response)
    {
        await this.conn.Log(
            "Notifications",
            $"{response.Notification.Title ?? response.Notification.Message}",
            $"Action: {response.ActionIdentifier} - Reply: {response.Text}"
        );
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

                    await this.notifications.Send(
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
