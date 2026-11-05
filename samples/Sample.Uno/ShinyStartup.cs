using Microsoft.Extensions.DependencyInjection;
using Shiny;

namespace Sample.Uno;


/// <summary>
/// The Shiny registrations every head shares - passed to <c>UseShiny</c> in <see cref="App.ConfigureApp"/>. It is the
/// Uno.Extensions container, so view models inject these like any other service.
/// </summary>
public static class ShinyStartup
{
    public static void Configure(IServiceCollection services)
    {
        services.AddSingleton<SampleState>();
        services.AddSingleton<IShinyStartupTask, StartupRecorder>();
        services.AddConnectivity();
        services.AddBattery();
#if PLATFORM
        services.AddNotifications<SampleNotificationDelegate>();
#endif
    }
}
