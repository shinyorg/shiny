#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Locations
{
    class GpsModule : ShinyModule
    {
        readonly Type delegateType;
        readonly Action<GpsRequest> requestIfPermissionGranted;


        public GpsModule(Type delegateType, Action<GpsRequest> requestIfPermissionGranted)
        {
            this.delegateType = delegateType;
            this.requestIfPermissionGranted = requestIfPermissionGranted;
        }


        public override void Register(IServiceCollection services)
        {
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
                var request = new GpsRequest();
                this.requestIfPermissionGranted(request);

                var access = await mgr.RequestAccess(request);
                if (access == AccessState.Available)
                {
                    request.UseBackground = true;
                    await mgr.StartListener(request);
                }
            }
        }
    }
}
#endif