using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Xunit.Runners.Maui;

namespace Shiny.Tests;


public static class MauiProgram
{
    public static IConfiguration Configuration { get; private set; } = null!;


    public static MauiApp CreateMauiApp()
    {
        Configuration = new ConfigurationBuilder()
            .AddJsonPlatformBundle(optional: false)
            .Build();

        var builder = MauiApp.CreateBuilder();

        builder
            .ConfigureTests(new TestOptions
            {
                Assemblies =
                {
                    typeof(MauiProgram).Assembly
                }
            })
            .UseShiny() // this is somewhat of a hack as it hooks the shiny events BUT to the current host provider
            .UseVisualRunner()
            .ConfigureLifecycleEvents(lc =>
            {
#if ANDROID
                lc.AddAndroid(x => x
                    .OnCreate((_, _) => DeviceDisplay.KeepScreenOn = true)
                );
#else
                DeviceDisplay.KeepScreenOn = true;
#endif
            });

        builder.Logging.AddDebug();

        return builder.Build();
    }
}