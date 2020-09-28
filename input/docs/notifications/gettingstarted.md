Title: Getting Started
Order: 1
RedirectFrom: docs/notifications/index
---

Notifications are a great way of letting your users know your app just figured something out in the background.  Thus, Shiny needed a decent local notification library.

[![NuGet](https://img.shields.io/nuget/v/Shiny.Notifications.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Notifications/)

## PLATFORMS

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|5|
|UWP|16299|

<!-- snippet: NotificationStartup.cs -->
<a id='snippet-NotificationStartup.cs'></a>
```cs
using Microsoft.Extensions.DependencyInjection;

using Shiny;

public class NotificationStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        throw new System.NotImplementedException();
    }
}
```
<sup><a href='/src/Snippets/NotificationStartup.cs#L1-L11' title='File snippet `NotificationStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-NotificationStartup.cs' title='Navigate to start of snippet `NotificationStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: NotificationDelegate.cs -->
<a id='snippet-NotificationDelegate.cs'></a>
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
<sup><a href='/src/Snippets/NotificationDelegate.cs#L1-L13' title='File snippet `NotificationDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-NotificationDelegate.cs' title='Navigate to start of snippet `NotificationDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: NotificationUsage.cs -->
<a id='snippet-NotificationUsage.cs'></a>
```cs
using System.Threading.Tasks;
using Shiny;
using Shiny.Notifications;

public class NotificationUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<INotificationManager>();
    }
}
```
<sup><a href='/src/Snippets/NotificationUsage.cs#L1-L11' title='File snippet `NotificationUsage.cs` was extracted from'>snippet source</a> | <a href='#snippet-NotificationUsage.cs' title='Navigate to start of snippet `NotificationUsage.cs`'>anchor</a></sup>
<!-- endSnippet -->

# Notifications

## Send a Notification

## Scheduling a Notification

## Getting A List of Pending Notifications

## Cancelling Individual or All Pending Notifications
