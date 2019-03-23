using System;
using System.Reactive.Linq;
using Foundation;
using UIKit;


namespace Shiny.Sensors
{
    public class DeviceOrientationImpl : IDeviceOrientation
    {
        public bool IsAvailable { get; } = true;


        IObservable<DeviceOrientation> readOb;
        public IObservable<DeviceOrientation> WhenReadingTaken()
        {
            this.readOb = this.readOb ?? Observable.Create<DeviceOrientation>(ob =>
            {
                this.BroadcastCurrent(ob); // start with

                var observer = NSNotificationCenter.DefaultCenter.AddObserver(
                    UIApplication.DidChangeStatusBarOrientationNotification,
                    _ => this.BroadcastCurrent(ob)
                );
                UIApplication.SharedApplication.InvokeOnMainThread(UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications);

                return () =>
                {
                    UIApplication.SharedApplication.InvokeOnMainThread(UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications);
                    NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
                };
            })
            .Publish()
            .RefCount()
            .Repeat(1);

            return this.readOb;
        }


        void BroadcastCurrent(IObserver<DeviceOrientation> ob) => UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            switch (UIDevice.CurrentDevice.Orientation)
            {
                case UIDeviceOrientation.Portrait:
                    ob.OnNext(DeviceOrientation.Portrait);
                    break;

                case UIDeviceOrientation.PortraitUpsideDown:
                    ob.OnNext(DeviceOrientation.PortraitUpsideDown);
                    break;

                case UIDeviceOrientation.LandscapeRight:
                    ob.OnNext(DeviceOrientation.LandscapeRight);
                    break;

                case UIDeviceOrientation.LandscapeLeft:
                    ob.OnNext(DeviceOrientation.LandscapeLeft);
                    break;
            }
        });
    }
}