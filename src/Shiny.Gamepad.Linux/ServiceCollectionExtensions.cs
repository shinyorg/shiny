using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Gamepad;

namespace Shiny;


public static class LinuxGamepadServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Linux <see cref="IGamepadManager"/>, backed by evdev.
    /// </summary>
    /// <remarks>
    /// <para>Reading controllers needs read access to <c>/dev/input/event*</c>. On most
    /// distributions that means the user is in the <c>input</c> group; a desktop session usually
    /// gets it through logind's seat ACLs, and a headless service usually does not. Check with
    /// <c>ls -l /dev/input/event*</c> if no controller is found while <c>evtest</c> as root sees
    /// one.</para>
    /// <para><b>Rumble additionally needs write access</b> to the same node, because uploading a
    /// force-feedback effect is a write. The node is opened read-write when permitted and read-only
    /// otherwise, so a controller always works - it just reports no
    /// <see cref="GamepadCapabilities.Vibration"/> where the permission is missing.</para>
    /// <para><b>The light bar needs write access to its sysfs LED</b>, under
    /// <c>/sys/class/leds</c>, which is root-owned by default. A udev rule of the form
    /// <c>ACTION=="add", SUBSYSTEM=="leds", KERNEL=="*:rgb:indicator", RUN+="/bin/chgrp input
    /// /sys/class/leds/%k/multi_intensity"</c> is the usual way to grant it.</para>
    /// <para>Battery readings and motion sensors need no extra permission - both come from sysfs
    /// attributes that are world-readable.</para>
    /// <para>Works on X11, Wayland and with no display server at all: evdev is a kernel interface
    /// and does not care whether anything is on screen.</para>
    /// </remarks>
    public static IServiceCollection AddGamepads(this IServiceCollection services)
    {
        services.AddSingleton<IGamepadManager>(sp => new LinuxGamepadManager(
            sp.GetRequiredService<ILogger<LinuxGamepadManager>>()
        ));

        return services;
    }
}
