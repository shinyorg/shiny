namespace Shiny.Jobs;

public enum JobState
{
    Start,
    Finish,
    Error
}

public enum InternetAccess
{
    None = 0,
    Any = 1,
    Unmetered = 2
}

public enum NetworkAccess
{
    None,
    Unknown,
    Bluetooth,
    Ethernet,
    WiFi,
    Cellular
}

public enum NetworkReach
{
    Unknown,
    None,
    Local,
    ConstrainedInternet,
    Internet
}

public enum PowerState
{
    /// <summary>
    /// Power state hasn't been checked yet or it is in an unknown state
    /// </summary>
    Unknown,

    /// <summary>
    /// Device is plugged in and charging
    /// </summary>
    Charging,

    /// <summary>
    /// Device is fully charged and plugged in
    /// </summary>
    Charged,

    /// <summary>
    /// No battery has been detected so device is plugged in
    /// </summary>
    NoBattery,

    /// <summary>
    /// Device is running on battery
    /// </summary>
    Discharging
}