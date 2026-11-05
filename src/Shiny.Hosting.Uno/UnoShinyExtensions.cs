using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Shiny.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny;


/// <summary>
/// Runs Shiny inside an Uno Platform app's Uno.Extensions.Hosting container.
/// </summary>
public static class UnoShinyExtensions
{
    /// <summary>
    /// Registers Shiny's core services in the Uno.Extensions.Hosting container and runs the Shiny host as soon as the
    /// container is built (through Uno's <see cref="IServiceInitialize"/>, so it runs whether or not the host is started).
    /// Register Shiny modules (<c>AddNotifications</c>, <c>AddJobs</c>, ...) in <paramref name="configure"/> or in your
    /// own <c>ConfigureServices</c> - it is the same container.
    /// </summary>
    /// <param name="builder">The Uno.Extensions host builder</param>
    /// <param name="configure">Optional - register Shiny modules and your own services</param>
    public static IHostBuilder UseShiny(this IHostBuilder builder, Action<IServiceCollection>? configure = null)
        => builder.ConfigureServices(services =>
        {
            services.AddShinyCoreServices();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IServiceInitialize, ShinyUnoServiceInitializer>());
            configure?.Invoke(services);
        });


    /// <summary>
    /// Returns the process' one <see cref="IApplicationBuilder"/>, configured by
    /// <see cref="IShinyUnoApplication.ConfigureApp"/>. Call it from <c>OnLaunched</c> in place of
    /// <c>this.CreateBuilder(args)</c>, then use it as usual: <c>MainWindow = builder.Window</c>, then
    /// <c>await builder.NavigateAsync&lt;Shell&gt;()</c> or <c>builder.Build()</c>.
    /// </summary>
    /// <remarks>
    /// On Android, <c>StartShiny&lt;TApp&gt;()</c> has usually built it already in <c>Application.OnCreate</c>; this fills
    /// in the <see cref="IApplicationBuilder.App"/> and <see cref="IApplicationBuilder.Arguments"/> that did not exist then,
    /// and its <c>Build()</c> returns the host that is already running instead of building a second one.
    /// </remarks>
    public static IApplicationBuilder GetShinyBuilder<TApp>(this TApp app, LaunchActivatedEventArgs? args)
        where TApp : Application, IShinyUnoApplication
    {
        ShinyUnoHost.SetMainThread();
        var builder = ShinyUnoHost.GetOrCreateBuilder<TApp>();
        builder.SetLaunch(app, args);
        return builder;
    }
}
