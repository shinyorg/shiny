using System;
using System.Threading.Tasks;

namespace Shiny.Locations;


public interface IGpsManager
{
    /// <summary>
    /// If the device is currently listening to GPS broadcasts
    /// </summary>
    GpsRequest? CurrentListener { get; }

    /// <summary>
    /// Get the current access state
    /// </summary>
    AccessState GetCurrentStatus(GpsRequest request);

    /// <summary>
    /// Request access to use GPS hardware
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<AccessState> RequestAccess(GpsRequest request);


    /// <summary>
    /// Gets the last reading - will also try to get access if you have not used RequestAccess, if access is not granted, this will throw an exception
    /// </summary>
    /// <returns></returns>
    IObservable<GpsReading?> GetLastReading();


    /// <summary>
    /// Hook to the GPS events - useful for front ends ONLY.  If you need background operations, register the delegate
    /// </summary>
    /// <returns></returns>
    IObservable<GpsReading> WhenReading();


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
