using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.AppState;


namespace Shiny
{
    public static class AppStateServiceCollectionExtensions
    {
        public static void AddAppState<T>(this IServiceCollection services) where T : class, IAppStateDelegate
        {
            if (!services.IsRegistered<AppStateManager>())
                services.AddSingleton<AppStateManager>();

            services.AddSingleton<IAppStateDelegate, T>();
        }


        public static void AddAppState<T>(this IServiceCollection services, T instance) where T : class, IAppStateDelegate
        {
            if (!services.IsRegistered<AppStateManager>())
                services.AddSingleton<AppStateManager>();

            services.AddSingleton<IAppStateDelegate>(instance);
        }
    }
}
