using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Push.Blazor;

namespace Shiny;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPush(this IServiceCollection services)
    {
        services.AddShinyService<PushManager>();
        return services;
    }
}

