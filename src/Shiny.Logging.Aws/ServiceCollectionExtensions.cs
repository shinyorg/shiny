using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Logging;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseAwsLogging(this IServiceCollection services, bool crashesEnabled = true, bool analyticsEnabled = false)
        {
            Log.AddLogger(new AwsLogger(), crashesEnabled, analyticsEnabled);
        }
    }
}
