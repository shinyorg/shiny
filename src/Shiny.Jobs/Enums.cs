namespace Shiny.Jobs;

/// <summary>
/// Indicates the lifecycle phase of a job or task run.
/// </summary>
public enum JobState
{
    /// <summary>The job or task is starting.</summary>
    Start,
    /// <summary>The job or task completed successfully.</summary>
    Finish,
    /// <summary>The job or task threw an exception.</summary>
    Error
}

/// <summary>
/// Specifies the kind of internet connectivity a job requires before it can run.
/// </summary>
public enum InternetAccess
{
    /// <summary>The job has no internet requirement.</summary>
    None = 0,
    /// <summary>The job runs when any internet connection is available, metered or not.</summary>
    Any = 1,
    /// <summary>The job runs only over an unmetered connection (typically Wi-Fi).</summary>
    Unmetered = 2
}

/// <summary>
/// Identifies a specific physical network transport.
/// </summary>
public enum NetworkAccess
{
    /// <summary>No network is available.</summary>
    None,
    /// <summary>The network transport could not be determined.</summary>
    Unknown,
    /// <summary>The device is connected via Bluetooth.</summary>
    Bluetooth,
    /// <summary>The device is connected via wired Ethernet.</summary>
    Ethernet,
    /// <summary>The device is connected via Wi-Fi.</summary>
    WiFi,
    /// <summary>The device is connected via cellular data.</summary>
    Cellular
}

/// <summary>
/// Describes the level of network reachability available.
/// </summary>
public enum NetworkReach
{
    /// <summary>Reachability could not be determined.</summary>
    Unknown,
    /// <summary>No network is reachable.</summary>
    None,
    /// <summary>Only a local network is reachable; the internet is not.</summary>
    Local,
    /// <summary>The internet is reachable but with constraints (e.g. Low Data Mode).</summary>
    ConstrainedInternet,
    /// <summary>The full internet is reachable.</summary>
    Internet
}

/// <summary>
/// Describes the device's current power/charging state.
/// </summary>
public enum PowerState
{
    /// <summary>
    /// Power state hasn't been checked yet or it is in an unknown state.
    /// </summary>
    Unknown,

    /// <summary>
    /// Device is plugged in and charging.
    /// </summary>
    Charging,

    /// <summary>
    /// Device is fully charged and plugged in.
    /// </summary>
    Charged,

    /// <summary>
    /// No battery has been detected so device is plugged in.
    /// </summary>
    NoBattery,

    /// <summary>
    /// Device is running on battery.
    /// </summary>
    Discharging
}
