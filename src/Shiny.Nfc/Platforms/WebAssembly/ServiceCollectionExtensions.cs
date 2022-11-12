using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Nfc;
using Shiny.Nfc.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNfc(this IServiceCollection services)
    {
        services.AddShinyService<NfcManager>();
        return services;
    }
}

