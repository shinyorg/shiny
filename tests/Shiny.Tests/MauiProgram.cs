using Microsoft.Extensions.Configuration;
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

        return MauiApp
            .CreateBuilder()
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
                lc.AddAndroid(x => x.OnApplicationCreating(app => Acr.UserDialogs.UserDialogs.Init(app)));
#else
                Acr.UserDialogs.UserDialogs.Init();
#endif
            })
            .Build();
    }
}