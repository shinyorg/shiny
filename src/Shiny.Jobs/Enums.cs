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
