using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class JobRegistrationAttribute : ServiceModuleAttribute
    {
        public JobRegistrationAttribute(Type jobType)
        {
            this.Type = jobType;
        }


        public Type Type { get; }
        public string Identifier { get; set; }
        public bool BatteryNotLow { get; set; }
        public bool DeviceCharging { get; set; }
        public InternetAccess RequiredInternetAccess { get; set; } = InternetAccess.None;


        public override void Register(IServiceCollection services)
        {
            services.RegisterJob(new JobInfo
            {
                Type = this.Type,
                Identifier = this.Identifier ?? this.Type.GetType().AssemblyQualifiedName,
                Repeat = true,
                DeviceCharging = this.DeviceCharging,
                BatteryNotLow = this.BatteryNotLow,
                RequiredInternetAccess = this.RequiredInternetAccess
            });            
        }
    }
}
