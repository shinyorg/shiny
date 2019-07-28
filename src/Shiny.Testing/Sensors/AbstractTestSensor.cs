using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Sensors;


namespace Shiny.Testing.Sensors
{
    public abstract class AbstractTestSensor<T> : ISensor<T>
    {
        public bool IsAvailable { get; set; }
        public virtual Subject<T> SensorSubject { get; } = new Subject<T>();
        public virtual IObservable<T> WhenReadingTaken() => this.SensorSubject;

        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public virtual Task<AccessState> RequestAccess() => Task.FromResult(this.RequestAccessReply);
    }
}
