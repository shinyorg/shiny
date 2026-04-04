using Platform.Maui.Linux.Gtk4.Essentials.Hosting;
using Platform.Maui.Linux.Gtk4.Hosting;
using Sample.Linux.Pages;
using Sample.Linux.Pages.BLE;

namespace Sample.Linux;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppLinuxGtk4<App>()
            .AddLinuxGtk4Essentials()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var s = builder.Services;
        s.AddBluetoothLE();

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
