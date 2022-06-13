using Microsoft.Extensions.Configuration;
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
            .Build();
    }
}