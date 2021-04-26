Title: Platform - UWP
Xref: uwp
Order: 5
---
# UWP

1. Add the following to your App.xaml.cs constructor

```csharp
this.ShinyInit(new YourStartup());
```

Manifest
```xml
<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="general"/>
        <Task Type="systemEvent"/>
        <Task Type="timer"/>
    </BackgroundTasks>
</Extension>
```

```xml
<Capabilities>
    <Capability Name="internetClient" />
    <DeviceCapability Name="bluetooth" />
    <DeviceCapability Name="location" />
</Capabilities>
```