using System;
using Avalonia;
using Avalonia.Threading;
using Shiny.Hosting;
using Shiny.Infrastructure;

namespace Shiny;


public static class AvaloniaShinyExtensions
{
    /// <summary>
    /// Builds and runs the Shiny host while Avalonia sets up - after its platform services, before your
    /// <c>Application</c> is created - so <see cref="Host.Current"/> is ready in <c>Initialize()</c> and
    /// <c>OnFrameworkInitializationCompleted()</c>.
    /// </summary>
    /// <remarks>
    /// On Android and iOS, call it from <c>CustomizeAppBuilder</c> on your <c>AvaloniaAndroidApplication&lt;TApp&gt;</c> /
    /// <c>AvaloniaAppDelegate&lt;TApp&gt;</c>. Avalonia runs that in <c>Application.OnCreate</c> /
    /// <c>didFinishLaunching</c>, so the host is also up when the OS launches the app in the background
    /// (jobs, push, geofences) with no activity or window. On desktop, call it on the builder in <c>Program.Main</c>.
    /// </remarks>
    /// <param name="builder">The Avalonia app builder</param>
    /// <param name="configure">Register Shiny modules and your own services on <see cref="HostBuilder.Services"/></param>
    public static AppBuilder UseShiny(this AppBuilder builder, Action<HostBuilder>? configure = null)
        => builder.AfterPlatformServicesSetup(_ =>
        {
            SetMainThread();

            var hostBuilder = HostBuilder.Create();
            // HostBuilder only adds the core services on platform targets - a desktop (plain .NET) head needs
            // them too. A second call is a no-op, so the platform builds are unaffected.
            hostBuilder.Services.AddShinyCoreServices();
            configure?.Invoke(hostBuilder);
            hostBuilder.Build().Run();
        });


    static void SetMainThread()
    {
#if WINDOWS
        WindowsPlatform.MainThreadHandler = RunOnUIThread;
#elif !PLATFORM
        // desktop heads get NetPlatform, which has no main thread of its own
        NetPlatform.MainThreadHandler = RunOnUIThread;
#endif
        // Android and Apple marshal through their own main looper / main queue, which is Avalonia's UI thread
    }


    static void RunOnUIThread(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}
