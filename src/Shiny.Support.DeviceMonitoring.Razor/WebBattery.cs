using Shiny.Power;

namespace Shiny.Support.DeviceMonitoring;


public class WebBattery : IBattery
{
    public IObservable<IBattery> WhenChanged() => throw new NotImplementedException();

    public BatteryState Status { get; }
    public double Level { get; }
}