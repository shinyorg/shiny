using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms;


namespace Shiny
{
    public static class ServicesCollectionExtensions
    {
        public static void UseXamarinFormsDependencyService(this IServiceCollection services)
        {
            services.RegisterPostBuildAction(sp =>
            {
                foreach (var service in services)
                {
                    var instance = service.ImplementationInstance ?? sp.GetService(service.ServiceType);
                    var serviceType = service.ServiceType ?? service.ImplementationType ?? instance.GetType();

                    //var method = typeof(DependencyService)
                    //    .GetMethods()
                    //    .FirstOrDefault(x =>
                    //        x.Name == "Register" &&
                    //        x.IsStatic &&
                    //        x.IsPublic &&
                    //        x.GetParameters().Length == 1 &&
                    //        x.GetGenericArguments().Length == 1
                    //    );
                    //var generic = method.MakeGenericMethod(new [] { serviceType });

                    //Console.WriteLine($"{serviceType} - {instance}");
                    //generic.Invoke(null, new [] { instance });
                }
            });
        }
    }
}
