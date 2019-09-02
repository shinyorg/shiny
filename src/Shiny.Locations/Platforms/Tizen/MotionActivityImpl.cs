using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tizen.Sensor;


namespace Shiny.Locations
{
    public class MotionActivityImpl : IMotionActivity, IShinyStartupTask
    {
        readonly InVehicleActivityDetector vehicle;
        readonly WalkingActivityDetector walking;
        readonly RunningActivityDetector running;
        readonly StationaryActivityDetector stationary;


        public MotionActivityImpl()
        {
            this.vehicle = new InVehicleActivityDetector();
            this.walking = new WalkingActivityDetector();
            this.running = new RunningActivityDetector();
            this.stationary = new StationaryActivityDetector();
        }


        public void Start()
        {
        }


        public bool IsSupported => true;


        public Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset end)
        {
            throw new NotImplementedException();
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged()
        {
            throw new NotImplementedException();
        }
    }
}
