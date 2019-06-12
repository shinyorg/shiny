using System;
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
                    var instance = sp.GetService(service.ServiceType);
                    var method = typeof(DependencyService).GetMethod("Register");
                    var generic = method.MakeGenericMethod(new [] { service.ServiceType });
                    generic.Invoke(null, new [] { instance });
                }
            });
        }
    }
}
