using Shiny.Locations;

namespace Sample;


public class SampleGpsDelegate : IGpsDelegate
{
    public Task OnReading(GpsReading reading) => Task.CompletedTask;
}
