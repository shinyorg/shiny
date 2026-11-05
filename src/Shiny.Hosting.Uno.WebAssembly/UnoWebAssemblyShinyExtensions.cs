using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using Shiny.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny;


/// <summary>
/// Gives Shiny's browser modules what they need on Uno's WebAssembly head.
/// </summary>
public static class UnoWebAssemblyShinyExtensions
{
    /// <summary>
    /// Registers the <see cref="IJSRuntime"/> (and <see cref="IJSInProcessRuntime"/>) Shiny's browser modules use -
    /// battery, connectivity, screen, BluetoothLE, GPS, push and the rest - on Uno's WebAssembly head, where there is no
    /// Blazor runtime to provide one. Does nothing outside the browser, so it is safe in configuration every head shares.
    /// Call it next to <c>UseShiny()</c>.
    /// </summary>
    /// <remarks>
    /// Also registers an <see cref="IStartupService"/> that starts the browser monitors before the first navigation -
    /// what <c>await services.UseShiny()</c> does in a Blazor app - so the first page reads real values rather than
    /// Unknown. An <see cref="IJSRuntime"/> already registered is kept.
    /// </remarks>
    public static IHostBuilder UseShinyWebAssembly(this IHostBuilder builder)
    {
        if (!OperatingSystem.IsBrowser())
            return builder;

        // created here, not lazily, so the interop script starts loading as the container is configured
        var runtime = UnoJSRuntime.GetOrCreate();
        return builder.ConfigureServices(services =>
        {
            services.TryAddSingleton<IJSRuntime>(runtime);
            services.TryAddSingleton<IJSInProcessRuntime>(runtime);

            // navigation awaits this before the first page, so the monitors report real values from the first read
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupService, ShinyBrowserStartup>());
        });
    }
}
