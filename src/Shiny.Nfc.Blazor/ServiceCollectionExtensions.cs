using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Nfc;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNfc(this IServiceCollection services)
    {
        //services.TryAddSingleton<INfcManager, NfcManager>();
        return services;
    }
}

