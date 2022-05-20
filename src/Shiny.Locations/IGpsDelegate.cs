using System.Threading.Tasks;

namespace Shiny.Locations;


public interface IGpsDelegate
{
    /// <summary>
    /// This is fired when the gps reading has changed.
    /// </summary>
    /// <param name="reading">The gps reading.</param>
    Task OnReading(GpsReading reading);
}
