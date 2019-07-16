using System;
using System.Threading.Tasks;

namespace Shiny.Locations
{
    public class GpsManagerImpl : IGpsManager
    {
        public bool IsListening => throw new NotImplementedException();

        public AccessState GetCurrentStatus(bool background)
        {
            throw new NotImplementedException();
        }

        public IObservable<IGpsReading> GetLastReading()
        {
            throw new NotImplementedException();
        }

        public Task<AccessState> RequestAccess(bool backgroundMode)
        {
            throw new NotImplementedException();
        }

        public Task StartListener(GpsRequest request)
        {
            throw new NotImplementedException();
        }

        public Task StopListener()
        {
            throw new NotImplementedException();
        }

        public IObservable<AccessState> WhenAccessStatusChanged(bool forBackground)
        {
            throw new NotImplementedException();
        }

        public IObservable<IGpsReading> WhenReading()
        {
            throw new NotImplementedException();
        }
    }
}
