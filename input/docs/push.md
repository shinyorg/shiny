Title: Push
Description: One push provider to rule them all!
---

# Push
Push comes in several different flavours.  The main goal with push was to make it easy to switch between push providers without changing anything besides configuration in your code base.


## Providers
|Provider|iOS|Android|UWP|Server Setup|NuGet|
|---|---|---|---|---|---|---|
|Native|10+|6+|16299+|||
|Azure Notification Hubs|10+|6+|16299+|[Link](https://docs.microsoft.com/en-ca/azure/notification-hubs/)||
|Firebase|10+|6||||
|OneSignal|10+|6||||

## Registration
Look to each appropriate provider to see setups for each.  

<!-- snippet: PushStartup.cs -->
<a id='snippet-PushStartup.cs'></a>
```cs
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace Snippets
{
    public class PushStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // you can only register one :)
            // NOTE: these also all take a notification category if you wish to have actions available
            // on the user notification
            services.UsePush<PushDelegate>();

            services.UsePushAzureNotificationHubs<PushDelegate>("connection string", "hub name");
            services.UseFirebaseMessaging<PushDelegate>();
            services.UseOneSignalPush<PushDelegate>(new Shiny.Push.OneSignal.OneSignalPushConfig("onesignal appId"));
        }
    }
}
```
<sup><a href='/src/Snippets/PushStartup.cs#L1-L21' title='File snippet `PushStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-PushStartup.cs' title='Navigate to start of snippet `PushStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->

All providers use the native implementations on the platform to some degree, as such, you will always need to call

<!-- snippet: PushPermissions.cs -->
<a id='snippet-PushPermissions.cs'></a>
```cs
using System;
using System.Threading.Tasks;
using Shiny;
using Shiny.Push;

public class PushRegistration
{
    public async Task CheckPermission()
    {
        var push = ShinyHost.Resolve<IPushManager>();
        var result = await push.RequestAccess();
        if (result.Status == AccessState.Available)
        {
            // good to go

            // you should send this to your server with a userId attached if you want to do custom work
            var value = result.RegistrationToken;
        }
    }
}
```
<sup><a href='/src/Snippets/PushPermissions.cs#L1-L20' title='File snippet `PushPermissions.cs` was extracted from'>snippet source</a> | <a href='#snippet-PushPermissions.cs' title='Navigate to start of snippet `PushPermissions.cs`'>anchor</a></sup>
<!-- endSnippet -->

## Background Delegate
<!-- snippet: PushDelegate.cs -->
<a id='snippet-PushDelegate.cs'></a>
```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Push;


public class PushDelegate : IPushDelegate
{
    public async Task OnEntry(PushEntryArgs args)
    {
        // fires when the user taps on a push notification
    }

    public async Task OnReceived(IDictionary<string, string> data)
    {
        // fires when a push notification is received (silient or notification)
    }

    public async Task OnTokenChanged(string token)
    {
        // fires when a push notification change is set by the operating system or provider
    }
}
```
<sup><a href='/src/Snippets/PushDelegate.cs#L1-L23' title='File snippet `PushDelegate.cs` was extracted from'>snippet source</a> | <a href='#snippet-PushDelegate.cs' title='Navigate to start of snippet `PushDelegate.cs`'>anchor</a></sup>
<!-- endSnippet -->

## Foreground Monitoring
It is quite often that you may want to change data due to a silent notification being received.  This is similar to watching a SignalR broadcast, but with observables because RX is awesome and Shiny dies on the RX hill!

<!-- snippet: PushForeground.cs -->
<a id='snippet-PushForeground.cs'></a>
```cs
using System.Reactive.Linq;
using Shiny;
using Shiny.Push;

public class PushForeground
{
    public void YourMethod()
    {
        var push = ShinyHost.Resolve<IPushManager>(); // assign through DI, static, or ShinyHost.Resolve
        var disp = push
            .WhenReceived()
            .Where(x => x["newdata"] == "true")
            .SubscribeAsync(async data =>
            {
                // make you HTTP call here
            });
    }
}
```
<sup><a href='/src/Snippets/PushForeground.cs#L1-L18' title='File snippet `PushForeground.cs` was extracted from'>snippet source</a> | <a href='#snippet-PushForeground.cs' title='Navigate to start of snippet `PushForeground.cs`'>anchor</a></sup>
<!-- endSnippet -->

## Additional Features
Like other modules in Shiny, there are certain providers that support additional feature sets.  Push really only has 1 extra, tagging.

The following providers, support tagging
* Azure Notification Hubs
* Firebase

In order to safely support tagging without the need for constantly feature flag or type checking, the following extension methods exist to make life easy

<!-- snippet: PushExtensions.cs -->
<a id='snippet-PushExtensions.cs'></a>
```cs
using System.Threading.Tasks;
using Shiny;
using Shiny.Push;

public class Extensions
{
    public async Task Method()
    {
        var push = ShinyHost.Resolve<IPushManager>();

        var supported = push.IsTagsSupport();

        // tries to set a params list of tags if available
        await push.TrySetTags("tag1", "tag2");

        // gets a list of currently set tags
        var tags = push.TryGetTags();

        // requests permission from the user and sets tags if available
        var permissionResult = await push.TryRequestAccessWithTags("tag1", "tag2");
    }
}
```
<sup><a href='/src/Snippets/PushExtensions.cs#L1-L22' title='File snippet `PushExtensions.cs` was extracted from'>snippet source</a> | <a href='#snippet-PushExtensions.cs' title='Navigate to start of snippet `PushExtensions.cs`'>anchor</a></sup>
<!-- endSnippet -->
