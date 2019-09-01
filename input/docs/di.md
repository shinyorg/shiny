Title: Modules, Startable Tasks, and State Restored Services
Order: 2
---

There are lots of little tidbits that help your Shiny applications really shine.  Below are some great additions to the underlying dependency injection engine that help decouple your applications even further.  

## Modules

Modules are handy little things that allows your library to inject other services it may need into the container so that they can be used.  This way, you are able to keep loose coupling on your libraries

```csharp
using Shiny;
using Microsoft.Extensions.DependencyInjection;


public class YourModule : ShinyModule 
{
    public override void Register(IServiceCollection services) 
    {

    }
}
```

## Startup Tasks

Startup tasks are great for wiring up events and spinning up infrastructure.  These fire immediately after the container is built.  However, don't do any sort of blocking operation in them as this will cause your app to pause starting up causing a poor user experience.

```csharp
public class YourStartupTask : IShinyStartupTask
{
    // you can inject into the constructor here as long as you register the service in the sta
    public void Start() 
    {

    }
}
```

## State Restorable Services

This is pretty cool, imagine you want the state of your service preserved across restarts - Shiny does this in epic fashion

Simply turn your service implement INotifyPropertyChanged (or the easy Shiny.NotifyPropertyChanged) and register it in your shiny startup and Shiny will take care of the rest

```csharp
// you can inject into this thing as well add IShinyStartupTask as well
public class MyBadAssService : Shiny.NotifyPropertyChanged, IMyBadAssService, IStartupTask
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