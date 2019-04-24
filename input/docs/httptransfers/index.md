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
1. Install the NuGet package - [![NuGet](https://img.shields.io/nuget/v/Shiny.HttpTransferTasks.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.HttpTransfers/)

2. In your [Shiny Startup](./startup) - add the following 
```csharp

public override void ConfigureServices(IServiceCollection services)
{
    services.UseHttpTransfers<YourHttpTransferDelegate>();
}
```

3. Implement a background delegate
```csharp
public class YouHttpTransferDelegate : IHttpTransferDelegate
{
    // yes - you can inject anything registered with Shiny into the constructor here

    public void OnError(HttpTransfer transfer, Exception ex)
    {
    }


    public void OnCompleted(HttpTransfer transfer)
    {
    }
}
```

4. Queue up an upload or download by injecting IHttpTransferManager into your viewmodel
```csharp

public class YourViewModel
{
    public YouViewModel(IHttpTransferManager httpManager)
    {
        this.Download = new Command(async () =>
        {

        });

        this.Upload = new Command(async () => 
        {

        });
    }


    public ICommand Download { get; }
    public ICommand Upload { get; }
}
```

5. Additional platform setup
    * [Android](android)
    * [iOS](ios)