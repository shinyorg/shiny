using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public class AssemblyServiceModule : ShinyModule
    {
        readonly Assembly[] assemblies;
        public AssemblyServiceModule(Assembly[] assemblies = null)
            => this.assemblies = assemblies ?? AssemblyQueries.GetAssumedUserAssemblies().ToArray();


        public override void Register(IServiceCollection services)
        {
            var attributes = this.assemblies.SelectMany(ass => ass.GetCustomAttributes(true));

            foreach (var attribute in attributes)
            {
                if (attribute is ShinyServiceAttribute serviceAttribute)
                {
                    switch (serviceAttribute.Lifetime)
                    {
                        case ServiceLifetime.Transient:
                            if (serviceAttribute.ServiceType == null)
                                services.AddTransient(serviceAttribute.ImplementationType);
                            else
                                services.AddTransient(serviceAttribute.ServiceType, serviceAttribute.ImplementationType);
                            break;

                        case ServiceLifetime.Scoped:
                            if (serviceAttribute.ServiceType == null)
                                services.AddScoped(serviceAttribute.ImplementationType);
                            else
                                services.AddScoped(serviceAttribute.ServiceType, serviceAttribute.ImplementationType);
                            break;

                        case ServiceLifetime.Singleton:
                            if (serviceAttribute.ServiceType == null)
                                services.AddSingleton(serviceAttribute.ImplementationType);
                            else
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
