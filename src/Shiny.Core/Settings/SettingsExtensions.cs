using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Settings;

namespace Shiny
{
    public static class SettingsExtensions
    {
        static readonly object syncLock = new object();


        /// <summary>
        /// Register a strongly typed application settings provider on the service container
        /// </summary>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="prefix"></param>
        public static void RegisterSettings<TImpl>(this IServiceCollection services, string prefix = null)
                where TImpl : class, INotifyPropertyChanged, new()
            => services.RegisterSettings<TImpl, TImpl>(prefix);


        /// <summary>
        /// Register a strongly typed application settings provider on the service container with a service interface
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        /// <param name="services"></param>
        /// <param name="prefix"></param>
        public static void RegisterSettings<TService, TImpl>(this IServiceCollection services, string prefix = null)
                where TService : class
                where TImpl : class, TService, INotifyPropertyChanged, new()
            => services.AddSingleton<TService>(c => c
                .GetService<ISettings>()
                .Bind<TImpl>(prefix)
            );

        /// <summary>
        /// Thread safetied setting value incrementor
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int IncrementValue(this ISettings settings, string name = "NextId")
        {
            var id = 0;

            lock (syncLock)
            {
                id = settings.Get(name, 0);
                id++;
                settings.Set(name, id);
            }
            return id;
        }
    }
}
