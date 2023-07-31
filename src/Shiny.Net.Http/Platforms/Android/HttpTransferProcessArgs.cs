using System;
using AndroidX.Core.App;

namespace Shiny.Net.Http;


public record HttpTransferProcessArgs(
    int NotificationId,
    NotificationCompat.Builder Builder,
    NotificationManagerCompat Notifications,
    Action OnComplete
)
{
    public void SendNotification()
    {
        var not = this.Builder.Build();
        this.Notifications.Notify(this.NotificationId, not);
    }


    public void CancelNotification()
        => this.Notifications.Cancel(this.NotificationId);
};