using Shiny;
using Microsoft.Extensions.Configuration;

namespace Sample.Push.Maui;


public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()            
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Configuration.AddJsonPlatformBundle();
#if FIREBASE
        var cfg = builder
            .Configuration
            .GetSection("firebase")
            .Get<Shiny.Push.FirebaseConfiguration>();
        builder.Services.AddFirebaseMessaging(cfg);
#elif NATIVE
#elif AZURE
#else
        throw new InvalidProgramException("No push provider configuration");
#endif

        return builder.Build();
    }
}

