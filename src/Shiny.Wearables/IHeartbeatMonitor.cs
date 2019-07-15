using System;


namespace Shiny
{
    public interface IHeartbeatMonitor
    {
        bool IsAvailable { get; }
        //IObservable<ushort> WhenBpmChanged();
    }
}
