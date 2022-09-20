namespace Sample.Platform;


public class BatteryViewModel : ViewModel
{
    public BatteryViewModel(BaseServices services, Shiny.Power.IBattery battery) : base(services)
    {
        battery
            .WhenChanged()
            .SubOnMainThread(_ =>
            {
                this.Level = battery.Level;
                this.Status = battery.Status;
            })
            .DisposedBy(this.DestroyWith);
    }


    [Reactive] public BatteryState Status { get; private set; }
    [Reactive] public double Level { get; private set; }
}