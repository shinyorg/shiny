using Shiny;
using Microsoft.Extensions.Configuration;

namespace Sample;


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
        builder.Services.AddFirebaseMessaging<MyPushDelegate>(cfg);
#elif NATIVE
        services.AddPush<MyPushDelegate>();
#elif AZURE
        services.AddPushAzureNotificationHubs<MyPushDelegate>(
            builder.Configuration["AzureNotificationHubs:ListenerConnectionString"],
            builder.Configuration["AzureNotificationHubs:HubName"]
        );
#else
        throw new InvalidProgramException("No push provider configuration");
#endif

        return builder.Build();
    }
}

