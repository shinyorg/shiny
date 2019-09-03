using System;
using MvvmCross;
using MvvmCross.Core;


namespace Shiny.Integrations.MvvmCross
{
    public static class Extensions
    {
        public static void InstallShiny(this MvxSetup setup)
        {
            ShinyHost.Populate((serviceType, func, lifetime) =>
                Mvx.IoCProvider.RegisterType(serviceType, func)
            );
        }
    }
}
