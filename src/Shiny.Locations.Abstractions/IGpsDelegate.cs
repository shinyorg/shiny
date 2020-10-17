using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Locations
{
    public interface IGpsDelegate : IShinyDelegate
    {
        /// <summary>
        /// This is fired when the gps reading has changed.
        /// </summary>
        /// <param name="reading">The gps reading.</param>
        Task OnReading(IGpsReading reading);
    }
}
