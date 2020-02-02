using System;
using Microsoft.Extensions.DependencyInjection;
using Xamarin.Forms.Internals;


namespace Shiny
{
    public static class ServicesCollectionExtensions
    {
        public static void UseXamarinFormsDependencyService(this IServiceCollection services)
        {
            DependencyResolver.ResolveUsing(t => ShinyHost.Container.GetService(t));

            //services.RegisterPostBuildAction(sp =>
            //{
            //    var depDataType = Type.GetType("Xamarin.Forms.DependencyService.DependencyData");

            //    foreach (var service in services)
            //    {
            //        var instance = service.ImplementationInstance ?? sp.GetService(service.ServiceType);
            //        var serviceType = service.ServiceType ?? service.ImplementationType ?? instance.GetType();

            //        DependencyResolver.ResolveUsing(t => ShinyHost.Container.GetService(t));
            //        //var method = typeof(DependencyService)
            //        //    .GetMethods()
            //        //    .FirstOrDefault(x =>
            //        //        x.Name == nameof(DependencyService.Register) &&
            //        //        x.IsStatic &&
            //        //        x.IsPublic &&
            //        //        //x.GetParameters().Length == 1 &&
            //        //        x.GetGenericArguments().Length == 1
            //        //    );

            //        // Console.WriteLine($"{serviceType} - {instance}");

            //        //var generic = method.MakeGenericMethod(new[] { serviceType });
            //        //generic.Invoke(null, new[] { instance });
            //    }
            //});
        }


        //static Type dependencyDataType;

        //static void Init()
        //{
        //    // DependencyImplementations //new Dictionary<Type, DependencyData>();
        //    dependencyDataType = Type.GetType("Xamarin.Forms.DependencyService.DependencyData");
        //}

        //static void AddType(Type serviceType, object instance)
        //{

        //}
    }
}

