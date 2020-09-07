Title: HTTP Transfers
Description: Background Uploads & Downloads with Metrics
---

## FEATURES

* Background Uploads & Downloads on each platform
* Supports transfer filtering based on metered connections (iOS & UWP only at the moment)
* Event Based Metrics
  * Percentage Complete
  * Total Bytes Expected
  * Total Bytes Transferred
  * Transfer Speed (Bytes Per Second)
  * Estimated Completion Time

## SUPPORTED PLATFORMS
|Platform|Version|
|--------|-------|
iOS|7+
Android|4.3+
Windows UWP|16299+
.NET Standard|2.0

## SETUP
1. Install the NuGet package - [![NuGet](https://img.shields.io/nuget/v/Shiny.Net.Http.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Net.Http/)

2. In your [Shiny Startup](./startup) - add the following 

snippet: HttpTransferDelegate.cs

snippet: HttpTransferStartup.cs

snippet: HttpTransferUsage.cs


4. Queue up an upload or download by injecting IHttpTransferManager into your viewmodel


5. Additional platform setup
    * [Android](android)
    * [iOS](ios)