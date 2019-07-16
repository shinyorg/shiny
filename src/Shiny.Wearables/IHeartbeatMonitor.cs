using System;


namespace Shiny
{
    //https://docs.microsoft.com/en-us/xamarin/ios/watchos/platform/workout-apps
    public interface IHeartbeatMonitor
    {
        bool IsAvailable { get; }
        //IObservable<ushort> WhenBpmChanged();
    }
}
