using System;
using System.Threading.Tasks;


namespace Shiny.Sensors
{
    //https://docs.microsoft.com/en-us/xamarin/ios/watchos/platform/workout-apps
    public class HeartRateMonitorImpl : IHeartRateMonitor
    {
        public bool IsAvailable => throw new NotImplementedException();

        public Task<AccessState> RequestAccess()
        {
            throw new NotImplementedException();
        }

        public IObservable<ushort> WhenReadingTaken()
        {
            throw new NotImplementedException();
        }
    }
}
