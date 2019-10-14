using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinySqliteIntegrationAttribute : ServiceModuleAttribute
    {
        public ShinySqliteIntegrationAttribute(bool useCache = false,
                                               bool useSettings = false,
                                               bool useRepository = false,
                                               bool crashLogging = false,
                                               bool eventLogging = false)
        {
            this.UseCache = useCache;
            this.UseSettings = useSettings;
            this.UseRepository = useRepository;
            this.UseCrashLogging = crashLogging;
            this.UseEventLogging = eventLogging;
        }


        public bool UseCache { get; set; }
        public bool UseSettings { get; set; }
        public bool UseRepository { get; set; }
        public bool UseCrashLogging { get; set; }
        public bool UseEventLogging { get; set; }


        public override void Register(IServiceCollection services)
        {
            if (this.UseEventLogging || this.UseCrashLogging)
                services.UseSqliteLogging(this.UseCrashLogging, this.UseEventLogging);

            if (this.UseCache)
                services.UseSqliteCache();

            if (this.UseSettings)
                services.UseSqliteSettings();

            if (this.UseRepository)
                services.UseSqliteStorage();
        }
    }
}
