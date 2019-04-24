Title: Recipes
---

Triggering a Notification when your download is complete

```csharp
public class Startup : Shiny.Startup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseNotifications();
        services.UseHttpTransfers<YourDelegate>();
    }
}

public class YourDelegate : IHttpTransferDelegate
{
    readonly INotificationManager notifications;


    public YourDelegate(INotificationManager notifications)
    {
        this.notifications = notifications;
    }


    public async void OnError(HttpTransfer transfer, Exception ex)
    {
        await this.notifications.Send("Your transfer failed");
    }


    public async void OnCompleted(HttpTransfer transfer)
    {
        await this.notifications.Send("Your transfer is complete");
    }
}
```