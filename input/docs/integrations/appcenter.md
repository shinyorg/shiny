Title: App Center Logging
---

# SETUP

Install from NuGet: [![NuGet](https://img.shields.io/nuget/v/Shiny.Logging.AppCenter.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Logging.AppCenter/)


## Setup

```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class AppCenterStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseAppCenterLogging("android=YourAppSecret;ios=YourAppSecret");
    }
}
```