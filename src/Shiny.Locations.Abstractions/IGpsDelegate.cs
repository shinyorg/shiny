using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public interface IGpsDelegate : IShinyDelegate
    {
        Task OnReading(IGpsReading reading);
    }
}
