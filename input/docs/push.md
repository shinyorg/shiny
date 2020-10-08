<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/push.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: Push
Description: One push provider to rule them all!
---

# Push
Push notifications come in so many flavours these days.  The community needed one way of doing things, but the ability to swap providers in and out at will.  The reason, push providers, aside from native have been dropping
like flies the last several years.  The latest to go was AppCenter which it turns out the Xamarin community was heavily invested in.  


## Providers
|Provider|iOS|Android|UWP|Server Setup|NuGet|
|---|---|---|---|---|---|---|
|Native|10+|6+|16299+|||
|Azure Notification Hubs|10+|6+|16299+|[Link](https://docs.microsoft.com/en-ca/azure/notification-hubs/)||
|Firebase|10+|6||||
|OneSignal|10+|6||||
|Amazon|10+|6|||

## Registration
Look to each appropriate provider to see setups for each.  

<!-- snippet: PushStartup.cs -->
```cs
using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;


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
```
<sup>[snippet source](/src/Snippets/PushStartup.cs#L1-L19)</sup>
<!-- endsnippet -->

All providers use the native implementations on the platform to some degree, as such, you will always need to call

<!-- snippet: PushPermissions.cs -->
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
<sup>[snippet source](/src/Snippets/PushPermissions.cs#L1-L21)</sup>
<!-- endsnippet -->

## Background Delegate
<!-- snippet: PushDelegate.cs -->
```cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public interface IPushDelegate
    {
        Task OnEntry(PushEntryArgs args);
        Task OnReceived(IDictionary<string, string> data);
        Task OnTokenChanged(string token);
    }
}

```
<sup>[snippet source](/src/Shiny.Push.Abstractions/IPushDelegate.cs#L1-L15)</sup>
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
<sup>[snippet source](/src/Snippets/PushDelegate.cs#L1-L23)</sup>
<!-- endsnippet -->


## Foreground Monitoring
It is quite often that you may want to change data due to a silent notification being received.  This is similar to watching a SignalR broadcast, but with observables because RX is awesome and Shiny dies on the RX hill!

<!-- snippet: PushForeground.cs -->
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
<sup>[snippet source](/src/Snippets/PushForeground.cs#L1-L19)</sup>
<!-- endsnippet -->

## Additional Features
Like other modules in Shiny, there are certain providers that support additional feature sets.  Push really only has 1 extra, tagging.

The following providers, support tagging
* Azure Notification Hubs
* Firebase

In order to safely support tagging without the need for constantly feature flag or type checking, the following extension methods exist to make life easy

<!-- snippet: PushExtensions.cs -->
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
<sup>[snippet source](/src/Snippets/PushExtensions.cs#L1-L22)</sup>
<!-- endsnippet -->
