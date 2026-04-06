using Microsoft.Maui.Essentials.MacOS;
using Microsoft.Maui.Platform.MacOS.Hosting;
using Sample.MacOS.Delegates;

namespace Sample.MacOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppMacOS<App>()
            .AddMacOSEssentials()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddBluetoothLE<SampleBleDelegate>();
        builder.Services.AddBluetoothLeHosting();
        builder.Services.AddPush<SamplePushDelegate>();
        builder.Services.AddNotifications<SampleNotificationDelegate>();
        builder.Services.AddBattery();
        builder.Services.AddConnectivity();

        return builder.Build();
    }
}
