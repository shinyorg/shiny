Title: Getting Started
Order: 1
---

You may ask "what does local notifications" have to do with device or background services?  Well.... turns out quite a bit.  You need a way
of letting your users know that you've done something in the background.  Maybe your job ran and determined that you need to do something OR you connected
to a BLE peripheral... who knows, but you'll definitely need local notifications at some point well using Shiny 

<?! PackageInfo "Shiny.Notifications" /?>


## iOS Specifics
There isn't anything specific to setting up local notifications on iOS.  However,
iOS has one specific rule - While the application  is running in the **FOREGROUND**, notifications will not be displayed.
You are responsible for notifying your users of actions happening while your app is running.  


## Android Specifics
Android has many different customization points - please review the [Android Specifics Documentation](xref:notifications-android)


# Notifications
First rule, as with any Shiny service.  Run RequestAccess

```csharp
var result = await NotificationManager.RequestAccess();
if (result == PermissionState.Available) {
    // ... do something
}
```


## Send a Notification

## Scheduling a Notification

## Getting A List of Pending Notifications

## Cancelling Individual or All Pending Notifications

## Notification Responses
```cs
using System.Threading.Tasks;
using Shiny.Notifications;

public class NotificationDelegate : INotificationDelegate
{
    public async Task OnEntry(NotificationResponse response)
    {
    }
}

```
