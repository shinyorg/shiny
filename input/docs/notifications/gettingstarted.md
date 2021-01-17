Title: Getting Started
Order: 1
RedirectFrom: docs/notifications/index
---

## DESCRIPTION

Notifications are a great way of letting your users know your app just figured something out in the background.  Thus, Shiny needed a decent local notification library.


## PLATFORMS

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|8|
|UWP|16299|

## USAGE

|Area|Info|
|----|----|
|NuGet| [![NotificationsNugetShield]][NotificationsNuget] |
|Shiny Startup|services.UseNotifications|
|Main Service|Shiny.Notifications.INotificationManager|
|Shiny Delegate|Shiny.Notifications.INotificationDelegate|
|Static Generated|ShinyNotifications|
|Manual Resolve|ShinyHost.Resolve<Shiny.Notifications.INotificationManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.Notifications.INotificationManager>>()|


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

<?! Include "../../nuget.md" /?>