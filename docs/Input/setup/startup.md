Title: Startup
Order: 0
Xref: startup
---

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny;


namespace Samples
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureLogging(ILoggingBuilder builder, IPlatform platform)
        {
            builder.AddFirebase(LogLevel.Warning);
            builder.AddAppCenter("YourAppCenterKey", LogLevel.Warning);
        }


        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        {
        }

    }
}

```

## Best Practices

1. Always use the <TheShinyService>.RequestAccess to check if your user gives the right permissions to use the service in question. 
2. Don't do UI centric things from a Shiny delegate, this includes things like calling RequestAccess.  Most Shiny services have an equivalent "foreground" type call on the service that can be used safely within the UI portion of your application.
3. Don't try to setup background processes in a delegate or as your app is going to sleep.  The methods for setting up background processes in Shiny are all asyncc and the OS will not wait for things to complete.  
4. Treat everything as a singleton in Shiny

### Registering Platform Specific Services

You are also likely to want to occasionally register your own cross platform services.  Shiny offers a way of doing this via the startup.  NOTE: If you do this, you will have to remove the boilerplate source generation from the Shiny package

#### iOS
```csharp
public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate 
{
    public override bool FinishedLaunching(UIApplication app, NSDictionary options) 
    {
        this.ShinyFinishedLaunching(new YourStartup 
        {
            RegisterPlatformServices = services => 
            {
                services.AddSingleton<IYourService, YourService>();
            }
        });
    }
}
```


### Android
```csharp
public class AndroidApplication : Android.App.Application
{
    public override void OnCreate() 
    {
        base.OnCreate();
        this.ShinyOnCreate(new YourStartup {
            RegisterPlatformServices = services => 
            {
                services.AddSingleton<IYourService, YourService>();
            }
        })
    }
}
```