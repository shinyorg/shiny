using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Integrations.XamEssentials;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public static class ServicesCollectionExtensions
    {
        public static void UseXamEssentials(this IServiceCollection services)
        {
            services.AddSingleton<IConnectivity, ConnectivityImpl>();
            services.AddSingleton<ISettings, SettingsImpl>();
            services.AddSingleton<IPowerManager, PowerManagerImpl>();
            services.AddSingleton<IEnvironment, EnvironmentImpl>();
        }
    }
}
