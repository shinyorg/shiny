using Prism.DryIoc;
using Sample.Maui.Infrastructure;

namespace Sample.Maui;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .UsePrism(
                new DryIocContainerExtension(),
                prism => prism
                    .RegisterTypes(registry =>
                    {
                        registry.RegisterForNavigation<LogsPage, LogsViewModel>();
                        //registry.RegisterForNavigation<BleHostPage, BleHostViewModel>();
                    })
                    .OnAppStart("NavigationPage/LogsPage")
            );

        builder.Logging.AddDebug();
        builder.Services.AddSingleton<SampleSqlConnection>();
        builder.Services.AddShinyService<PlatformStateTests>();
        builder.Services.AddConnectivity();
        builder.Services.AddBattery();
        builder.Services.AddBluetoothLeHosting();

        return builder.Build();
    }
}
