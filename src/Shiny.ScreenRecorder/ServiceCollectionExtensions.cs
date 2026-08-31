using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// there is no way to suppress it, and there should not be.</para>
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
    /// <para>Linux: reference <c>Shiny.ScreenRecorder.Linux</c> instead of this package - it
    /// registers a portal-backed implementation of the same interface.</para>
    /// <para>Blazor WebAssembly: reference <c>Shiny.ScreenRecorder.Blazor</c>.</para>
    /// </remarks>
    public static IServiceCollection AddScreenRecorder(this IServiceCollection services)
    {
        // registered by factory rather than by type so nothing here needs reflection under AOT
#if ANDROID
        services.AddSingleton<IScreenRecorder>(sp => new AndroidScreenRecorder(
            sp.GetRequiredService<AndroidPlatform>(),
            sp.GetRequiredService<ILogger<AndroidScreenRecorder>>()
        ));
#elif IOS || MACCATALYST
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
    /// Registers an <see cref="IScreenRecorder"/> that reports
    /// <see cref="ScreenRecorderCapabilities.None"/> and throws on every call.
    /// </summary>
    /// <remarks>
    /// <para>For server, console and test hosts that resolve <see cref="IScreenRecorder"/> from a
    /// shared library but have no screen to record. Well-behaved code branches on
    /// <see cref="IScreenRecorder.Capabilities"/> and never reaches the throw.</para>
    /// <para>Deliberately named differently from <c>AddScreenRecorder</c>: a Linux desktop wants
    /// <c>Shiny.ScreenRecorder.Linux</c> and a Blazor WebAssembly app wants
    /// <c>Shiny.ScreenRecorder.Blazor</c>, and both of those register a real implementation under
    /// the <c>AddScreenRecorder</c> name on this same target framework. Sharing the name here would
    /// make every call from a project referencing one of them ambiguous.</para>
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
