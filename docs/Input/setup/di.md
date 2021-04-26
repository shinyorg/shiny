Title: Dependency Injection
Order: 99
---

## Modules

Modules are handy little things that allows your library to inject other services it may need into the container so that they can be used.  This way, you are able to keep loose coupling on your libraries

```cs
using Shiny;
using Microsoft.Extensions.DependencyInjection;

public class YourModule : ShinyModule
{
    public override void Register(IServiceCollection services, IPlatform platform)
    {

    }
}
```

And then to register this with your startup

<?! Startup ?>
services.RegisterModule(new YourModule());
<?!/ Startup ?>



## Startup Tasks

Startup tasks are great for wiring up events and spinning up infrastructure.  These fire immediately after the container is built.  However, don't do any sort of blocking operation in them as this will cause your app to pause starting up causing a poor user experience.

**CAUTION:** You can do async processes here, but your app could start before the process finishes, so if you are pre-loading data, it may not be ready for use before your app starts

```cs
using Shiny;

public class YourStartupTask : IShinyStartupTask
{
    // you can inject into the constructor here as long as you register the service in the startup
    public void Start()
    {

    }
}
```

To register them and have them automatically run:

<?! Startup ?>
services.AddSingleton<YourStartupTask>();
<?!/ Startup ?>

## State Restorable Services

This is pretty cool, imagine you want the state of your service preserved across restarts - Shiny does this in epic fashion

Simply turn your service implement INotifyPropertyChanged (or the easy Shiny.NotifyPropertyChanged) and register it in your shiny startup and Shiny will take care of the rest

```cs
using Shiny;

public class MyBadAssService : NotifyPropertyChanged, IShinyStartupTask
{
    int count;

    public int Count
    {
        get => this.count;
        set => this.Set(ref this.count, value);
    }

    public void Start()
    {
        this.Count++;
    }
}
```

and the same to register this guy

<?! Startup ?>
services.AddSingleton<MyBadAssService>();
<?!/ Startup ?>
