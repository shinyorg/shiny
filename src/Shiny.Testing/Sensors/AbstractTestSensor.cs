using System;
using System.Reactive.Subjects;
using Shiny.Sensors;


namespace Shiny.Testing.Sensors
{
    public abstract class AbstractTestSensor<T> : ISensor<T>
    {
        public bool IsAvailable { get; set; }
        public Subject<T> SensorSubject { get; } = new Subject<T>();
        public IObservable<T> WhenReadingTaken() => this.SensorSubject;
    }
}
