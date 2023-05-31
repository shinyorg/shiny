#if APPLE
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreLocation;
using Shiny.Locations;

namespace Shiny;


public abstract class ShinyLocationDelegate : CLLocationManagerDelegate
{
    readonly Subject<CLAuthorizationStatus> statusSubject = new();


    public IObservable<AccessState> WhenAccessStatusChanged(bool background) => this.statusSubject
        .Where(x => x != CLAuthorizationStatus.NotDetermined)
        .Select(x => x.FromNative(background));


    public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
        => this.statusSubject.OnNext(status);
}
#endif
/*
Accuracy authorization in iOS14
iOS14 introduces a new dimension to location permissions. On the one hand, When In Use and Always define the circumstances in which the app can use location; on the other, Accuracy Authorization defines the frequency and precision of location data.

In iOS 14, every time your app asks for Always or When In Use permissions, user will have the option to also choose the accuracy. The two options will be:

Full Accuracy, which works the same way as previous iOS versions do,
Reduced Accuracy, which only provides a 1-20 kilometer circular area that covers the current user's location. iOS14 only guarantees that the user is somewhere inside this area. The frequency of updates is also reduced to four updates per hour.
User will also be able to switch between the two in settings at any point in time. This might be okay for consumer apps that don't require precise location access, but is a non-starter for field service and logistics apps.

Luckily, iOS14 has APIs to query the current status of Accuracy Authorization and to subscribe to any changes. Once your app detects Reduced Accuracy state it can:

Navigate the user to Settings.app to flip the Full Accuracy switch on for Precise Location.
Use the new API to request temporary Full Accuracy authorization. This is analogous to Allow Once option, where Full Accuracy will be available while the app is in use.
Choosing between the two can depend on how location is used in a business context, but in most cases blocking the app until the user enables Precise Location is the right way to go.
*/