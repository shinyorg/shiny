Title: Getting Started
Order: 1
RedirectFrom: docs/httptransfers/index
---

## DESCRIPTION

As developers, we often take internet connectivity and speed for granted.  Mobile phones are taking HUGE sized photos these days that can take quite some time to upload.

## FEATURES

* Background Uploads & Downloads on each platform
* Supports transfer filtering based on metered connections (iOS & UWP only at the moment)
* Event Based Metrics
  * Percentage Complete
  * Total Bytes Expected
  * Total Bytes Transferred
  * Transfer Speed (Bytes Per Second)
  * Estimated Completion Time

## PLATFORMS

|Platform|Version|
|--------|-------|
iOS|7+
Android|4.3+
Windows UWP|16299+

## USAGE

|Area|Info|
|----|----|
|NuGet| [![HttpNugetShield]][HttpNuget] |
|Shiny Startup|services.UseHttpTransfers<YourHttpDelegate>()|
|Main Service|Shiny.Net.Http.IHttpTransferManager|
|Background Delegate (required)|Shiny.Net.Http.IHttpTransferDelegate (required)|
|Static Generated|ShinyHttpTransfers|
|Manual Resolve|ShinyHost.Resolve<Shiny.Net.Http.IHttpTransferManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.Net.Http.IHttpTransferManager>()|

<?! Include "../../nuget.md" /?>