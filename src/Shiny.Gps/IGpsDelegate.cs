using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// Background listener invoked by the GPS manager whenever a new reading is received.
/// </summary>
public interface IGpsDelegate
{
    /// <summary>
    /// Invoked by the GPS manager when a new reading is available.
    /// </summary>
    /// <param name="reading">The latest GPS reading.</param>
    Task OnReading(GpsReading reading);
}
