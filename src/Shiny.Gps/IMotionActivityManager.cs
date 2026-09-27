using System;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Manages access to motion activity recognition (walking, running, cycling, etc.) on the device.
/// </summary>
public interface IMotionActivityManager
{
    /// <summary>
    /// Indicates whether the manager is currently listening for motion activity changes.
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Gets the current access state for motion activity recognition without prompting the user.
    /// </summary>
    AccessState GetCurrentStatus();

    /// <summary>
    /// Requests the platform permissions required to read motion activity.
    /// </summary>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Returns the last known motion activity reading, querying the platform for the current one if necessary.
    /// </summary>
    /// <returns>The latest reading, or null if none is available.</returns>
    Task<MotionActivityReading?> GetLastReading();

    /// <summary>
    /// Raised whenever a new motion activity reading is received. Intended for foreground consumers; background consumers should register an <see cref="IMotionActivityDelegate"/> instead.
    /// </summary>
    event EventHandler<MotionActivityReading> MotionActivityReadingReceived;

    /// <summary>
    /// Starts listening for motion activity changes.
    /// </summary>
    Task StartListener();

    /// <summary>
    /// Stops listening for motion activity changes.
    /// </summary>
    Task StopListener();
}
