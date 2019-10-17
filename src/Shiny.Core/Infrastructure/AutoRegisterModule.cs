using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;


namespace Shiny.Infrastructure
{
    public class AutoRegisterModule : ShinyModule
    {
        public override void Register(IServiceCollection services)
        {
            var serviceModules = AssemblyQueries
                .GetShinyAssemblies()
                .SelectMany(x => x.GetTypes())
                .OfType<ShinyServiceModuleAttribute>();

            var searchTypes = serviceModules
                .Where(x => x.DelegateType != null)
                .Select(x => x.DelegateType);

            var delegateTypes = AssemblyQueries
                .GetAssumedUserTypes()
                .Where(x => searchTypes.Any(y => y.IsSubclassOf(x)))
                .ToList();

            foreach (var serviceModule in serviceModules)
            {
                if (serviceModule.DelegateType != null)
                {
                    var delTypes = delegateTypes.Where(x => x == serviceModule.DelegateType).ToList();
                    var first = delTypes.FirstOrDefault();
                    services.AddSingleton(serviceModule.DelegateType, first);

                    switch (delTypes.Count)
                    {
                        case 0:
                            if (serviceModule.IsDelegateRequired)
                                throw new ArgumentException($"{serviceModule.GetType().Name} has a required delegate type implementation of {serviceModule.DelegateType.FullName} which was not found in your assemblies");
                            break;

                        case 1:
                            Log.Write("AutoRegisterSuccess", $"Implementation of {serviceModule.DelegateType.FullName} found for {serviceModule.GetType().Name} - {first.FullName}, {first.AssemblyQualifiedName}");
                            break;

                        default:
                            Log.Write("AutoRegisterWarning", $"Multiple implementations of {serviceModule.DelegateType.FullName} found for {serviceModule.GetType().Name} - registering first type {first.FullName}, {first.AssemblyQualifiedName}");
                            break;
                    }
                }
                serviceModule.Register(services); // TODO: not all constructors are nullable
            }
        }
    }
}
