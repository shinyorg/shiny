using System;


namespace Shiny.Sensors
{
    //https://docs.microsoft.com/en-us/xamarin/ios/watchos/platform/workout-apps
    public interface IHeartRate
    {
        bool IsAvailable { get; }
        //IObservable<ushort> WhenBpmChanged();
    }
}
