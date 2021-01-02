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
<sup>[snippet source](/src/Snippets/NfcStartup.cs#L1-L10)</sup>
<!-- endsnippet -->


<!-- snippet: NfcUsage.cs -->
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
<sup>[snippet source](/src/Snippets/NfcUsage.cs#L1-L28)</sup>
<!-- endsnippet -->
