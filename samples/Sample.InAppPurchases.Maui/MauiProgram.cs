using Microsoft.Extensions.Logging;
using Shiny;

namespace Sample.InAppPurchases.Maui;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .UseShiny();

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif

        builder.Services.AddInAppPurchases<SamplePurchaseDelegate>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<PurchaseApi>();
        builder.Services.AddSingleton<EntitlementService>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
