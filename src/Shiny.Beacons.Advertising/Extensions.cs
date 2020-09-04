using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Beacons.Advertising;


namespace Shiny
{
    public static class Extensions
    {
        public static bool UseBeaconAdvertising(this IServiceCollection services)
        {
            services.TryAddSingleton<IBeaconAdvertiser, BeaconAdvertiser>();
            return services.UseBleHosting();
        }
    }
}
