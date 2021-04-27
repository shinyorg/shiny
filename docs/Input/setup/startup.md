Title: Startup
Order: 0
Xref: startup
---

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny;


namespace Samples
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureLogging(ILoggingBuilder builder, IPlatform platform)
        {
            builder.AddFirebase(LogLevel.Warning);
            builder.AddAppCenter("YourAppCenterKey", LogLevel.Warning);
        }


        public override void ConfigureServices(IServiceCollection services, IPlatform platform)
        {
        }

    }
}

```

<?# NugetShield "Shiny.Logging.AppCenter" /?>
<?# NugetShield "Shiny.Logging.Firebase" /?>