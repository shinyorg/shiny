using Microsoft.Maui.Platforms.Linux.Gtk4.Essentials.Hosting;
using Microsoft.Maui.Platforms.Linux.Gtk4.Hosting;
using Sample.Maui;
using Sample.Shared.Maui;

namespace Sample.Linux;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiAppLinuxGtk4<App>()
            .AddLinuxGtk4Essentials()
            .UseSampleShiny();

        // Linux-specific registrations — the cross-platform Shiny packages do not
        // expose these on plain net10.0, so the .Linux packages wire them here.
        var s = builder.Services;
        s.AddBluetoothLE();
        s.AddBluetoothLeHosting();
        s.AddNotifications<SampleNotificationDelegate>();
        s.AddBattery();
        s.AddConnectivity();
        
        s.AddDefaultRepository();
        s.AddStandardHttpTransfers<SampleHttpTransferDelegate>();

        return builder.Build();
    }
}
