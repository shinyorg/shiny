using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Versioning;


namespace Shiny
{
    public static partial class ServiceCollectionExtensions
    {
        public static void UseVersionManagement(this IServiceCollection services, Type? delegateType = null)
        {
            if (delegateType != null)
                services.AddSingleton(typeof(IVersionChangeDelegate), delegateType);

            services.TryAddSingleton<IVersionManager, VersionManager>();
        }


        public static void UseVersionManagement<T>(this IServiceCollection services) where T : class, IVersionChangeDelegate
            => services.UseVersionManagement(typeof(T));
    }
}
