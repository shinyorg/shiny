using System;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public interface IGpsManager
    {
        /// <summary>
        /// If the device is currently listening to GPS broadcasts
        /// </summary>
        bool IsListening { get; }


        /// <summary>
        /// The current status of the GPS manager
        /// </summary>
        AccessState GetCurrentStatus(bool background);


        /// <summary>
        /// Observes changes in the access state
        /// </summary>
        /// <param name="forBackground"></param>
        /// <returns></returns>
        IObservable<AccessState> WhenAccessStatusChanged(bool forBackground);


        /// <summary>
        /// Request access to use GPS hardware
        /// </summary>
        /// <param name="backgroundMode"></param>
        /// <returns></returns>
        Task<AccessState> RequestAccess(bool backgroundMode);


        /// <summary>
        /// Gets the last reading - will also try to get access if you have not used RequestAccess, if access is not granted, this will throw an exception
        /// </summary>
        /// <returns></returns>
        IObservable<IGpsReading> GetLastReading();


        /// <summary>
        /// Hook to the GPS events - usualful for front ends ONLY.  If you need background operations, register the delegate
        /// </summary>
        /// <returns></returns>
        IObservable<IGpsReading> WhenReading();


        /// <summary>
        /// Start the GPS listener
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task StartListener(GpsRequest request);


        /// <summary>
        /// Stop the GPS listener
        /// </summary>
        /// <returns></returns>
        Task StopListener();
    }
}
