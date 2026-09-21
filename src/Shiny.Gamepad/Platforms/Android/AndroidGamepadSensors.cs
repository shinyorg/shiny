using Android.Hardware;
using Android.Views;

namespace Shiny.Gamepad;


/// <summary>
/// Reads the gyroscope and accelerometer built into a controller, through the
/// <see cref="SensorManager"/> that <see cref="InputDevice"/> hands out for it.
/// </summary>
/// <remarks>
/// <para>Android 12 gave each input device its own sensor manager, so a controller's motion sensors
/// are reached the same way the phone's own are - but scoped to that controller, which is what
/// makes telling two connected pads apart possible at all.</para>
/// <para>The two sensors report independently and at different rates, so a reading is assembled
/// from the most recent sample of each rather than waiting for them to line up. A controller with
/// only one of the two still reports, with the missing half left at zero.</para>
/// </remarks>
class AndroidGamepadSensors(InputDevice device, Action<GamepadMotion> onReading)
    : Java.Lang.Object, ISensorEventListener
{
    const float MetresPerSecondSquaredToG = 9.80665f;

    readonly SensorManager? manager = device.SensorManager;
    Sensor? gyroscope;
    Sensor? accelerometer;
    bool running;

    float gyroX, gyroY, gyroZ;
    float accelX, accelY, accelZ;


    /// <summary>Whether the controller has either sensor.</summary>
    public static bool IsSupported(InputDevice device)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return false;

        var manager = device.SensorManager;
        if (manager == null)
            return false;

        return manager.GetDefaultSensor(SensorType.Gyroscope) != null ||
               manager.GetDefaultSensor(SensorType.Accelerometer) != null;
    }


    public void Start()
    {
        if (this.running || this.manager == null)
            return;

        this.gyroscope = this.manager.GetDefaultSensor(SensorType.Gyroscope);
        this.accelerometer = this.manager.GetDefaultSensor(SensorType.Accelerometer);

        // SensorDelay.Game is the rate Android intends for this - roughly 50Hz - and is the right
        // trade for motion aiming. Fastest would flood the callback with samples no frame can use.
        if (this.gyroscope != null)
            this.manager.RegisterListener(this, this.gyroscope, SensorDelay.Game);

        if (this.accelerometer != null)
            this.manager.RegisterListener(this, this.accelerometer, SensorDelay.Game);

        this.running = true;
    }


    public void Stop()
    {
        if (!this.running || this.manager == null)
            return;

        this.manager.UnregisterListener(this);
        this.running = false;
        this.gyroscope = null;
        this.accelerometer = null;
    }


    public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }


    public void OnSensorChanged(SensorEvent? e)
    {
        if (e?.Values is not { Count: >= 3 } values)
            return;

        switch (e.Sensor?.Type)
        {
            case SensorType.Gyroscope:
                this.gyroX = values[0];
                this.gyroY = values[1];
                this.gyroZ = values[2];
                break;

            case SensorType.Accelerometer:
                // Android reports acceleration in m/s2; the model is in g, as Apple and evdev report it
                this.accelX = values[0] / MetresPerSecondSquaredToG;
                this.accelY = values[1] / MetresPerSecondSquaredToG;
                this.accelZ = values[2] / MetresPerSecondSquaredToG;
                break;

            default:
                return;
        }

        onReading(new GamepadMotion(
            this.gyroX, this.gyroY, this.gyroZ,
            this.accelX, this.accelY, this.accelZ,
            Environment.TickCount64
        ));
    }
}
