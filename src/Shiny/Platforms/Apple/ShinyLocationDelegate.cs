using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreLocation;


namespace Shiny
{
    public abstract class ShinyLocationDelegate : CLLocationManagerDelegate
    {
        readonly Subject<CLAuthorizationStatus> statusSubject = new Subject<CLAuthorizationStatus>();


        public IObservable<AccessState> WhenAccessStatusChanged(bool background) => this.statusSubject
            .Where(x => x != CLAuthorizationStatus.NotDetermined)
            .Select(x => x.FromNative(background));


        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            => this.statusSubject.OnNext(status);
    }
}
