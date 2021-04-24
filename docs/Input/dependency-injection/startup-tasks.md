# Startup Tasks

Startup tasks are great for wiring up events and spinning up infrastructure.  These fire immediately after the container is built.  However, don't do any sort of blocking operation in them as this will cause your app to pause starting up causing a poor user experience.

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