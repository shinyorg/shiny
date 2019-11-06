﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Infrastructure;
using Shiny.Locations;

[assembly: Shiny.ShinyLocationsAutoRegister]

namespace Shiny
{
    public class ShinyGpsAttribute : ServiceModuleAttribute
    {
        public ShinyGpsAttribute(Type delegateType = null)
            => this.DelegateType = delegateType;

        public Type DelegateType { get; set; }

        public override void Register(IServiceCollection services)
            => services.UseGps(this.DelegateType);
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


    public class ShinyLocationsAutoRegisterAttribute : AutoRegisterAttribute
    {
        public override void Register(IServiceCollection services)
        {
            var implType = this.FindImplementationType(typeof(IGeofenceDelegate), true);
            services.UseGeofencing(implType);

            implType = this.FindImplementationType(typeof(IGpsDelegate), false);
            services.UseGps(implType);

            services.UseMotionActivity();
        }
    }
}