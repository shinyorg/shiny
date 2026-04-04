using Microsoft.Maui.Essentials.MacOS;
using Microsoft.Maui.Platform.MacOS.Hosting;
using Sample.MacOS.Delegates;
using Sample.MacOS.Pages;
using Sample.MacOS.Pages.BLE;

namespace Sample.MacOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppMacOS<App>()
            .AddMacOSEssentials()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;
        s.AddBluetoothLE<SampleBleDelegate>();

        // Pages & ViewModels
        s.AddTransient<MainPage>();
        s.AddTransient<MainViewModel>();
        s.AddTransient<BleScanPage>();
        s.AddTransient<BleScanViewModel>();
        s.AddTransient<BlePeripheralPage>();
        s.AddTransient<BlePeripheralViewModel>();
        s.AddTransient<BleCharacteristicPage>();
        s.AddTransient<BleCharacteristicViewModel>();

        return builder.Build();
    }
}
