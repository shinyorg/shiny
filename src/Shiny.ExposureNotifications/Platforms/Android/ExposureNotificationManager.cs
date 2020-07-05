using System;
using System.Threading.Tasks;

namespace Shiny.ExposureNotifications
{
    public class ExposureNotificationManager : IExposureNotificationManager
    {
        public Task<AccessState> StartMonitoring()
        {
            throw new NotImplementedException();
        }

        public Task StopMonitoring()
        {
            throw new NotImplementedException();
        }
    }
}
