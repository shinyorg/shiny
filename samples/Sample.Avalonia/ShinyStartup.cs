using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace Sample.Avalonia;


/// <summary>
/// The one place every head registers Shiny - passed to <c>UseShiny</c> from Program.Main (desktop),
/// MainApplication (Android) and AppDelegate (iOS).
/// </summary>
public static class ShinyStartup
{
    public static void Configure(HostBuilder builder)
    {
        builder.Services.AddSingleton<SampleState>();
        builder.Services.AddConnectivity();
#if PLATFORM
        builder.Services.AddNotifications<SampleNotificationDelegate>();
#endif
    }
}
