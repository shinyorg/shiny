using System;
using Microsoft.UI.Xaml;

namespace Shiny.Infrastructure;


/// <summary>
/// Owns the process' one <see cref="ShinyApplicationBuilder"/>, so the Android background path
/// (<c>Application.OnCreate</c>) and the foreground path (<c>OnLaunched</c>) share a single builder, window and host.
/// </summary>
static class ShinyUnoHost
{
    static readonly object sync = new();
    static ShinyApplicationBuilder? builder;


    internal static ShinyApplicationBuilder GetOrCreateBuilder<TApp>() where TApp : Application, IShinyUnoApplication
    {
        lock (sync)
        {
            if (builder == null)
            {
                var b = new ShinyApplicationBuilder(typeof(TApp).Assembly);
                TApp.ConfigureApp(b);
                builder = b;
            }
            return builder;
        }
    }


    /// <summary>
    /// Called by <see cref="ShinyUnoServiceInitializer"/> when a container is built. False when Shiny already runs on
    /// this one - <c>UseShiny()</c> registered twice, or a host built again by hand.
    /// </summary>
    internal static bool TryClaim(IServiceProvider provider)
        => !(Hosting.Host.IsInitialized && ReferenceEquals(Hosting.Host.Current.Services, provider));


    internal static void SetMainThread()
    {
#if WINDOWS
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcher != null)
            WindowsPlatform.MainThreadHandler = action => Run(dispatcher, action);
#elif !PLATFORM
        // the Skia desktop and WebAssembly heads get NetPlatform, which has no main thread of its own. This runs from
        // OnLaunched, on the UI thread, so the current thread's queue is Uno's.
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcher != null)
            NetPlatform.MainThreadHandler = action => Run(dispatcher, action);
#endif
        // Android and Apple marshal through their own main looper / main queue, which is Uno's UI thread
    }


#if WINDOWS || !PLATFORM
    static void Run(Microsoft.UI.Dispatching.DispatcherQueue dispatcher, Action action)
    {
        if (dispatcher.HasThreadAccess)
            action();
        else
            dispatcher.TryEnqueue(() => action());
    }
#endif
}
