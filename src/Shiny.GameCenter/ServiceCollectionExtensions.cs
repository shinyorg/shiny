using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.GameCenter;
using Shiny.GameCenter.Infrastructure;
using Shiny.Net;

namespace Shiny;


public static class GameCenterServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IGameCenterManager"/> - Apple Game Center on iOS, Mac Catalyst and macOS, Google Play
    /// Games Services v2 on Android, and a local-only manager everywhere else.
    /// </summary>
    /// <remarks>
    /// <para><b>Apple:</b> add the <c>com.apple.developer.game-center</c> entitlement, enable Game Center for the app
    /// in App Store Connect and tick it on the app version. <see cref="IGameCenterManager.GetFriendsAsync"/> also needs
    /// <c>NSGKFriendListUsageDescription</c> in Info.plist. Sandboxed macOS apps need
    /// <c>com.apple.security.network.client</c>.</para>
    /// <para><b>Android:</b> add <c>com.google.android.gms.games.APP_ID</c> meta-data to the manifest, pointing at a
    /// string resource holding the numeric project id from the Play Console, and register the SHA-1 of every signing
    /// key (debug, upload, Play App Signing) as an OAuth Android credential.</para>
    /// <para>To use another service, register an <see cref="IGameServicesProvider"/> before calling this.</para>
    /// </remarks>
    public static IServiceCollection AddGameCenter(this IServiceCollection services, Action<GameCenterOptions>? configure = null)
    {
        var options = new GameCenterOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.TryAddSingleton<IGameServicesProvider>(sp => new PlayGamesProvider(
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetRequiredService<ILogger<PlayGamesProvider>>()
        ));
#elif APPLE
        services.TryAddSingleton<IGameServicesProvider>(sp => new GameKitProvider(
            sp.GetRequiredService<GameCenterOptions>(),
            sp.GetRequiredService<ILogger<GameKitProvider>>()
        ));
#else
        services.TryAddSingleton<IGameServicesProvider>(_ => new NotSupportedGameServicesProvider());
#endif

        services.TryAddSingleton(sp => new GameCenterManager(
            sp.GetRequiredService<IGameServicesProvider>(),
            sp.GetRequiredService<GameCenterOptions>(),
            new GameCenterStateStore(GetStatePath(sp)),
            sp.GetRequiredService<ILogger<GameCenterManager>>(),
            sp.GetService<IConnectivity>()
        ));
        services.TryAddSingleton<IGameCenterManager>(sp => sp.GetRequiredService<GameCenterManager>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IShinyStartupTask, GameCenterManager>(
            sp => sp.GetRequiredService<GameCenterManager>()
        ));
        return services;
    }


    static string GetStatePath(IServiceProvider sp)
    {
        var dir = sp.GetService<IPlatform>()?.AppData.FullName
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "shiny");

        return Path.Combine(dir, "shiny_gamecenter.json");
    }
}
