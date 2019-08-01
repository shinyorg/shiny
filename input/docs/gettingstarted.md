Title: Getting Started
Order: 1
RedirectFrom:
  - index
---

## Platform Setup

 TODO: Links to all nuget packages on nuget/myget
 
### Android


```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
{
  AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}

```
# Shiny Startup

Startup is the place where you wire up all of the necessary application depedencies you need


Out of the box, Shiny automatically pushes the following on to the service container

* IEnvironment
* IPowerManager
* IJobManager
* ISettings

## Modules

```csharp

public class YourModule : Shiny.ShinyModule 
{
    public override void () 
    {

    }
}
```

## Startup Tasks

