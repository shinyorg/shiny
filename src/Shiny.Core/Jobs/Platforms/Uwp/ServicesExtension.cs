using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Jobs
{
    public static class ServicesExtension
    {
        public static void ConfigureJobService(this ServiceCollection services, TimeSpan serviceInterval)
        {
            // TODO: if current job already registered, replace it with new interval
        }
    }
}
