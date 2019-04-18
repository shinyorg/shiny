using System;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public interface IGpsManager
    {
        AccessState Status { get; }
        Task<AccessState> RequestAccess(bool backgroundMode);
        IObservable<IGpsReading> GetLastReading();
        IObservable<IGpsReading> WhenReading();

        bool IsListening { get; }
        Task StartListener(GpsRequest request = null);
        Task StopListener();
    }
}
