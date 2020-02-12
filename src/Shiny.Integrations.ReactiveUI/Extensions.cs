using System;
using Splat;


namespace Shiny.Integrations.ReactiveUI
{
    public static class Extensions
    {
        public static void InstallShiny(this IMutableDependencyResolver locator)
        {
            ShinyHost.Populate((serviceType, func, lifetime) =>
                locator.Register(func, serviceType)
            );
        }


        public static void InstallShiny() => Locator.CurrentMutable.InstallShiny();
    }
}
