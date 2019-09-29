using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestMotionActivity : IMotionActivity
    {
        public bool IsSupported { get; set; } = true;
        public List<MotionActivityEvent> EventData { get; set; } = new List<MotionActivityEvent>();
        public Subject<MotionActivityEvent> ActivitySubject { get; } = new Subject<MotionActivityEvent>();


        public Task<IList<MotionActivityEvent>> Query(DateTimeOffset start, DateTimeOffset? end = null)
        {
            end = end ?? DateTimeOffset.UtcNow;
            var data = this.EventData.Where(x => x.Timestamp >= start && x.Timestamp <= end).ToList();
            return Task.FromResult<IList<MotionActivityEvent>>(data);
        }


        public IObservable<MotionActivityEvent> WhenActivityChanged() => this.ActivitySubject;
    }
}
