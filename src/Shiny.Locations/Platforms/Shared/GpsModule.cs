﻿#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Locations
{
    class GpsModule : ShinyModule
    {
        static bool added = false;
        readonly Type? delegateType;
        readonly GpsRequest? requestIfPermissionGranted;


        public GpsModule(Type? delegateType, GpsRequest? requestIfPermissionGranted)
        {
            this.delegateType = delegateType;
            this.requestIfPermissionGranted = requestIfPermissionGranted;
        }


        public override void Register(IServiceCollection services)
        {
            if (added)
                return;

            added = true;
            if (this.delegateType != null)
                services.AddSingleton(typeof(IGpsDelegate), this.delegateType);

            services.AddSingleton<IGpsManager, GpsManagerImpl>();
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (this.requestIfPermissionGranted != null)
            {
                var mgr = services.GetService<IGpsManager>();

                var access = await mgr.RequestAccess(this.requestIfPermissionGranted);
                if (access == AccessState.Available)
                {
                    this.requestIfPermissionGranted.UseBackground = true;
                    await mgr.StartListener(this.requestIfPermissionGranted);
                }
            }
        }
    }
}
#endif