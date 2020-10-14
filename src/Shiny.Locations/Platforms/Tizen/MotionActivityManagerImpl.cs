using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.Sensor;


namespace Shiny.Locations
{
    public class MotionActivityManagerImpl : IMotionActivityManager, IShinyStartupTask
    {
        readonly InVehicleActivityDetector vehicle;
        readonly WalkingActivityDetector walking;
        readonly RunningActivityDetector running;
        readonly StationaryActivityDetector stationary;


        public MotionActivityManagerImpl()
        {
            this.vehicle = new InVehicleActivityDetector();
            this.walking = new WalkingActivityDetector();
            this.running = new RunningActivityDetector();
            this.stationary = new StationaryActivityDetector();
        }


        public void Start()
        {
            this.vehicle.DataUpdated += (sender, args) => {  };
            this.walking.DataUpdated += (sender, args) => { };
            this.running.DataUpdated += (sender, args) => { };
            this.stationary.DataUpdated += (sender, args) => { };

            this.vehicle.Start();
            this.walking.Start();
            this.running.Start();
            this.stationary.Start();
        }

        public Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);
        public bool IsSupported => true;


        public Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
        {
            throw new NotImplementedException();
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged()
        {
            throw new NotImplementedException();
        }
    }
}
