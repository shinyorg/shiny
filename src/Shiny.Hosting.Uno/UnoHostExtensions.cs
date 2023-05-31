#if PLATFORM
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Shiny.Infrastructure;
using Uno.Extensions.Hosting;

namespace Shiny.Hosting;


public static class UnoHostExtensions
{
    // public static IHostBuilder UseShiny(this IHostBuilder builder)
    // {
    //     builder.Services.AddSingleton<IStartupService, ShinyStartupService>();
    //     builder.Services.AddShinyCoreServices();
    //
    //     return builder;
    // }
}
#endif