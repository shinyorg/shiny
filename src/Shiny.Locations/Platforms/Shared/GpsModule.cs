#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Locations
{
    class GpsModule : ShinyModule
    {
        readonly Action<GpsRequest> requestIfPermissionGranted;
        public GpsModule(Action<GpsRequest> requestIfPermissionGranted) => this.requestIfPermissionGranted = requestIfPermissionGranted;


        public override void Register(IServiceCollection services)
        {
            services.AddSingleton<IGpsManager, GpsManagerImpl>();
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (this.requestIfPermissionGranted != null)
            {
                var mgr = services.GetService<IGpsManager>();
                var access = await mgr.RequestAccess(true);
                if (access == AccessState.Available)
                {
                    var request = new GpsRequest();
                    this.requestIfPermissionGranted(request);
                    request.UseBackground = true;
                    await mgr.StartListener(request);
                }
            }
        }
    }
}
#endif