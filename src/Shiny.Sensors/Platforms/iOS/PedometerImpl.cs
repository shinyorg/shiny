using System;
using System.Reactive.Linq;
using CoreMotion;
using Foundation;


namespace Shiny.Sensors
{
    public class PedometerImpl : IPedometer
    {
        public bool IsAvailable => CMStepCounter.IsStepCountingAvailable;


        IObservable<int>? stepOb;
        public IObservable<int> WhenReadingTaken()
            => this.stepOb ??= Observable.Create<int>(ob =>
            {
                var scm = new CMStepCounter();
                scm.StartStepCountingUpdates(NSOperationQueue.CurrentQueue ?? new NSOperationQueue(), 1, (steps, timestamp, error) =>
                    ob.OnNext((int)steps)
                );
                return () =>
                {
                    scm.StopStepCountingUpdates();
                    scm.Dispose();
                };
            })
            .Publish()
            .RefCount();
    }
}
