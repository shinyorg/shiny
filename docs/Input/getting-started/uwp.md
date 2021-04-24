Title: UWP
---
# UWP

1. Add the following to your App.xaml.cs constructor

```csharp
this.ShinyInit(new YourStartup());
```

2. Add the following to your Package.appxmanifest under the <Application><Extensions> node

```xml
<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="general"/>
        <Task Type="systemEvent"/>
        <Task Type="timer"/>
    </BackgroundTasks>
</Extension>
```