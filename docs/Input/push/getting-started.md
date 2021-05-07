Title: Getting Started
Order: 1
---

Push notifications come in so many flavours these days.  The community needed one way of doing things, but the ability to swap providers in and out at will.  The reason, push providers, aside from native have been dropping
like flies the last several years.  The latest to go was AppCenter which it turns out the Xamarin community was heavily invested in. 

## Setup
General OS setup is mostly the same on all platforms, but please review the specific provider you intend to use from the menu for more information


## Registration
Look to each appropriate provider to see setups for each.  The most important function otherwise, is RequestAccess shown below which will give you the push notification token that you can send to your backend. 

All providers use the native implementations on the platform to some degree, as such, you will always need to call

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


## Background Delegate

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

## Foreground Monitoring
It is quite often that you may want to change data due to a silent notification being received.  This is similar to watching a SignalR broadcast, but with observables because RX is awesome and Shiny dies on the RX hill!

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

## Additional Features
Like other modules in Shiny, there are certain providers that support additional feature sets.  Push really only has 1 extra, tagging.

The following providers, support tagging
* Azure Notification Hubs
* Firebase

In order to safely support tagging without the need for constantly feature flag or type checking, the following extension methods exist to make life easy

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
