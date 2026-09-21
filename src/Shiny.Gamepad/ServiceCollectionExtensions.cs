using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Gamepad;

namespace Shiny;


public static class GamepadServiceCollectionExtensions
{
#if PLATFORM
    /// <summary>
    /// Registers <see cref="IGamepadManager"/> for reading game controllers.
    /// </summary>
    /// <remarks>
    /// <para>No permission, entitlement or manifest entry is needed on any platform - controllers
    /// are not a privacy surface. There are platform notes worth knowing all the same.</para>
    /// <para><b>Android:</b> input is delivered to the focused activity, not to a service, so this
    /// package wraps the current activity's window callback to see it. That happens automatically
    /// through Shiny's activity lifecycle and needs nothing from your code. Events are observed and
    /// passed on untouched, so the D-pad still moves focus and B still goes back; set
    /// <c>AndroidGamepadManager.ConsumeEvents</c> to stop that if the app is a game. Rumble,
    /// battery, lights and motion sensors need API 31, and the controller has to expose them.</para>
    /// <para><b>iOS/tvOS/Mac Catalyst/macOS:</b> add <c>GCSupportsControllerUserInteraction</c> to
    /// Info.plist so the system knows the app handles controllers, and list
    /// <c>GCSupportedGameControllers</c> for the profiles you use. Neither is required to read
    /// input, but without them the system keeps some buttons - the Home button above all - for
    /// itself.</para>
    /// <para><b>Windows:</b> needs Windows 10 1903 or later. <c>Windows.Gaming.Input</c> only
    /// delivers input while the app has focus, so a backgrounded window reports every stick centred
    /// and every button up rather than the player's last input.</para>
    /// <para><b>Linux:</b> reference <c>Shiny.Gamepad.Linux</c> instead of this package - it
    /// registers an evdev-backed implementation of the same interface.</para>
    /// <para><b>Blazor WebAssembly:</b> reference <c>Shiny.Gamepad.Blazor</c>.</para>
    /// </remarks>
    public static IServiceCollection AddGamepads(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.AddSingleton<IGamepadManager>(sp => new AndroidGamepadManager(
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetRequiredService<ILogger<AndroidGamepadManager>>()
        ));
#elif APPLE
        services.AddSingleton<IGamepadManager>(sp => new AppleGamepadManager(
            sp.GetRequiredService<ILogger<AppleGamepadManager>>()
        ));
#elif WINDOWS
        services.AddSingleton<IGamepadManager>(sp => new WindowsGamepadManager(
            sp.GetRequiredService<ILogger<WindowsGamepadManager>>()
        ));
#endif
        return services;
    }
#else

    /// <summary>
    /// Registers an <see cref="IGamepadManager"/> that reports no controllers, ever.
    /// </summary>
    /// <remarks>
    /// <para>For server, console and test hosts that resolve <see cref="IGamepadManager"/> from a
    /// shared library but have no controller API underneath. Nothing throws - an app that already
    /// handles "no controller connected" needs no second code path.</para>
    /// <para>Deliberately named differently from <c>AddGamepads</c>: a Linux desktop wants
    /// <c>Shiny.Gamepad.Linux</c> and a Blazor WebAssembly app wants <c>Shiny.Gamepad.Blazor</c>,
    /// and both register a real implementation under the <c>AddGamepads</c> name on this same
    /// target framework. Sharing the name here would make every call from a project referencing one
    /// of them ambiguous.</para>
    /// </remarks>
    public static IServiceCollection AddNotSupportedGamepads(this IServiceCollection services)
    {
        services.AddSingleton<IGamepadManager>(sp => new NotSupportedGamepadManager(
            sp.GetRequiredService<ILogger<NotSupportedGamepadManager>>()
        ));

        return services;
    }
#endif
}
