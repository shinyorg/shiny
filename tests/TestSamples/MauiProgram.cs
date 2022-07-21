using Prism.DryIoc;

namespace TestSamples;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .UsePrismApp<App>(
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
        //builder.Services.AddBluetoothLeHosting();

        return builder.Build();
    }
}
