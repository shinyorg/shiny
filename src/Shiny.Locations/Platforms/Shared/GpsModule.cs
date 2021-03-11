#if !NETSTANDARD
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
#if MONOANDROID
using Android.App;
using Android.Gms.Common;
#endif


namespace Shiny.Locations
{
    class GpsModule : ShinyModule
    {
        readonly Type? delegateType;
        public GpsModule(Type? delegateType) => this.delegateType = delegateType;


        public override void Register(IServiceCollection services)
        {
            if (this.delegateType != null)
                services.AddSingleton(typeof(IGpsDelegate), this.delegateType);

#if !MONOANDROID
            services.TryAddSingleton<IGpsManager, GpsManager>();
#else
            var resultCode = GoogleApiAvailability
                .Instance
                .IsGooglePlayServicesAvailable(Application.Context);

            if (resultCode == ConnectionResult.ServiceMissing)
                services.TryAddSingleton<IGpsManager, GooglePlayServiceGpsManagerImpl>();
            else
                services.TryAddSingleton<IGpsManager, LocationServicesGpsManagerImpl>();
#endif
        }
    }
}
#endif