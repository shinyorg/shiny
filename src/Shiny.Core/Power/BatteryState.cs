
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
    Charged,

    /// <summary>
    /// Device is running on battery
    /// </summary>
    Discharging
}