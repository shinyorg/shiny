using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Locations;


namespace Shiny
{
    public class ShinyGpsAttribute : ServiceModuleAttribute
    {
        public ShinyGpsAttribute(Type delegateType = null)
            => this.DelegateType = delegateType;
        public Type DelegateType { get; set; }


        public override void Register(IServiceCollection services)
        {
            if (this.DelegateType != null)
                services.AddSingleton(typeof(IGpsDelegate), this.DelegateType);

            services.UseGps();
        }
    }


    public class ShinyMotionActivityAttribute : ServiceModuleAttribute
    {
        public override void Register(IServiceCollection services)
        {
            services.UseMotionActivity();
        }
    }


    public class ShinyGeofencesAttribute : ServiceModuleAttribute
    {
        public ShinyGeofencesAttribute(Type delegateType)
            => this.DelegateType = delegateType;

        public Type DelegateType { get; }
        public override void Register(IServiceCollection services)
            => services.UseGeofencing(this.DelegateType);
    }
}