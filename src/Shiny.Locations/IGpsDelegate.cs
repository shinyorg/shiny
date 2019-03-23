using System;


namespace Shiny.Locations
{
    public interface IGpsDelegate
    {
        void OnReading(IGpsReading reading);
    }
}
