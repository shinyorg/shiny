Title: Getting Started
Order: 1
---

Notifications are a great way of letting your users know your app just figured something out in the background.  Thus, Shiny needed a decent local notification library.

<?! PackageInfo "Shiny.Notifications" /?>



```cs
using System.Threading.Tasks;
using Shiny.Notifications;

public class NotificationDelegate : INotificationDelegate
{
    public async Task OnEntry(NotificationResponse response)
    {
    }

    public async Task OnReceived(Notification notification)
    {
    }
}

```

# Notifications

## Send a Notification

## Scheduling a Notification

## Getting A List of Pending Notifications

## Cancelling Individual or All Pending Notifications
