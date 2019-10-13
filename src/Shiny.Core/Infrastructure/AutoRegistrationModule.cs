using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Infrastructure
{
    public class AutoRegistrationModule : ShinyModule
    {
        public override void Register(IServiceCollection services)
        {
            var attributes = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x.GetName()?.Name?.StartsWith("Shiny") ?? false)
                .SelectMany(x => x
                    .GetCustomAttributes(true)
                    .Where(y => y
                        .GetType()
                        .IsAssignableFrom(typeof(AutoRegisterAttribute))
                    )
                )
                .OfType<AutoRegisterAttribute>();

            foreach (var attribute in attributes)
                attribute.Register(services);
        }
    }
}
