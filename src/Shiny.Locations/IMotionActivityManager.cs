using System;
using System.Threading.Tasks;

namespace Shiny.Locations;


public interface IMotionActivityManager
{
    /// <summary>
    /// If the manager is currently listening for activity changes
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Get the current access state for motion activity recognition
    /// </summary>
    AccessState GetCurrentStatus();

    /// <summary>
    /// Request access to motion activity recognition
    /// </summary>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Gets the last known motion activity reading, or queries the platform for the current one
    /// </summary>
    IObservable<MotionActivityReading?> GetLastReading();

    /// <summary>
    /// Hook to activity change events - useful for front ends.  If you need background operations, register the delegate
    /// </summary>
    IObservable<MotionActivityReading> WhenReading();

    /// <summary>
    /// Start listening for motion activity changes
    /// </summary>
    Task StartListener();

    /// <summary>
    /// Stop listening for motion activity changes
    /// </summary>
    Task StopListener();
}
