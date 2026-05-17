using System;

namespace Shiny.Net;


public enum InternetAccess
{
    None = 0,
    Any = 1,
    Unmetered = 2
}


/// <summary>
/// Identifies the physical network transports currently in use by the device.
/// </summary>
[Flags]
public enum ConnectionTypes
{
    /// <summary>No active connection.</summary>
    None = 0,
    /// <summary>Connection type could not be determined.</summary>
    Unknown = 1,
    /// <summary>Connected via Bluetooth tethering or PAN.</summary>
    Bluetooth = 2,
    /// <summary>Connected via a wired Ethernet interface.</summary>
    Wired = 4,
    /// <summary>Connected via Wi-Fi.</summary>
    Wifi = 8,
    /// <summary>Connected via a cellular/mobile network.</summary>
    Cellular = 16
}


/// <summary>
/// Describes the level of network reachability available to the application.
/// </summary>
public enum NetworkAccess
{
    /// <summary>Network reachability could not be determined.</summary>
    Unknown,
    /// <summary>No network connection is available.</summary>
    None,
    /// <summary>Connected to a local network but the internet is not reachable.</summary>
    Local,
    /// <summary>Internet is reachable but restricted (e.g. captive portal or low-data mode).</summary>
    ConstrainedInternet,
    /// <summary>Full, unrestricted internet access is available.</summary>
    Internet
}
