using System;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Manages access to the device GPS hardware, including permissions, listener lifecycle, and reading delivery.
/// </summary>
public interface IGpsManager
{
    /// <summary>
    /// The request that the active GPS listener was started with, or null if no listener is running.
    /// </summary>
    GpsRequest? CurrentListener { get; }

    /// <summary>
    /// Gets the current access state for the supplied GPS request without prompting the user.
    /// </summary>
    /// <param name="request">The GPS request whose permissions should be evaluated.</param>
    AccessState GetCurrentStatus(GpsRequest request);

    /// <summary>
    /// Requests the platform permissions needed to fulfil the supplied GPS request.
    /// </summary>
    /// <param name="request">The GPS request whose permissions should be granted.</param>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess(GpsRequest request);


    /// <summary>
    /// Returns the last cached GPS reading. Will request access if it has not already been granted; throws if access is denied.
    /// </summary>
    /// <param name="timeout">Optional maximum age allowed for the cached reading.</param>
    /// <returns>The last reading, or null if none is available.</returns>
    Task<GpsReading?> GetLastReading(TimeSpan? timeout = null);


    /// <summary>
    /// Raised whenever a new GPS reading is received. Intended for foreground consumers; background consumers should register an <see cref="IGpsDelegate"/> instead.
    /// </summary>
    event EventHandler<GpsReading> GpsReadingReceived;


    /// <summary>
    /// Starts the GPS listener using the supplied request configuration.
    /// </summary>
    /// <param name="request">The request describing accuracy and background behavior.</param>
    Task StartListener(GpsRequest request);


    /// <summary>
    /// Stops the active GPS listener if one is running.
    /// </summary>
    Task StopListener();
}
