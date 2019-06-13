using System;
using System.Threading.Tasks;

namespace Shiny.Locations
{
    public interface IGpsDelegate
    {
        Task OnReading(IGpsReading reading);
    }
}
