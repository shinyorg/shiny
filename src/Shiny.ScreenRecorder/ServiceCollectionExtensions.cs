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
    /// <para>Blazor WebAssembly is covered by this package's plain .NET build - see the
    /// <c>AddScreenRecorder</c> documented there. Linux: reference <c>Shiny.ScreenRecorder.Linux</c> and
    /// call <c>AddLinuxScreenRecorder()</c>.</para>
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
    /// Registers <see cref="IScreenRecorder"/>: <c>getDisplayMedia</c> and <c>MediaRecorder</c> on Blazor
    /// WebAssembly (an <c>OperatingSystem.IsBrowser()</c> check), and
    /// <see cref="AddNotSupportedScreenRecorder"/> anywhere else. On Linux, reference
    /// <c>Shiny.ScreenRecorder.Linux</c> and call <c>AddLinuxScreenRecorder()</c> instead - it is a separate
    /// package so its D-Bus dependency never reaches a WebAssembly app.
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
    /// library but have no screen to record. Well-behaved code branches on
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
