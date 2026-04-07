using Platform.Maui.Linux.Gtk4.Essentials.Hosting;
using Platform.Maui.Linux.Gtk4.Hosting;
using Sample.Linux.Delegates;
using Shiny.Jobs;

namespace Sample.Linux;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppLinuxGtk4<App>()
            .AddLinuxGtk4Essentials()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddBluetoothLE();
        builder.Services.AddBluetoothLeHosting();
        builder.Services.AddBattery();
        builder.Services.AddConnectivity();
        builder.Services.AddNotifications();

        builder.Services.AddJob(
            typeof(SampleJob),
            identifier: "SampleJob",
            runInForeground: true
        );
        builder.Services.AddHttpTransfers<SampleHttpTransferDelegate>();

        return builder.Build();
    }
}
