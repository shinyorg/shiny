Title: Advertising
---

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|5|
|UWP|16299|

[![NuGet](https://img.shields.io/nuget/v/Shiny.Beacons.Advertising.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Beacons.Advertising/)


# TODO

<!-- snippet: BeaconAdsStartup.cs -->
<a id='snippet-BeaconAdsStartup.cs'></a>
```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class BeaconAdsStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseBeaconAdvertising();
    }
}
```
<sup><a href='/src/Snippets/BeaconAdsStartup.cs#L1-L10' title='File snippet `BeaconAdsStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-BeaconAdsStartup.cs' title='Navigate to start of snippet `BeaconAdsStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->


