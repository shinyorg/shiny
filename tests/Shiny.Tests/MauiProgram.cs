using Acr.UserDialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.LifecycleEvents;
using Xunit.Runners.Maui;

namespace Shiny.Tests;


public static class MauiProgram
{
    public static IConfiguration Configuration { get; private set; }


    public static MauiApp CreateMauiApp()
    {
        //Configuration = new ConfigurationBuilder()
        //    .AddJsonPlatformBundle(optional: false)
        //    .Build();

        return MauiApp
            .CreateBuilder()
            .ConfigureTests(new TestOptions
            {
                Assemblies =
                {
                    typeof(MauiProgram).Assembly
                }
            })
            .UseVisualRunner()
            .ConfigureLifecycleEvents(lc =>
            {
#if ANDROID
                lc.AddAndroid(x => x.OnApplicationCreating(app => UserDialogs.Init(app)));
#elif IOS
                UserDialogs.Init();
#endif
            })
            .Build();
    }
}