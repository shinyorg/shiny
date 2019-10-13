using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public class AssemblyServiceModule : ShinyModule
    {
        public override void Register(IServiceCollection services)
        {
            var attributes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(ass => ass.GetCustomAttributes(true));

            foreach (var attribute in attributes)
            {
                if (attribute is ServiceRegisterAttribute serviceAttribute)
                {
                    switch (serviceAttribute.Lifetime)
                    {
                        case ServiceLifetime.Transient:
                            services.AddTransient(serviceAttribute.ServiceType, serviceAttribute.ImplementationType);
                            break;

                        case ServiceLifetime.Scoped:
                            services.AddScoped(serviceAttribute.ServiceType, serviceAttribute.ImplementationType);
                            break;

                        case ServiceLifetime.Singleton:
                            services.AddSingleton(serviceAttribute.ServiceType, serviceAttribute.ImplementationType);
                            break;
                    }
                    
                }
                else if (attribute is ServiceModuleAttribute module)
                {
                    module.Register(services);
                }
            }
        }
    }
}
