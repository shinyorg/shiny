using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Shiny.BluetoothLE.Hosting.Hubs.Infrastructure
{
    public class HostingHubModule : ShinyModule
    {
        static bool isRegistered = false;
        internal static List<Type> HubTypes { get; } = new List<Type>();


        public override void Register(IServiceCollection services)
            => services.TryAddSingleton<IHostingHubRegistration, HostingHubRegistrationImpl>();


        public override void OnContainerReady(IServiceProvider services)
        {
            if (isRegistered)
                return;

            isRegistered = true;
            base.OnContainerReady(services);
            var manager = services.GetService<IBleHostingManager>();
            services
                .GetRequiredService<IHostingHubRegistration>()
                .Register(manager, HubTypes.ToArray());
        }
    }
}
