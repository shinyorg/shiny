using Shiny.Sensors;
using IAccelerometer = Shiny.Sensors.IAccelerometer;
using IBarometer = Shiny.Sensors.IBarometer;
using ICompass = Shiny.Sensors.ICompass;
using IGyroscope = Shiny.Sensors.IGyroscope;
using IMagnetometer = Shiny.Sensors.IMagnetometer;

namespace Sample.Sensors;


public class AllSensorsViewModel
{
    readonly IServiceProvider services;
    public List<ISensorViewModel> Sensors { get; } = new();
    public bool HasSensors => this.Sensors.Any();


    public AllSensorsViewModel(IServiceProvider services)
    {
        this.services = services;

        this.AddIf<IAccelerometer, MotionReading>("G");
        this.AddIf<IGyroscope, MotionReading>("G");
        this.AddIf<IMagnetometer, MotionReading>("M");
        this.AddIf<ICompass, CompassReading>("D");
        this.AddIf<IAmbientLight, double>("Light");
        this.AddIf<IBarometer, double>("Pressure");
        this.AddIf<IPedometer, int>("Steps");
        this.AddIf<IProximity, bool>("Near");
        this.AddIf<IHumidity, double>("Humidity");
        this.AddIf<ITemperature, double>("Temp");
    }


    void AddIf<T, U>(string measurement)
    {
        var sensor = this.services.GetService<T>() as ISensor<U>;
        if (sensor != null)
            this.Sensors.Add(new SensorViewModel<U>(sensor, measurement));
    }
}
