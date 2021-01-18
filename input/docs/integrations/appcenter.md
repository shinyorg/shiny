Title: App Center
---

## DESCRIPTION

Logging is important for you to successfully develop your applications and monitor them in production.  Shiny has it's own built in logging framework and was designed to plug into various providers.  This is the plugin for AppCenter.

## SETUP

1. Install from NuGet: [![AppCenterNugetShield]][AppCenterNuget]

2. In your Shiny Startup (NOTE: generated startups do not auto-register this for you)

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

<?! Include "../../nuget.md" /?>