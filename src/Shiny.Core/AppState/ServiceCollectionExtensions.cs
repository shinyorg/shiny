using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.AppState;


namespace Shiny
{
    public static class AppStateServiceCollectionExtensions
    {
        public static void AddAppState<T>(this IServiceCollection services) where T : class, IAppStateDelegate
        {
            services.TryAddSingleton<AppStateManager>();
            services.AddSingleton<IAppStateDelegate, T>();
        }


        public static void AddAppState<T>(this IServiceCollection services, T instance) where T : class, IAppStateDelegate
        {
            services.TryAddSingleton<AppStateManager>();
            services.AddSingleton<IAppStateDelegate>(instance);
        }


        public static void AddAppState<T>(this IServiceCollection services, Func<IServiceProvider, T> createInstance) where T : class, IAppStateDelegate
        {
            services.TryAddSingleton<AppStateManager>();
            services.AddSingleton<IAppStateDelegate>(createInstance);
        }
    }
}
