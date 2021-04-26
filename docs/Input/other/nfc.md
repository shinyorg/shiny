Title: NFC
Description: Near Field Communication
---

## DESCRIPTION

Near Field Communication (NFC) 


## PLATFORMS

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|8|
|UWP|16299|

## USAGE

|Area|Info|
|----|----|
|NuGet| [![NfcNugetShield]][NfcNuget] |
|Shiny Startup|services.UseNfc|
|Shiny Delegate|None|
|Main Service|Shiny.Nfc.INfcManager|
|Static Generated|ShinyNfc|
|Manual Resolve|ShinyHost.Resolve<Shiny.Nfc.INfcManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.Nfc.INfcManager>()|


## HOW TO

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