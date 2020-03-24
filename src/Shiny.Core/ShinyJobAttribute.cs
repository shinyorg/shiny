using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny
{
    public class ShinyJobAttribute : ServiceModuleAttribute
    {
        public ShinyJobAttribute(Type jobType, string? identifier = null)
        {
            this.Type = jobType;
            this.Identifier = identifier;
        }


        public Type Type { get; }
        public string? Identifier { get; set; }
        public bool BatteryNotLow { get; set; }
        public bool DeviceCharging { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;


        public override void Register(IServiceCollection services)
        {
            services.RegisterJob(new JobInfo(this.Type, this.Identifier)
            {
                Repeat = true,
                DeviceCharging = this.DeviceCharging,
                BatteryNotLow = this.BatteryNotLow,
                RequiredInternetAccess = this.RequiredInternetAccess
            });
        }
    }
}
