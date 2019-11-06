using System;



namespace Shiny.Sensors
{
    public class PedometerImpl : IPedometer
    {
        public bool IsAvailable => throw new NotImplementedException();

        public IObservable<int> WhenReadingTaken()
        {
            throw new NotImplementedException();
        }
    }
}
