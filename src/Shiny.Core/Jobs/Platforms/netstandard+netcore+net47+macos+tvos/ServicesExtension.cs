using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Jobs
{
    public static class ServicesExtension
    {
        public static void ConfigureJobService(this ServiceCollection services, TimeSpan serviceInterval)
        {
            var config = new JobManagerConfig { PeriodInterval  = serviceInterval };
            services.AddSingleton(config);
        }
    }
}
