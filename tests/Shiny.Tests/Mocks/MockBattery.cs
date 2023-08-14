using System.Reactive.Subjects;
using Shiny.Power;

namespace Shiny.Tests.Mocks;


public class MockBattery : IBattery
{
    public void Change(BatteryState state = BatteryState.Full, double level = 1.0)
    {
        this.Status = state;
        this.Level = level;
        this.changeSubj.OnNext(this);
    }


    readonly Subject<IBattery> changeSubj = new();
    public BatteryState Status { get; private set; }
    public double Level { get; private set; }
    public IObservable<IBattery> WhenChanged() => this.changeSubj.StartWith(this);
}

