
/// <summary>
/// Describes the current charging state of the device battery.
/// </summary>
public enum BatteryState
{
    /// <summary>
    /// Power state hasn't been checked yet or it is in an unknown state
    /// </summary>
    Unknown,

    /// <summary>
    /// No battery has been detected so device is plugged in
    /// </summary>
    None,

    /// <summary>
    /// Device is plugged in and charging
    /// </summary>
    Charging,

    /// <summary>
    /// Device is fully charged and plugged in
    /// </summary>
    Full,

    /// <summary>
    /// Device is possibly plugged in, but not receiving enough power from source
    /// </summary>
    NotCharging,

    /// <summary>
    /// Device is running on battery
    /// </summary>
    Discharging
}


/// <summary>
/// Where the device is drawing its power from.
/// </summary>
public enum BatteryPowerSource
{
    /// <summary>
    /// The power source could not be determined
    /// </summary>
    Unknown,

    /// <summary>
    /// Running on the battery
    /// </summary>
    Battery,

    /// <summary>
    /// Plugged into mains power
    /// </summary>
    AC,

    /// <summary>
    /// Powered over USB
    /// </summary>
    Usb,

    /// <summary>
    /// Charging wirelessly
    /// </summary>
    Wireless
}


/// <summary>
/// Whether the operating system's energy saver (Low Power Mode, Battery Saver, the power-saver profile) is on.
/// </summary>
public enum EnergySaverStatus
{
    /// <summary>
    /// The platform does not report it, or it could not be read
    /// </summary>
    Unknown,

    On,

    Off
}
