Title: Native
Order: 2
Xref: pushnative
---

Native push is the root of all other push providers.  It works at the native OS level and is usually feeding other providers like Azure Notification Hubs and Firebase.  As such, the setup instructions found
within this document generally apply to all.

<?! PackageInfo "Shiny.Push" /?>

## Native Push Provider

If you don't intend to use a 3rd party provider, this library still uses everything applied from "Getting Started".  To register it with Shiny, simply do the following in your Shiny Startup:

```csharp
services.UsePush<MyPushDelegate>();
```

## Android Additional Setup
The process of setting up Android is a bit of process

```csharp
<ItemGroup>
    <GoogleServicesJson Include="google-services.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </GoogleServicesJson>
    <PackageReference Include="Xamarin.GooglePlayServices.Basement" Version="The same version Shiny.Push is using" />
    <PackageReference Include="Xamarin.GooglePlayServices.Tasks" Version="The same version Shiny.Push is using" />
</ItemGroup>

```