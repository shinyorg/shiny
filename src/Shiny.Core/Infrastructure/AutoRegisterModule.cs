using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public class AutoRegisterModule : ShinyModule
    {
        public override void Register(IServiceCollection services)
        {
            var classes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x.GetName()?.FullName?.StartsWith("Shiny") ?? false)
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsPublic && x.IsClass && !x.IsAbstract)
                .Select(x => new
                {
                    Type = x,
                    Attribute = x
                        .GetCustomAttributes(true)
                        .OfType<AutoRegisterAttribute>()
                        .FirstOrDefault()
                })
                .Where(x => x.Attribute != null);

            // TODO: go hunt for each delegate, awful
        }
    }
}
