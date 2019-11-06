using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Caching;
using Shiny.Infrastructure;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an injectable (ICache) cache service that doesn't actually cache at all - good for testing
        /// </summary>
        /// <param name="services"></param>
        public static void UseVoidCache(this IServiceCollection services)
            => services.AddSingleton<ICache, VoidCache>();


        /// <summary>
        /// Adds an injectable (ICache) in-memory cache
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="defaultLifespan">The default timespan for how long objects should live in cache if time is not explicitly set</param>
        /// <param name="cleanUpTimer">The internal cleanup time interval (don't make this too big or too small)</param>
        public static void UseMemoryCache(this IServiceCollection services,
                                          TimeSpan? defaultLifespan = null,
                                          TimeSpan? cleanUpTimer = null)
            => services.AddSingleton<ICache>(_ => new MemoryCache(defaultLifespan, cleanUpTimer));


        /// <summary>
        /// Uses the built-in repository (default is file based) to store cache data
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="defaultLifespan">The default timespan for how long objects should live in cache if time is not explicitly set</param>
        /// <param name="cleanUpTimer">The internal cleanup time interval (don't make this too big or too small)</param>
        public static void UseRepositoryCache(this IServiceCollection services,
                                              TimeSpan? defaultLifespan = null,
                                              TimeSpan? cleanUpTimer = null)
            => services.AddSingleton<ICache>(sp =>
            {
                var repository = sp.GetRequiredService<IRepository>();
                return new RepositoryCache(repository, defaultLifespan, cleanUpTimer);
            });
    }
}
