namespace Shiny.Locations;


/// <summary>
/// A category of motion activity reported by the platform's activity recognition.
/// </summary>
public enum MotionActivityType
{
    /// <summary>
    /// The activity could not be determined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The device is not moving.
    /// </summary>
    Stationary = 1,

    /// <summary>
    /// The user is walking.
    /// </summary>
    Walking = 2,

    /// <summary>
    /// The user is running.
    /// </summary>
    Running = 3,

    /// <summary>
    /// The user is cycling.
    /// </summary>
    Cycling = 4,

    /// <summary>
    /// The user is traveling in a motor vehicle.
    /// </summary>
    Automotive = 5
}
