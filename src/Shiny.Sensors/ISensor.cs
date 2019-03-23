using System;


namespace Shiny.Sensors
{
    public interface ISensor<out T>
    {
        bool IsAvailable { get; }
        IObservable<T> WhenReadingTaken();
    }
}
