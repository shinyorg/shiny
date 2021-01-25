using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestMotionActivityManager : IMotionActivityManager
    {
        public AccessState CurrentStatus { get; set; } = AccessState.Available;
        public List<MotionActivityEvent> EventData { get; set; } = new List<MotionActivityEvent>();
        public Subject<MotionActivityEvent> ActivitySubject { get; } = new Subject<MotionActivityEvent>();


        public Task<AccessState> RequestAccess() => Task.FromResult(this.CurrentStatus);

        public Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
        {
            end = end ?? DateTimeOffset.UtcNow;
            lock (this.EventData)
            {
                var data = this.EventData.Where(x => x.Timestamp >= start && x.Timestamp <= end).ToList();
                return Task.FromResult<IList<MotionActivityEvent>>(data);
            }
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged() => this.ActivitySubject;


        IDisposable? generating;
        public bool IsGeneratingTestData => this.generating != null;
        public MotionActivityType? GeneratingActivityType { get; private set; }
        public MotionActivityConfidence? GeneratingConfidence { get; private set; }
        public TimeSpan? GeneratingInterval { get; private set; }


        public void StartGeneratingTestData(MotionActivityType type, TimeSpan interval, MotionActivityConfidence? confidence = null)
        {
            if (this.generating != null)
                throw new ArgumentException("Manager is already generating test data");

            this.GeneratingActivityType = type;
            this.GeneratingConfidence = confidence ?? MotionActivityConfidence.High;
            this.GeneratingInterval = interval;

            this.generating = Observable
                .Interval(interval)
                .Subscribe(x =>
                {
                    var e = new MotionActivityEvent(
                        type,
                        MotionActivityConfidence.High,
                        DateTimeOffset.Now
                    );
                    lock (this.EventData)
                        this.EventData.Add(e);

                    this.ActivitySubject.OnNext(e);
                });
        }


        public void StopGeneratingTestData()
        {
            this.generating?.Dispose();
            this.generating = null;

            this.GeneratingActivityType = null;
            this.GeneratingConfidence = null;
            this.GeneratingInterval = null;
        }
    }
}
