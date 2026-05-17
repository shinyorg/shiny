using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Background listener invoked by the motion activity manager when a new activity reading is received.
/// </summary>
public interface IMotionActivityDelegate
{
    /// <summary>
    /// Invoked when a new motion activity reading is available.
    /// </summary>
    /// <param name="reading">The latest motion activity reading.</param>
    Task OnReading(MotionActivityReading reading);
}
