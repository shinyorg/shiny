using System;
using System.Reactive.Linq;
using Foundation;
using UIKit;


namespace Shiny.Sensors
{
    public class ProximityImpl : IProximity
    {
        public bool IsAvailable => true;


        IObservable<bool>? readOb;
        public IObservable<bool> WhenReadingTaken()
            => this.readOb ??= Observable.Create<bool>(ob =>
            {
                UIDevice.CurrentDevice.ProximityMonitoringEnabled = true;
                var observer = NSNotificationCenter
                    .DefaultCenter
                    .AddObserver(
                        UIDevice.ProximityStateDidChangeNotification,
                        _ => ob.OnNext(UIDevice.CurrentDevice.ProximityState)
                    );

                return () =>
                {
                    UIDevice.CurrentDevice.ProximityMonitoringEnabled = false;
                    NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
                };
            })
            .StartWith(UIDevice.CurrentDevice.ProximityState)
            .Publish()
            .RefCount();
    }
}
