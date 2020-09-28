Title: NFC
Description: Near Field Communication
---

[![NuGet](https://img.shields.io/nuget/v/Shiny.Nfc.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Nfc/)

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|5|
|UWP|16299|

<!-- snippet: NfcStartup.cs -->
<a id='snippet-NfcStartup.cs'></a>
```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class NfcStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseNfc();
    }
}
```
<sup><a href='/src/Snippets/NfcStartup.cs#L1-L10' title='File snippet `NfcStartup.cs` was extracted from'>snippet source</a> | <a href='#snippet-NfcStartup.cs' title='Navigate to start of snippet `NfcStartup.cs`'>anchor</a></sup>
<!-- endSnippet -->


<!-- snippet: NfcUsage.cs -->
<a id='snippet-NfcUsage.cs'></a>
```cs
using Shiny;
using Shiny.Nfc;

public class NfcUsage
{
    public void ContinuousScan()
    {
        //ShinyHost
        //    .Resolve<INfcManager>()
        //    .ContinuousRead()
        //    .Subscribe(x =>
        //    {

        //    });
    }


    public void SingleRead()
    {
        //ShinyHost
        //    .Resolve<INfcManager>()
        //    .SingleRead()
        //    .Subscribe(x =>
        //    {

        //    });
    }
}
```
<sup><a href='/src/Snippets/NfcUsage.cs#L1-L28' title='File snippet `NfcUsage.cs` was extracted from'>snippet source</a> | <a href='#snippet-NfcUsage.cs' title='Navigate to start of snippet `NfcUsage.cs`'>anchor</a></sup>
<!-- endSnippet -->
