using System;
using System.Threading.Tasks;


namespace Shiny.ExposureNotifications
{
    public interface IExposureNotificationManager
    {
        Task<AccessState> StartMonitoring();
        Task StopMonitoring();
    }
}
