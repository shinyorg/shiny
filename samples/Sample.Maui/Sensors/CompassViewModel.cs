using Shiny.Sensors;
using ICompass = Shiny.Sensors.ICompass;

namespace Sample.Sensors;


public class CompassViewModel : ViewModel
{
    readonly ICompass? compass;
    public CompassViewModel(BaseServices services, ICompass? compass) : base(services) => this.compass = compass;


    [Reactive] public double Rotation { get; private set; }
    [Reactive] public double Heading { get; private set; }


    public override async Task InitializeAsync(INavigationParameters parameters)
    {
        if (this.compass == null || !this.compass.IsAvailable)
        {
            await this.Dialogs.DisplayAlertAsync("ERROR", "Compass is not available", "OK");
        }
        else
        {
            this.compass
                .WhenReadingTaken()
                .SubOnMainThread(x =>
                {
                    this.Rotation = 360 - x.MagneticHeading;
                    this.Heading = x.MagneticHeading;
                })
                .DisposedBy(this.DestroyWith);
        }
        await base.InitializeAsync(parameters);
    }
}
