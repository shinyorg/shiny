using System;
using System.Reactive.Linq;


namespace Shiny.Sensors
{
    public class AmbientLightImpl : IAmbientLight
    {
        public bool IsAvailable => false;
        public IObservable<double> WhenReadingTaken() => Observable.Empty<double>();
    }
}
