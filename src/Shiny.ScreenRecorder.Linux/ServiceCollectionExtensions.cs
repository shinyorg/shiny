using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.ScreenRecorder;

namespace Shiny;


public static class LinuxScreenRecorderServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Linux <see cref="IScreenRecorder"/>, backed by the xdg-desktop-portal
    /// ScreenCast API and an external encoder.
    /// </summary>
    /// <remarks>
    /// <para>Needs a desktop session with a running <c>xdg-desktop-portal</c> implementing
    /// ScreenCast - GNOME, KDE Plasma and the wlroots portal all do - plus one of:</para>
    /// <list type="bullet">
    /// <item><description><c>gst-launch-1.0</c> with the good and bad plugin sets
    /// (<c>gstreamer1.0-tools gstreamer1.0-plugins-good gstreamer1.0-plugins-bad
    /// gstreamer1.0-pipewire</c>) - works on Wayland and X11.</description></item>
    /// <item><description><c>ffmpeg</c> - X11 only, and captures the whole display with no
    /// picker.</description></item>
    /// </list>
    /// <para>Audio needs a reachable PulseAudio or PipeWire-Pulse server and the <c>pactl</c>
    /// tool, which is what resolves the monitor source for system audio.</para>
    /// <para>Flatpak-sandboxed hosts are not supported - reaching PipeWire from a sandbox needs the
    /// portal's file descriptor passed to the encoder, which a child process cannot inherit
    /// here.</para>
    /// <para>Everything above is probed at runtime. On a machine missing the pieces the
    /// registration still succeeds and <see cref="IScreenRecorder.Capabilities"/> reports
    /// <see cref="ScreenRecorderCapabilities.None"/>.</para>
    /// </remarks>
    public static IServiceCollection AddScreenRecorder(this IServiceCollection services)
    {
        services.AddSingleton<IScreenRecorder>(sp => new LinuxScreenRecorder(
            sp.GetRequiredService<ILogger<LinuxScreenRecorder>>()
        ));

        return services;
    }
}
