using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Versioning;


namespace Shiny
{
    public static partial class ServiceCollectionExtensions
    {
        public static void RegisterVersionDetection<T>(this IServiceCollection services) where T : class, IVersionChangeDelegate
        {
            services.AddSingleton<IVersionChangeDelegate, T>();
            services.TryAddSingleton<VersionDetectionTask>();
        }
    }
}
