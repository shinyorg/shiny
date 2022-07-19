using Prism.DryIoc;
using Shiny;

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
                        registry.RegisterForNavigation<BleHostPage, BleHostViewModel>();
                    })
                    .OnAppStart("NavigationPage/BleHostPage")
            );

        builder.Services.AddBluetoothLeHosting();

        return builder.Build();
    }
}
