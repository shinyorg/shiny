using System.Threading.Tasks;

namespace Shiny.Locations;


public interface IMotionActivityDelegate
{
    Task OnReading(MotionActivityReading reading);
}
