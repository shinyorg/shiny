using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Shiny.Gamepad;

namespace Shiny;


public static class BlazorGamepadServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Blazor WebAssembly <see cref="IGamepadManager"/>, backed by the W3C Gamepad
    /// API.
    /// </summary>
    /// <remarks>
    /// <para><b>A controller is invisible to the page until the player presses a button on it.</b>
    /// Browsers hide connected controllers from a page that has not seen input from one, because
    /// the list of what is plugged in is a fingerprinting signal. There is no permission to ask
    /// for and no way around it - design the first screen to say "press any button on your
    /// controller", not "no controller found".</para>
    /// <para>Vibration needs <c>vibrationActuator</c>, which Chrome and Edge implement and Firefox
    /// and Safari do not. It is reported per controller through
    /// <see cref="GamepadCapabilities.Vibration"/>; battery, motion and lights are not in the
    /// specification at all and are never available.</para>
    /// <para>Call <see cref="BlazorGamepadManager.Probe"/> at start-up to tell "this browser has no
    /// Gamepad API" apart from "no controller has been used yet" - the two look identical
    /// otherwise.</para>
    /// </remarks>
    public static IServiceCollection AddGamepads(this IServiceCollection services)
    {
        services.AddSingleton<IGamepadManager>(sp => new BlazorGamepadManager(
            sp.GetRequiredService<IJSRuntime>(),
            sp.GetRequiredService<ILogger<BlazorGamepadManager>>()
        ));

        return services;
    }
}
