using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public class AutoRegisterModule : ShinyModule
    {
        public override void Register(IServiceCollection services) => AssemblyQueries
            .GetShinyAssemblies()
            .SelectMany(x => x.GetCustomAttributes(true))
            .Where(x => !x.GetType().IsAbstract)
            .OfType<AutoRegisterAttribute>()
            .ToList()
            .ForEach(x => x.Register(services));
    }
}
