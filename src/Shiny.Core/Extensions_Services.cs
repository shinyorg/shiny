using System;
using System.ComponentModel;
using System.Linq;
using Shiny.Settings;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;
using Shiny.Infrastructure;

namespace Shiny
{
    public static class Extensions_Services
    {
        public static void RegisterSettings<TImpl>(this IServiceCollection services, string prefix = null)
                where TImpl : class, INotifyPropertyChanged, new()
            => services.RegisterSettings<TImpl, TImpl>(prefix);


        public static void RegisterSettings<TService, TImpl>(this IServiceCollection services, string prefix = null)
                where TService : class
                where TImpl : class, TService, INotifyPropertyChanged, new()
            => services.AddSingleton<TService>(c => c
                .GetService<ISettings>()
                .Bind<TImpl>(prefix)
            );


        public static void RegisterJob(this IServiceCollection services, JobInfo jobInfo)
        {
            if (!services.Any(x => x.ImplementationType == typeof(PostRegisterTask)))
                services.AddSingleton<IStartupTask, PostRegisterTask>();

            PostRegisterTask.Jobs.Add(jobInfo);
        }


        public static void Replace<TService, TImpl>(this IServiceCollection services)
        {
            var desc = services.SingleOrDefault(x => x.ServiceType == typeof(TService));
            if (desc != null)
                services.Remove(desc);

            services.Add(new ServiceDescriptor(typeof(TService), typeof(TImpl), desc.Lifetime));
        }


        public static void AddIfNotRegister<TService, TImpl>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
        {
            if (!services.IsRegistered<TService>())
                services.Add(new ServiceDescriptor(typeof(TService), typeof(TImpl), lifetime));
        }


        public static bool IsRegistered<TService>(this IServiceCollection services)
            => services.Any(x => x.ServiceType == typeof(TService));


        public static void RegisterStartupTask<T>(this IServiceCollection services) where T : class, IStartupTask
            => services.AddSingleton<IStartupTask, T>();


        public static bool IsRegistered<T>(this IServiceProvider services)
            => services.GetService(typeof(T)) != null;


        public static bool IsRegistered(this IServiceProvider services, Type serviceType)
            => services.GetService(serviceType) != null;
    }
}
