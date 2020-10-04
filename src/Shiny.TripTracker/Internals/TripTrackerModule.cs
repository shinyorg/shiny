using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Locations;


namespace Shiny.TripTracker.Internals
{
    public class TripTrackerModule : ShinyModule
    {
        readonly Type delegateType;
        readonly TripTrackingType? startingType;


        public TripTrackerModule(Type delegateType, TripTrackingType? startingType = null)
        {
            this.delegateType = delegateType;
            this.startingType = startingType;
        }


        static bool added = false;
        public override void Register(IServiceCollection services)
        {
            if (!added)
            {
                services.AddSingleton<IDataService, SqliteDataService>();
                services.AddSingleton<ITripTrackerManager, TripTrackerManagerImpl>();
                services.AddSingleton<IGpsDelegate, TripTrackerGpsDelegate>();
                added = true;
            }
            services.AddSingleton(typeof(ITripTrackerDelegate), this.delegateType);
        }


        public override async void OnContainerReady(IServiceProvider services)
        {
            base.OnContainerReady(services);
            if (this.startingType != null)
            {
                var manager = services.Resolve<ITripTrackerManager>(true);
                var result = await manager.RequestAccess();
                if (result == AccessState.Available)
                    await manager.StartTracking(this.startingType.Value);
            }
        }
    }
}
