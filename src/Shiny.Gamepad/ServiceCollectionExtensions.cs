using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if !PLATFORM
using Microsoft.JSInterop;
#endif
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
    /// <para><b>Linux and Blazor WebAssembly</b> are covered by this package's plain .NET build -
    /// see the <c>AddGamepads</c> documented there.</para>
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
    /// Registers <see cref="IGamepadManager"/> for reading game controllers, choosing the backend at
    /// runtime: the W3C Gamepad API on Blazor WebAssembly, evdev on Linux, and a manager that reports
    /// no controllers anywhere else (plain .NET on Windows or macOS has no controller API).
    /// </summary>
    /// <remarks>
    /// <para><b>Blazor WebAssembly - a controller is invisible to the page until the player presses
    /// a button on it.</b> Browsers hide connected controllers from a page that has not seen input
    /// from one, because the list of what is plugged in is a fingerprinting signal. There is no
    /// permission to ask for and no way around it - design the first screen to say "press any button
    /// on your controller", not "no controller found". Vibration needs <c>vibrationActuator</c>, which
    /// Chrome and Edge implement and Firefox and Safari do not; battery, motion and lights are not in
    /// the specification at all. Call <see cref="BlazorGamepadManager.Probe"/> at start-up to tell
    /// "this browser has no Gamepad API" apart from "no controller has been used yet".</para>
    /// <para><b>Linux</b> reading controllers needs read access to <c>/dev/input/event*</c>. On most
    /// distributions that means the user is in the <c>input</c> group; a desktop session usually
    /// gets it through logind's seat ACLs, and a headless service usually does not. Check with
    /// <c>ls -l /dev/input/event*</c> if no controller is found while <c>evtest</c> as root sees
    /// one. <b>Rumble additionally needs write access</b> to the same node, because uploading a
    /// force-feedback effect is a write. The node is opened read-write when permitted and read-only
    /// otherwise, so a controller always works - it just reports no
    /// <see cref="GamepadCapabilities.Vibration"/> where the permission is missing. <b>The light bar
    /// needs write access to its sysfs LED</b>, under <c>/sys/class/leds</c>, which is root-owned by
    /// default. A udev rule of the form <c>ACTION=="add", SUBSYSTEM=="leds",
    /// KERNEL=="*:rgb:indicator", RUN+="/bin/chgrp input /sys/class/leds/%k/multi_intensity"</c> is
    /// the usual way to grant it. Battery readings and motion sensors need no extra permission. Works
    /// on X11, Wayland and with no display server at all.</para>
    /// </remarks>
    public static IServiceCollection AddGamepads(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
        if (OperatingSystem.IsBrowser())
        {
            services.AddSingleton<IGamepadManager>(sp => new BlazorGamepadManager(
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<ILogger<BlazorGamepadManager>>()
            ));
        }
        else if (OperatingSystem.IsLinux())
        {
            services.AddSingleton<IGamepadManager>(sp => new LinuxGamepadManager(
                sp.GetRequiredService<ILogger<LinuxGamepadManager>>()
            ));
        }
        else
        {
            services.AddNotSupportedGamepads();
        }
        return services;
    }


    /// <summary>
    /// Registers an <see cref="IGamepadManager"/> that reports no controllers, ever.
    /// </summary>
    /// <remarks>
    /// For server, console and test hosts that resolve <see cref="IGamepadManager"/> from a shared
    /// library but have no controller API underneath - including on Linux, where
    /// <c>AddGamepads</c> would read evdev. Nothing throws - an app that already handles "no
    /// controller connected" needs no second code path.
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
