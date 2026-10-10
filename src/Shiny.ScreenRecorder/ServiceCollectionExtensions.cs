using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if !PLATFORM
using Microsoft.JSInterop;
#endif
using Shiny.ScreenRecorder;

namespace Shiny;


public static class ScreenRecorderServiceCollectionExtensions
{
#if PLATFORM
    /// <summary>
    /// Registers <see cref="IScreenRecorder"/> for recording the screen to a video file.
    /// </summary>
    /// <remarks>
    /// <para>Android: manifest needs <c>FOREGROUND_SERVICE</c> and
    /// <c>FOREGROUND_SERVICE_MEDIA_PROJECTION</c>, plus <c>RECORD_AUDIO</c> when capturing audio.
    /// The <c>ScreenRecorderService</c> and <c>ScreenCapturePermissionActivity</c> in this package
    /// are merged into your manifest automatically. Recording always shows the OS cast indicator -
    /// there is no way to suppress it, and there should not be. The recording's own notification
    /// is yours to word: register an <c>IScreenRecordingNotificationDelegate</c>.</para>
    /// <para>iOS/Mac Catalyst: no entitlement is needed to record your own app, but
    /// <c>NSMicrophoneUsageDescription</c> is required in Info.plist when
    /// <see cref="ScreenRecordingRequest.IncludeMicrophone"/> is used. ReplayKit records the app's
    /// own UI only and requires the app to be in the foreground.</para>
    /// <para>macOS: needs the Screen Recording grant in System Settings, which the OS prompts for
    /// once. Add <c>NSMicrophoneUsageDescription</c> for the microphone and
    /// <c>com.apple.security.device.audio-input</c> when sandboxed.</para>
    /// <para>Windows: needs Windows 10 1903 or later. Packaged apps declare the
    /// <c>graphicsCapture</c> capability. There is no audio - see
    /// <see cref="ScreenRecorderCapabilities"/>.</para>
    /// <para>Linux and Blazor WebAssembly are covered by this package's plain .NET build - see the
    /// <c>AddScreenRecorder</c> documented there.</para>
    /// </remarks>
    public static IServiceCollection AddScreenRecorder(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.AddSingleton<IScreenRecorder>(sp => new AndroidScreenRecorder(
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetRequiredService<ILogger<AndroidScreenRecorder>>()
        ));
#elif IOS || MACCATALYST || TVOS
        services.AddSingleton<IScreenRecorder>(sp => new AppleScreenRecorder(
            sp.GetRequiredService<ILogger<AppleScreenRecorder>>()
        ));
#elif MACOS
        services.AddSingleton<IScreenRecorder>(sp => new MacOSScreenRecorder(
            sp.GetRequiredService<ILogger<MacOSScreenRecorder>>()
        ));
#elif WINDOWS
        services.AddSingleton<IScreenRecorder>(sp => new WindowsScreenRecorder(
            sp.GetRequiredService<ILogger<WindowsScreenRecorder>>()
        ));
#endif
        return services;
    }
#else

    /// <summary>
    /// Registers <see cref="IScreenRecorder"/>, choosing the backend at runtime: <c>getDisplayMedia</c>
    /// and <c>MediaRecorder</c> on Blazor WebAssembly, the xdg-desktop-portal ScreenCast API with an
    /// external encoder on Linux, and <see cref="AddNotSupportedScreenRecorder"/> anywhere else
    /// (plain .NET on Windows or macOS has no screen capture API).
    /// </summary>
    /// <remarks>
    /// <para><b>Blazor WebAssembly:</b> the page must be served over HTTPS (or from localhost), and
    /// <see cref="IScreenRecorder.Start"/> must be reached from a user gesture such as a button
    /// click - browsers refuse <c>getDisplayMedia</c> otherwise. When the app is hosted in an iframe,
    /// the frame needs <c>allow="display-capture; microphone"</c> or the picker never appears.
    /// Support varies by browser, so call <see cref="BlazorScreenRecorder.Probe"/> once at startup -
    /// until it has run, <see cref="IScreenRecorder.Capabilities"/> reports
    /// <see cref="ScreenRecorderCapabilities.None"/> because feature detection needs a JS round trip
    /// that a synchronous property cannot make.</para>
    /// <para><b>Linux:</b> needs a desktop session with a running <c>xdg-desktop-portal</c>
    /// implementing ScreenCast - GNOME, KDE Plasma and the wlroots portal all do - plus either
    /// <c>gst-launch-1.0</c> with the good and bad plugin sets (<c>gstreamer1.0-tools
    /// gstreamer1.0-plugins-good gstreamer1.0-plugins-bad gstreamer1.0-pipewire</c>, Wayland and X11)
    /// or <c>ffmpeg</c> (X11 only, whole display, no picker). Audio needs a reachable PulseAudio or
    /// PipeWire-Pulse server and the <c>pactl</c> tool. Flatpak-sandboxed hosts are not supported.
    /// Everything is probed at runtime: on a machine missing the pieces the registration still
    /// succeeds and <see cref="IScreenRecorder.Capabilities"/> reports
    /// <see cref="ScreenRecorderCapabilities.None"/>.</para>
    /// </remarks>
    public static IServiceCollection AddScreenRecorder(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
        if (OperatingSystem.IsBrowser())
        {
            services.AddSingleton<IScreenRecorder>(sp => new BlazorScreenRecorder(
                sp.GetRequiredService<IJSRuntime>(),
                sp.GetRequiredService<ILogger<BlazorScreenRecorder>>()
            ));
        }
        else if (OperatingSystem.IsLinux())
        {
            services.AddSingleton<IScreenRecorder>(sp => new LinuxScreenRecorder(
                sp.GetRequiredService<ILogger<LinuxScreenRecorder>>()
            ));
        }
        else
        {
            services.AddNotSupportedScreenRecorder();
        }
        return services;
    }


    /// <summary>
    /// Registers an <see cref="IScreenRecorder"/> that reports
    /// <see cref="ScreenRecorderCapabilities.None"/> and throws on every call.
    /// </summary>
    /// <remarks>
    /// For server, console and test hosts that resolve <see cref="IScreenRecorder"/> from a shared
    /// library but have no screen to record - including on Linux, where <c>AddScreenRecorder</c>
    /// would use the desktop portal. Well-behaved code branches on
    /// <see cref="IScreenRecorder.Capabilities"/> and never reaches the throw.
    /// </remarks>
    public static IServiceCollection AddNotSupportedScreenRecorder(this IServiceCollection services)
    {
        services.AddSingleton<IScreenRecorder>(sp => new NotSupportedScreenRecorder(
            sp.GetRequiredService<ILogger<NotSupportedScreenRecorder>>()
        ));

        return services;
    }
#endif
}
