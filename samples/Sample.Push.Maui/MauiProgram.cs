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

        builder.Configuration.AddJsonPlatformBundle(optional: false);
        builder.Services.AddSingleton<SampleSqliteConnection>();

        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<SetupPage>();
        builder.Services.AddTransient<SetupViewModel>();
        builder.Services.AddTransient<LogsPage>();
        builder.Services.AddTransient<LogsViewModel>();
        builder.Services.AddTransient<TagsPage>();
        builder.Services.AddTransient<TagsViewModel>();

        var cfg = builder.Configuration.GetSection("Firebase");
#if ANDROID
        var firebaseCfg = new FirebaseConfig(
            false,
            cfg["AppId"],
            cfg["SenderId"],
            cfg["ProjectId"],
            cfg["ApiKey"]
        );
#endif

#if NATIVE
        builder.Services.AddPush<MyPushDelegate>(
#if ANDROID
            firebaseConfig
#endif
        );
#elif AZURE
        var azureCfg = builder.Configuration.GetSection("AzureNotificationHubs");
        builder.Services.AddPushAzureNotificationHubs<MyPushDelegate>(
            azureCfg["ListenerConnectionString"]!,
            azureCfg["HubName"]!
#if ANDROID
            , firebaseCfg
#endif
        );

#elif FIREBASE
        builder.Services.AddPushFirebaseMessaging<MyPushDelegate>(new (
            false,
            cfg["AppId"],
            cfg["SenderId"],
            cfg["ProjectId"],
            cfg["ApiKey"]
        ));
#else
        throw new InvalidProgramException("No push provider configuration");
#endif

        return builder.Build();
    }
}

