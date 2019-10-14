using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ShinyAppCenterIntegrationAttribute : ServiceModuleAttribute
    {
        public ShinyAppCenterIntegrationAttribute(string appSecret = null, bool logCrashes = true, bool logEvents = false)
        {
            this.AppSecret = appSecret;
            this.LogCrashes = logCrashes;
            this.LogEvents = logEvents;
        }


        public string AppSecret { get; set; }
        public bool LogCrashes { get; set; }
        public bool LogEvents { get; set; }
        public override void Register(IServiceCollection services)
            => services.UseAppCenterLogging(this.AppSecret, this.LogCrashes, this.LogEvents);
    }
}
