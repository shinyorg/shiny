Title: Modules, Startable Tasks, and State Restored Services
Order: 2
---

There are lots of little tidbits that help your Shiny applications really shine.  Below are some great additions to the underlying dependency injection engine that help decouple your applications even further.  

## Modules

Modules are handy little things that allows your library to inject other services it may need into the container so that they can be used.  This way, you are able to keep loose coupling on your libraries

<!-- snippet: YourModule.cs -->
<a id='snippet-YourModule.cs'/></a>
```cs
using Shiny;
using Microsoft.Extensions.DependencyInjection;

public class YourModule : ShinyModule
{
    public override void Register(IServiceCollection services)
    {

    }
}
```
<sup><a href='/src/Snippets/YourModule.cs#L1-L10' title='File snippet `YourModule.cs` was extracted from'>snippet source</a> | <a href='#snippet-YourModule.cs' title='Navigate to start of snippet `YourModule.cs`'>anchor</a></sup>
<!-- endsnippet -->

## Startup Tasks

Startup tasks are great for wiring up events and spinning up infrastructure.  These fire immediately after the container is built.  However, don't do any sort of blocking operation in them as this will cause your app to pause starting up causing a poor user experience.

<!-- snippet: YourStartupTask.cs -->
<a id='snippet-YourStartupTask.cs'/></a>
```cs
using Shiny;

public class YourStartupTask : IShinyStartupTask
{
    // you can inject into the constructor here as long as you register the service in the sta
    public void Start()
    {

    }
}
```
<sup><a href='/src/Snippets/YourStartupTask.cs#L1-L10' title='File snippet `YourStartupTask.cs` was extracted from'>snippet source</a> | <a href='#snippet-YourStartupTask.cs' title='Navigate to start of snippet `YourStartupTask.cs`'>anchor</a></sup>
<!-- endsnippet -->

## State Restorable Services

This is pretty cool, imagine you want the state of your service preserved across restarts - Shiny does this in epic fashion

Simply turn your service implement INotifyPropertyChanged (or the easy Shiny.NotifyPropertyChanged) and register it in your shiny startup and Shiny will take care of the rest

<!-- snippet: RestorableServices -->
<a id='snippet-restorableservices'/></a>
```cs
public class MyBadAssService :
    NotifyPropertyChanged,
    IMyBadAssService,
    IShinyStartupTask
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
<sup><a href='/src/Snippets/RestorableServices.cs#L3-L23' title='File snippet `restorableservices` was extracted from'>snippet source</a> | <a href='#snippet-restorableservices' title='Navigate to start of snippet `restorableservices`'>anchor</a></sup>
<!-- endsnippet -->
